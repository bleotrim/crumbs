using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Crumbs
{
    private readonly Configuration _configuration;
    private readonly FileCollector _collector;
    private readonly FileAnalyzer _analyzer;
    private readonly DiskContentDetailExporter _exporter;
    private readonly SimpleLogger _logger;
    private List<string> _filesOnDisk = new List<string>();

    public Crumbs(Configuration configuration, SimpleLogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = new FileCollector();
        _analyzer = new FileAnalyzer();
        _exporter = new DiskContentDetailExporter();
    }

    public void Run()
    {
        _logger.Info("Starting Crumbs scan...");
        _filesOnDisk = _collector.GetFiles(@"c:\_work", "*.*", SearchOption.AllDirectories);
        _logger.Info($"Collected {_filesOnDisk.Count} files from disk.");

        CreateFileListIfNotExist();
        AddNewFilesOnDisk();
        RemoveFilesNotOnDisk();

        _logger.Info("Crumbs scan completed.");
    }

    private void CreateFileListIfNotExist()
    {
        if (File.Exists(_configuration.FilePath))
        {
            _logger.Info("File list already exists, skipping creation.");
            return;
        }

        _logger.Info("Creating initial file list...");
        var details = new List<FileDetail>();
        int successCount = 0;
        int errorCount = 0;

        for (int i = 0; i < _filesOnDisk.Count; i++)
        {
            var path = _filesOnDisk[i];
            Console.Write($"\rProcessing file {i + 1}/{_filesOnDisk.Count}...");
            try
            {
                var fd = _analyzer.Analyze(path);
                if (fd != null) details.Add(fd);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error analyzing {path}: {ex.Message}");
                errorCount++;
            }
        }
        Console.WriteLine();

        var diskContentDetail = new DiskContentDetail
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = details,
            FileCount = details.Count,
            TotalFileSize = details.Sum(f => f.SizeInBytes)
        };

        _exporter.SaveToJson(diskContentDetail, _configuration.FilePath);
        _logger.Info($"Initial file list saved: {_configuration.FilePath}");
        _logger.Info($"Successfully analyzed: {successCount}, errors: {errorCount}");
    }

    private void AddNewFilesOnDisk()
    {
        _logger.Info("Checking for new files to add...");
        var fileList = _exporter.LoadFromJson(_configuration.FilePath);
        var existingFilePaths = new HashSet<string>(fileList.FileDetails.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
        int addedCount = 0;

        for (int i = 0; i < _filesOnDisk.Count; i++)
        {
            var filePath = _filesOnDisk[i];
            Console.Write($"\rChecking file {i + 1}/{_filesOnDisk.Count}...");
            if (!existingFilePaths.Contains(filePath))
            {
                try
                {
                    var fd = _analyzer.Analyze(filePath);
                    if (fd != null)
                    {
                        fileList.FileDetails.Add(fd);
                        existingFilePaths.Add(filePath);
                        addedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error analyzing {filePath}: {ex.Message}");
                }
            }
        }
        Console.WriteLine();

        var updatedDetail = new DiskContentDetail
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = fileList.FileDetails,
            FileCount = fileList.FileDetails.Count,
            TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes)
        };
        _exporter.SaveToJson(updatedDetail, _configuration.FilePath);

        if (addedCount > 0)
            _logger.Info($"Added {addedCount} new files to the list.");
        else
            _logger.Info("No new files to add.");
    }

    private void RemoveFilesNotOnDisk()
    {
        _logger.Info("Checking for files no longer on disk...");
        var fileList = _exporter.LoadFromJson(_configuration.FilePath);
        var filesOnDiskSet = new HashSet<string>(_filesOnDisk, StringComparer.OrdinalIgnoreCase);

        int total = fileList.FileDetails.Count;
        int removedCount = 0;
        var removedFiles = new List<FileDetail>();

        for (int i = 0; i < total; i++)
        {
            var file = fileList.FileDetails[i];
            if (!filesOnDiskSet.Contains(file.Path))
            {
                removedFiles.Add(file);
                removedCount++;
            }
            if (i % 1000 == 0 || i == total - 1)
                Console.Write($"\rVerifying file {i + 1}/{total}...");
        }
        Console.WriteLine();

        if (removedFiles.Any())
        {
            _logger.Info("Files removed from list because they are no longer present on disk:");
            foreach (var removed in removedFiles)
                _logger.Info($"- {removed.Path}");
        }

        fileList.FileDetails.RemoveAll(f => !filesOnDiskSet.Contains(f.Path));
        fileList.FileCount = fileList.FileDetails.Count;
        fileList.TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes);

        _exporter.SaveToJson(fileList, _configuration.FilePath);
        _logger.Info($"Operation completed. Removed {removedCount} files.");
    }
}
