public class Crumbs
{
    private readonly Configuration _configuration;
    private readonly FileCollector _collector;
    private readonly FileAnalyzer _analyzer;
    private readonly DiskContentExporter _exporter;
    private readonly SimpleLogger _logger;
    private List<string> _filesOnDisk = new List<string>();
    public Crumbs(Configuration configuration, SimpleLogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = new FileCollector();
        _analyzer = new FileAnalyzer();
        _exporter = new DiskContentExporter();
    }
    public void Run(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Path where to scan files cannot be null or empty. Application will exit.",
                    nameof(path));
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The specified path does not exist: {path}");
            }

            _logger.LogInfo($"Starting Crumbs scan for path: {path}");
            _filesOnDisk = _collector.GetFiles(path, "*.*", SearchOption.AllDirectories);
            _logger.LogInfo($"Collected {_filesOnDisk.Count:N0} files from disk.");

            CreateFileListIfNotExist();
            AddNewFilesOnDisk();
            RemoveFilesNotOnDisk();

            _logger.LogInfo("Crumbs scan completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}{ex.StackTrace}");
            throw;
        }
    }
    private void CreateFileListIfNotExist()
    {
        if (File.Exists(_configuration.FileList))
        {
            _logger.LogInfo("File list already exists, skipping creation.");
            return;
        }

        _logger.LogInfo("Creating initial file list...");
        var details = new List<FileDetail>();
        int successCount = 0;
        int errorCount = 0;

        for (int i = 0; i < _filesOnDisk.Count; i++)
        {
            var path = _filesOnDisk[i];
            Console.Write($"\rProcessing file {i + 1:N0}/{_filesOnDisk.Count:N0}...");
            try
            {
                var fd = _analyzer.Analyze(path);
                if (fd != null) details.Add(fd);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error analyzing {path}: {ex.Message}");
                errorCount++;
            }
        }
        Console.WriteLine();

        var diskContent = new DiskContent
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = details,
            FileCount = details.Count,
            TotalFileSize = details.Sum(f => f.SizeInBytes)
        };

        _exporter.SaveToJson(diskContent, _configuration.FileList);
        _logger.LogInfo($"Initial file list saved: {_configuration.FileList}");
        _logger.LogInfo($"Successfully analyzed: {successCount}, errors: {errorCount}");
    }
    private void AddNewFilesOnDisk()
    {
        _logger.LogInfo("Checking for files to add...");
        var fileList = _exporter.LoadFromJson(_configuration.FileList);
        var existingFilePaths = new HashSet<string>(fileList.FileDetails.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
        int addedCount = 0;

        for (int i = 0; i < _filesOnDisk.Count; i++)
        {
            var filePath = _filesOnDisk[i];
            Console.Write($"\rChecking file to add {i + 1:N0} of {_filesOnDisk.Count:N0}...");
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
                    _logger.LogError($"Error analyzing {filePath}: {ex.Message}");
                }
            }
        }
        Console.WriteLine();

        var updatedDiskContent = new DiskContent
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = fileList.FileDetails,
            FileCount = fileList.FileDetails.Count,
            TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes)
        };

        _exporter.SaveToJson(updatedDiskContent, _configuration.FileList);
        _logger.LogInfo($"Operation completed. Added {addedCount:N0} files.");
    }
    private void RemoveFilesNotOnDisk()
    {
        _logger.LogInfo("Checking for files to remove...");
        var fileList = _exporter.LoadFromJson(_configuration.FileList);
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
                Console.Write($"\rChecking file to remove {i + 1:N0} of {total:N0}...");
        }
        Console.WriteLine();

        if (removedFiles.Any())
        {
            _logger.LogInfo("Files removed from list because they are no longer present on disk:");
            foreach (var removed in removedFiles)
                _logger.LogInfo($"To remove: {removed.Path}");
        }

        fileList.FileDetails.RemoveAll(f => !filesOnDiskSet.Contains(f.Path));
        fileList.FileCount = fileList.FileDetails.Count;
        fileList.TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes);

        _exporter.SaveToJson(fileList, _configuration.FileList);
        _logger.LogInfo($"Operation completed. Removed {removedCount:N0} files.");
    }
}
