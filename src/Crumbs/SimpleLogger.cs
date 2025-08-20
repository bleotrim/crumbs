public class SimpleLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();
    public SimpleLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
    }
    public void LogInfo(string message)
    {
        Log("INFO", message);
    }
    public void LogError(string message)
    {
        Log("ERROR", message);
    }
    private void Log(string level, string message)
    {
        var logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_lock)
        {
            Console.WriteLine(logLine);
            File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
        }
    }
}
