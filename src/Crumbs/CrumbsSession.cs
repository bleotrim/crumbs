public class CrumbsSession
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; } = null;
    public bool IsSuccessful { get; set; } = false;
    public AddingSession? AddingSession { get; set; } = null;
    public RemovingSession? RemovingSession { get; set; } = null;
    public UpdateSession? UpdateSession { get; set; } = null;
}

public class UpdateSession
{
    public bool IsSuccessful { get; set; } = false;
    public List<FileModificationDetail> ModifiedFiles { get; set; } = new();
    public List<Error> Errors { get; set; } = new();
}

public class FileModificationDetail
{
    public required FileDetail Previous { get; set; }
    public required FileDetail Current { get; set; }
}

public class AddingSession
{
    public bool IsSuccessful { get; set; } = false;
    public List<FileDetail> AddedFiles { get; set; } = new();
    public List<Error> Errors { get; set; } = new();
}

public class RemovingSession
{
    public bool IsSuccessful { get; set; } = false;
    public List<FileDetail> RemovedFiles { get; set; } = new();
}

public class Error
{
    public string? FilePath { get; set; }
    public required string Message { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
}