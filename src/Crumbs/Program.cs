class Program
{
    static void Main()
    {
        Configuration conf = new Configuration();

        CreateFileListIfNotExist(conf);
        AddNewFilesOnDisk(conf);
        RemoveFilesNotOnDisk(conf);
    }

    public static void CreateFileListIfNotExist(Configuration configuration)
    {
        if (File.Exists(configuration.FilePath))
            return;

        var collector = new FileCollector();
        var analyzer = new FileAnalyzer();
        var exporter = new FileDetailExporter();

        // Get all files
        var filePaths = collector.GetFiles(@"/Users/leotrim/Downloads", "*.*", SearchOption.AllDirectories);

        var details = new List<FileDetail>();
        int successCount = 0;
        int errorCount = 0;

        foreach (var path in filePaths)
        {
            try
            {
                var fd = analyzer.Analyze(path);
                if (fd != null)
                {
                    details.Add(fd);
                }
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing {path}: {ex.Message}");
                errorCount++;
            }
        }

        var diskContentDetail = new DiskContentDetail()
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = details,
            FileCount = details.Count,
            TotalFileSize = details.Sum(f => f.SizeInBytes)
        };

        exporter.SaveToJson(diskContentDetail, configuration.FilePath);

        Console.WriteLine($"Save completed: {configuration.FilePath}");
        Console.WriteLine($"Successfully analyzed: {successCount}, errors: {errorCount}");
    }

    public static void AddNewFilesOnDisk(Configuration configuration)
    {
        var collector = new FileCollector();
        var analyzer = new FileAnalyzer();
        var exporter = new FileDetailExporter();

        // Load existing list
        var fileList = exporter.LoadFromJson(configuration.FilePath);

        // Prepare HashSet for fast lookup
        var existingFilePaths = new HashSet<string>(fileList.FileDetails.Select(f => f.FilePath));

        // Get current files on disk
        var filesOnDisk = collector.GetFiles(@"/Users/leotrim/Downloads", "*.*", SearchOption.AllDirectories);

        int addedCount = 0;

        foreach (var filePath in filesOnDisk)
        {
            Console.Write($"{filesOnDisk.IndexOf(filePath) + 1} of {filesOnDisk.Count}\r");
            if (!existingFilePaths.Contains(filePath))
            {
                var fd = analyzer.Analyze(filePath);
                if (fd != null)
                {
                    fileList.FileDetails.Add(fd);
                    existingFilePaths.Add(filePath); // Update HashSet
                }

                //Console.WriteLine($"Added: {filePath}");
                addedCount++;
            }
        }

        if (addedCount == 0)
            Console.WriteLine("No new files to add.");

        // Update DiskContentDetail object (you can add real hardware info here)
        var hddContentDetail = new DiskContentDetail()
        {
            SerialNumber = "serial",
            PartNumber = "part",
            FileDetails = fileList.FileDetails,
            FileCount = fileList.FileDetails.Count,
            TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes)
        };

        // Save to JSON
        exporter.SaveToJson(hddContentDetail, configuration.FilePath);

        if (addedCount != 0)
            Console.WriteLine($"Updated JSON: {configuration.FilePath} (Added {addedCount} new files)");
    }

    public static void RemoveFilesNotOnDisk(Configuration configuration)
    {
        var collector = new FileCollector();
        var exporter = new FileDetailExporter();

        // Load list from JSON
        var fileList = exporter.LoadFromJson(configuration.FilePath);

        // Get current files on disk
        var collectedFiles = collector.GetFiles(@"/Users/leotrim/Downloads", "*.*", SearchOption.AllDirectories);

        // Find files in fileList that are no longer present on disk
        var removedFiles = fileList.FileDetails.Where(f => !collectedFiles.Contains(f.FilePath)).ToList();

        // Show removed files
        if (removedFiles.Any())
        {
            Console.WriteLine("Files removed from list because they are no longer present on disk:");
            foreach (var removed in removedFiles)
            {
                Console.WriteLine($"- {removed.FilePath}");
            }
        }

        // Remove files that are no longer on disk
        fileList.FileDetails.RemoveAll(f => !collectedFiles.Contains(f.FilePath));
        fileList.FileCount = fileList.FileDetails.Count;
        fileList.TotalFileSize = fileList.FileDetails.Sum(f => f.SizeInBytes);

        // Update JSON
        exporter.SaveToJson(fileList, configuration.FilePath);
    }
}
