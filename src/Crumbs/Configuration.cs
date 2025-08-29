using Microsoft.Extensions.Configuration;

public class Configuration
{
    public string FileList { get; set; } = "no file path available";
    public string LogFile { get; set; } = "";
    public string SessionFolder { get; set; } = "";

    public Configuration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        FileList = configuration["Configuration:Crumbs:FileList"] ?? FileList;
        LogFile = configuration["Configuration:Crumbs:LogFile"] ?? LogFile;
        SessionFolder = configuration["Configuration:Crumbs:SessionFolder"] ?? LogFile;
    }
}