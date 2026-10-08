namespace OpcPlc.PluginNodes;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Defines the configuration folder, which holds the list of nodes.
/// </summary>
public class ConfigFolder
{
    public string Folder { get; set; }

    public List<ConfigFolder> FolderList { get; set; }

    public List<ConfigNode> NodeList { get; set; }
}

/// <summary>
/// Used to define the node, which will be published by the server.
/// </summary>
public class ConfigNode
{
    private object _nodeId;

    [JsonRequired]
    [JsonConverter(typeof(ConfigValueConverter))]
    public dynamic NodeId
    {
        get => _nodeId;
        set => _nodeId = value ?? throw new JsonException("NodeId must not be null.");
    }

    public string Name { get; set; }

    public string DataType { get; set; } = "Int32";

    public int ValueRank { get; set; } = -1;

    public string AccessLevel { get; set; } = "CurrentReadOrWrite";

    public string Description { get; set; }

    [JsonConverter(typeof(ConfigNodeValueConverter))]
    public object Value { get; set; }
}

internal sealed class ConfigNodeValueConverter : ConfigValueConverter
{
    public override object Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && !reader.TryGetInt64(out _) && !reader.TryGetUInt64(out _))
        {
            // Preserve decimal/exponent tokens until the configured target type is known.
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }
        return base.Read(ref reader, typeToConvert, options);
    }
}

internal class ConfigValueConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long value))
                {
                    return value;
                }
                return reader.TryGetUInt64(out ulong unsignedValue) ? (object)unsignedValue : reader.GetDouble();
            case JsonTokenType.String:
                return reader.GetString();
            default:
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.Clone();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
