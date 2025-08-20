using System.Security.Cryptography;

public class FileAnalyzer
{
    /// <summary>
    /// Converts a file path into a FileDetail object.
    /// </summary>
    public FileDetail? Analyze(string filePath)
    {
        try
        {
            var fi = new FileInfo(filePath);
            var hash = ComputeSha256(filePath);

            if (hash == null)
                return null; // skip file if not readable

            return new FileDetail
            {
                Path = fi.FullName,
                Sha256 = hash,
                SizeInBytes = fi.Length,
                Created = fi.CreationTimeUtc,
                Modified = fi.LastWriteTimeUtc,
                LastHashCheckUtc = DateTime.UtcNow,
                AddedUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing {filePath}: {ex.Message}");
            return null;
        }
    }
    public static string? ComputeSha256(string filePath)
    {
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"File in use, unable to compute hash: {filePath} ({ioEx.Message})");
            return null;
        }
        catch (UnauthorizedAccessException unAuthEx)
        {
            Console.WriteLine($"Access denied: {filePath} ({unAuthEx.Message})");
            return null;
        }
    }
}
