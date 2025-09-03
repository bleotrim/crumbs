using System;
using System.Collections.Generic;
using System.IO;

public class FileCollector
{
    private readonly SimpleLogger _logger;

    public FileCollector(SimpleLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns a list of files from a folder, ignoring inaccessible paths.
    /// </summary>
    /// <param name="directoryPath">Folder path</param>
    /// <param name="searchPattern">Search pattern (e.g. "*.txt", "*.jpg", "*.*")</param>
    /// <param name="searchOption">Search options (TopDirectoryOnly or AllDirectories)</param>
    /// <returns>List of full paths of the found files</returns>
    public List<string> GetFiles(string directoryPath, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var files = new List<string>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogError($"The specified folder does not exist: {directoryPath}");
            return files;
        }

        try
        {
            CollectFiles(directoryPath, searchPattern, searchOption, files);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error while collecting files: {ex.Message}");
        }

        return files;
    }

    private void CollectFiles(string directoryPath, string searchPattern, SearchOption searchOption, List<string> files)
    {
        try
        {
            // aggiungi i file della cartella corrente
            files.AddRange(Directory.GetFiles(directoryPath, searchPattern));
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError($"Access denied to directory: {directoryPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error accessing files in {directoryPath}: {ex.Message}");
        }

        if (searchOption == SearchOption.AllDirectories)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(directoryPath))
                {
                    CollectFiles(subDir, searchPattern, searchOption, files);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogError($"Access denied to subdirectory: {directoryPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error accessing subdirectories in {directoryPath}: {ex.Message}");
            }
        }
    }
}
