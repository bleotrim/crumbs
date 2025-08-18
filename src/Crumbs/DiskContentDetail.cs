public class DiskContentDetail
{
    public required string SerialNumber { get; set; }
    public required string PartNumber { get; set; }
    public long FileCount { get; set; }
    public long TotalFileSize { get; set; }
    public required List<FileDetail> FileDetails { get; set; }
}