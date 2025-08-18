public class FileDetail
{
    public required string FilePath { get; set; }
    public required string Sha256 { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool HashCheked { get; set; }
    public DateTime LastHashCheckUtc { get; set; }
    public DateTime AddedUtc { get; set; }
}