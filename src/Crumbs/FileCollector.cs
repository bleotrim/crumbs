public class FileCollector
{
    /// <summary>
    /// Returns a list of files from a folder.
    /// </summary>
    /// <param name="directoryPath">Folder path</param>
    /// <param name="searchPattern">Search pattern (e.g. "*.txt", "*.jpg", "*.*")</param>
    /// <param name="searchOption">Search options (TopDirectoryOnly or AllDirectories)</param>
    /// <returns>List of full paths of the found files</returns>
    public List<string> GetFiles(string directoryPath, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        List<string> files = new List<string>();

        try
        {
            if (Directory.Exists(directoryPath))
            {
                files.AddRange(Directory.GetFiles(directoryPath, searchPattern, searchOption));
            }
            else
            {
                Console.WriteLine("The specified folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while searching for files: {ex.Message}");
        }

        return files;
    }
}
