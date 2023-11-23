namespace OpcPlc.DeterministicAlarms.Configuration;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Configuration
{
    static JsonSerializerOptions fromJsonOptions = new JsonSerializerOptions {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    static JsonSerializerOptions toJsonOptions = new JsonSerializerOptions {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public List<Folder> Folders { get; set; }

    public Script Script { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, toJsonOptions);
    }

    public static Configuration FromJson(string json)
    {
        return JsonSerializer.Deserialize<Configuration>(json, fromJsonOptions);
    }
}
