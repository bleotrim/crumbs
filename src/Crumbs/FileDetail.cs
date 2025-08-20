public class FileDetail
{
    public required string Path { get; set; }
    public required string Sha256 { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public DateTime LastHashCheckUtc { get; set; }
    public DateTime AddedUtc { get; set; }
}