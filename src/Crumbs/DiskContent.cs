public class DiskContent
{
    public required string PartitionId { get; set; }
    public long FileCount { get; set; }
    public long TotalFileSize { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public required List<FileDetail> FileDetails { get; set; }
}