using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonExporter<T>
{
    private readonly SimpleLogger _logger;

    public JsonExporter(SimpleLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Serializza un oggetto di tipo T e lo salva su file JSON.
    /// </summary>
    public void SaveToJson(T data, string outputPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        try
        {
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(outputPath, json);
            _logger.LogInfo($"JSON saved successfully to {outputPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving JSON to {outputPath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Carica e deserializza un file JSON in un oggetto di tipo T.
    /// </summary>
    public T LoadFromJson(string inputPath)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"The file {inputPath} does not exist.");

        try
        {
            string json = File.ReadAllText(inputPath);
            var result = JsonSerializer.Deserialize<T>(json);

            if (result == null)
                throw new InvalidOperationException("Deserialization returned null.");

            _logger.LogInfo($"JSON loaded successfully from {inputPath}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading JSON from {inputPath}: {ex.Message}");
            throw;
        }
    }
}
