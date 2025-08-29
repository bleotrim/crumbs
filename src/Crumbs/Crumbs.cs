using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public class ProgressChangedEventArgs : EventArgs
{
    public string Operation { get; }
    public int Processed { get; }
    public int Total { get; }

    public ProgressChangedEventArgs(string operation, int processed, int total)
    {
        Operation = operation;
        Processed = processed;
        Total = total;
    }
}

public class Crumbs
{
    private readonly Configuration _configuration;
    private readonly FileCollector _collector;
    private readonly FileAnalyzer _analyzer;
    private readonly JsonExporter<DiskContent> _diskExporter;
    private readonly SimpleLogger _logger;

    private List<string> _filesOnDisk = new();
    private CrumbsSession? _crumbsSession;

    public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

    protected virtual void OnProgressChanged(string operation, int processed, int total)
    {
        ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(operation, processed, total));
    }

    public Crumbs(Configuration configuration, SimpleLogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _collector = new FileCollector(_logger);
        _analyzer = new FileAnalyzer(_logger);
        _diskExporter = new JsonExporter<DiskContent>(_logger);
    }

    public void Run(string path, CancellationToken token)
    {
        try
        {
            _crumbsSession = new CrumbsSession();

            ValidatePath(path);
            _logger.LogInfo($"Starting Crumbs scan for path: \"{path}\" (SessionId: {_crumbsSession.Id})");

            _filesOnDisk = _collector.GetFiles(path, "*.*", SearchOption.AllDirectories);
            _logger.LogInfo($"Collected {_filesOnDisk.Count:N0} files from disk.");

            CreateFileListIfNotExist();
            RemoveFilesNotOnDisk(token);
            UpdateModifiedFilesWithHash(token);
            AddNewFilesOnDisk(token);

            _logger.LogInfo("Crumbs scan completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo($"Scan was canceled. Partial progress saved in {_configuration.FileList}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error: {ex}");
            throw;
        }
        finally
        {
            if (_crumbsSession != null)
            {
                _crumbsSession.EndedAtUtc = DateTime.UtcNow;
                _crumbsSession.IsSuccessful =
                    (_crumbsSession.AddingSession?.IsSuccessful ?? false) &&
                    (_crumbsSession.UpdateSession?.IsSuccessful ?? false) &&
                    (_crumbsSession.RemovingSession?.IsSuccessful ?? false);

                var exporter = new JsonExporter<CrumbsSession>(_logger);
                var sessionDataPath = Path.Combine(
                    _configuration.SessionFolder,
                    _crumbsSession.StartedAtUtc.ToString("yyyy-MM-dd-HH-mm-ss-fff")
                );
                exporter.SaveToJson(_crumbsSession, sessionDataPath);
            }
        }
    }


    private void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"The specified path does not exist: {path}");
    }

    private void CreateFileListIfNotExist()
    {
        if (File.Exists(_configuration.FileList))
            return;

        _logger.LogInfo("Creating initial file list...");
        SaveDiskContent(BuildDiskContent(new List<FileDetail>()));
    }

    private void AddNewFilesOnDisk(CancellationToken token)
    {
        _logger.LogInfo("Checking for new files to add...");

        if (_crumbsSession == null)
            throw new InvalidOperationException("Crumbs session not initialized.");

        var addedFiles = new List<FileDetail>();
        var errorFiles = new List<Error>();

        var fileList = _diskExporter.LoadFromJson(_configuration.FileList);
        var existingPaths = new HashSet<string>(
            fileList.FileDetails.Select(f => f.Path),
            StringComparer.OrdinalIgnoreCase
        );

        int totalFiles = _filesOnDisk.Count;
        int processed = 0;

        try
        {
            foreach (var filePath in _filesOnDisk)
            {
                token.ThrowIfCancellationRequested();
                processed++;
                OnProgressChanged("AddNewFiles", processed, totalFiles);

                if (existingPaths.Contains(filePath))
                    continue;

                try
                {
                    var fd = _analyzer.AnalyzeNewFile(filePath);
                    if (fd != null)
                    {
                        fileList.FileDetails.Add(fd);
                        existingPaths.Add(filePath);
                        addedFiles.Add(fd);
                    }
                }
                catch (Exception ex)
                {
                    errorFiles.Add(new Error
                    {
                        FilePath = filePath,
                        Message = ex.Message,
                        ExceptionType = ex.GetType().FullName,
                        StackTrace = ex.StackTrace
                    });
                    _logger.LogError($"Error analyzing {filePath}: {ex.Message}");
                }
            }

            SaveDiskContent(BuildDiskContent(fileList.FileDetails));
        }
        catch (OperationCanceledException)
        {
            SaveDiskContent(BuildDiskContent(fileList.FileDetails));
            throw;
        }
        finally
        {
            _crumbsSession.AddingSession = new AddingSession
            {
                AddedFiles = addedFiles,
                Errors = errorFiles,
                IsSuccessful = errorFiles.Count == 0
            };
        }
    }

    private void UpdateModifiedFilesWithHash(CancellationToken token)
    {
        _logger.LogInfo("Checking for files to update...");

        if (_crumbsSession == null)
            throw new InvalidOperationException("Crumbs session not initialized.");

        var fileList = _diskExporter.LoadFromJson(_configuration.FileList);
        var modificationDetails = new List<FileModificationDetail>();
        var errorFiles = new List<Error>();
        int updatedCount = 0;
        int totalFiles = fileList.FileDetails.Count;
        int processed = 0;

        try
        {
            for (int i = 0; i < totalFiles; i++)
            {
                token.ThrowIfCancellationRequested();
                processed++;
                OnProgressChanged("UpdateModifiedFiles", processed, totalFiles);

                var fileDetail = fileList.FileDetails[i];
                try
                {
                    var fileInfo = new FileInfo(fileDetail.Path);
                    bool isModified = fileInfo.LastWriteTimeUtc != fileDetail.Modified ||
                                      fileInfo.Length != fileDetail.SizeInBytes;

                    if (isModified)
                    {
                        var current = _analyzer.AnalyzeModifiedFile(fileDetail)
                                      ?? throw new InvalidOperationException($"Analyzer returned null for {fileDetail.Path}");

                        modificationDetails.Add(new FileModificationDetail
                        {
                            Previous = fileDetail,
                            Current = current
                        });

                        fileList.FileDetails[i] = current;
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorFiles.Add(new Error
                    {
                        FilePath = fileDetail.Path,
                        Message = ex.Message,
                        ExceptionType = ex.GetType().FullName,
                        StackTrace = ex.StackTrace
                    });
                    _logger.LogError($"Error updating {fileDetail.Path}: {ex.Message}");
                }
            }

            if (updatedCount > 0)
                SaveDiskContent(BuildDiskContent(fileList.FileDetails));
        }
        catch (OperationCanceledException)
        {
            SaveDiskContent(BuildDiskContent(fileList.FileDetails));
            _logger.LogInfo("UpdateModifiedFiles canceled. Partial results saved.");
            throw;
        }
        finally
        {
            _crumbsSession.UpdateSession ??= new UpdateSession();
            _crumbsSession.UpdateSession.ModifiedFiles = modificationDetails;
            _crumbsSession.UpdateSession.Errors = errorFiles;
            _crumbsSession.UpdateSession.IsSuccessful = errorFiles.Count == 0;
        }

        _logger.LogInfo($"UpdateModifiedFiles completed. Updated {updatedCount:N0} files, errors: {errorFiles.Count:N0}.");
    }

    private void RemoveFilesNotOnDisk(CancellationToken token)
    {
        _logger.LogInfo("Checking for files to remove...");

        if (_crumbsSession == null)
            throw new InvalidOperationException("Crumbs session not initialized.");

        var fileList = _diskExporter.LoadFromJson(_configuration.FileList);
        var filesOnDiskSet = new HashSet<string>(_filesOnDisk, StringComparer.OrdinalIgnoreCase);

        int totalFiles = fileList.FileDetails.Count;
        int processed = 0;
        var removedFiles = new List<FileDetail>();

        try
        {
            for (int i = 0; i < fileList.FileDetails.Count; i++)
            {
                processed++;
                OnProgressChanged("RemoveFiles", processed, totalFiles);

                var f = fileList.FileDetails[i];
                if (!filesOnDiskSet.Contains(f.Path))
                    removedFiles.Add(f);
            }

            fileList.FileDetails.RemoveAll(f => !filesOnDiskSet.Contains(f.Path));
            SaveDiskContent(BuildDiskContent(fileList.FileDetails));
        }
        catch (OperationCanceledException)
        {
            SaveDiskContent(BuildDiskContent(fileList.FileDetails));
            throw;
        }
        finally
        {
            _crumbsSession.RemovingSession = new RemovingSession
            {
                RemovedFiles = removedFiles,
                IsSuccessful = true
            };
        }
    }

    private DiskContent BuildDiskContent(List<FileDetail> details) =>
        new()
        {
            PartitionId = "UUID",
            FileDetails = details,
            FileCount = details.Count,
            UpdatedUtc = DateTime.UtcNow,
            TotalFileSize = details.Sum(f => f.SizeInBytes)
        };

    private void SaveDiskContent(DiskContent diskContent) =>
        _diskExporter.SaveToJson(diskContent, _configuration.FileList);
}
