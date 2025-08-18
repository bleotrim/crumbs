using System.Text.Json;
using System.Text.Json.Serialization;
public class FileDetailExporter
{
    /// <summary>
    /// Saves a list of FileDetail to a JSON file.
    /// </summary>
    public void SaveToJson(DiskContentDetail files, string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // for a human-readable JSON
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(files, options);

        File.WriteAllText(outputPath, json);
    }
    /// <summary>
    /// Loads a list of FileDetail from a JSON file.
    /// </summary>
    public DiskContentDetail LoadFromJson(string inputPath)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"The file {inputPath} does not exist.");

        string json = File.ReadAllText(inputPath);
        var result = JsonSerializer.Deserialize<DiskContentDetail>(json);
        if (result == null)
            throw new InvalidOperationException("Deserialization returned null.");
        return result;
    }
}
