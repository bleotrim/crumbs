using System.Security.Cryptography;

public class FileAnalyzer
{
    private readonly SimpleLogger _logger;

    public FileAnalyzer(SimpleLogger logger) 
        => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public FileDetail AnalyzeNewFile(string filePath, bool computeHash = true)
        => AnalyzeFile(filePath, addedUtc: DateTime.UtcNow, computeHash);

    public FileDetail AnalyzeModifiedFile(FileDetail previous)
        => AnalyzeFile(previous.Path, addedUtc: previous.AddedUtc, computeHash: true, previous);

    private FileDetail AnalyzeFile(string path, DateTime addedUtc, bool computeHash, FileDetail? previous = null)
    {
        var fileInfo = new FileInfo(path);
        var now = DateTime.UtcNow;

        return new FileDetail
        {
            Path = fileInfo.FullName,
            Sha256 = computeHash ? ComputeSha256(path) : string.Empty,
            SizeInBytes = fileInfo.Length,
            Created = fileInfo.CreationTimeUtc,
            Modified = fileInfo.LastWriteTimeUtc,
            CheckedUtc = now,
            AddedUtc = addedUtc,
            UpdatedUtc = previous != null ? now : null
        };
    }

    private string ComputeSha256(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
