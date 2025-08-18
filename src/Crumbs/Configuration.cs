using Microsoft.Extensions.Configuration;

public class Configuration
{
    public string FilePath { get; set; } = "no file path available";

    public Configuration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        FilePath = configuration["Configuration:FilePath"] ?? FilePath;
    }
}