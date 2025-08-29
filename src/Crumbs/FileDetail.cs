public class FileDetail
{
    public required string Path { get; set; }
    public required string Sha256 { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime CheckedUtc { get; set; }
    public DateTime AddedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}