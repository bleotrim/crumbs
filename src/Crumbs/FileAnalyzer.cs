using System.Security.Cryptography;
public class FileAnalyzer
{
    private readonly SimpleLogger _logger;
    public FileAnalyzer(SimpleLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Converts a file path into a FileDetail object.
    /// </summary>
    public FileDetail? Analyze(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

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
                LastHashCheckUtc = null,
                AddedUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }
    private string? ComputeSha256(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sha = SHA256.Create();

            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }
}
