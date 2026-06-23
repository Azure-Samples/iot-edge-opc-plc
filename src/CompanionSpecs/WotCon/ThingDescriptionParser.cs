// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Metadata extracted from a W3C Thing Description JSON-LD document.
/// </summary>
internal sealed class ThingDescriptionInfo
{
    public string Name { get; set; }

    public Dictionary<string, ThingPropertyInfo> Properties { get; set; } = new();
}

/// <summary>
/// Single property entry inside a <see cref="ThingDescriptionInfo"/>.
/// </summary>
internal sealed class ThingPropertyInfo
{
    public string Name { get; set; }

    /// <summary>JSON Schema primitive type ("string", "number", "integer", "boolean", "array", "object").</summary>
    public string Type { get; set; }

    /// <summary>Optional JSON Schema format hint, e.g. "date-time" for ISO-8601 timestamps.</summary>
    public string Format { get; set; }

    public string Description { get; set; }

    /// <summary>TD <c>unit</c> field — surfaces as an OPC UA <c>EngineeringUnits</c> property.</summary>
    public string Unit { get; set; }

    /// <summary>True when TD declares <c>readOnly: true</c> — maps to <c>AccessLevel.CurrentRead</c> only.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>True when TD declares <c>writeOnly: true</c> — maps to <c>AccessLevel.CurrentWrite</c> only.</summary>
    public bool WriteOnly { get; set; }

    /// <summary>TD <c>observable</c> field (defaults to <c>true</c> per WoT spec).</summary>
    public bool Observable { get; set; } = true;

    /// <summary>For TD <c>type: "array"</c>, the JSON type of the element from <c>items.type</c>.</summary>
    public string ItemsType { get; set; }
}

/// <summary>
/// Pure helpers for parsing W3C Thing Description JSON-LD documents and mapping their
/// primitive JSON types onto OPC UA built-in data types.
/// </summary>
internal static class ThingDescriptionParser
{
    /// <summary>
    /// Parses a Thing Description JSON-LD document and extracts the asset name plus its
    /// declared <c>properties</c>. Returns <c>null</c> when the payload is well-formed JSON
    /// but is missing a non-empty <c>title</c>; lets <see cref="JsonException"/> propagate
    /// so callers can distinguish malformed JSON (<c>Bad_DecodingError</c>) from semantic
    /// failure (<c>Bad_InvalidArgument</c>).
    /// </summary>
    public static ThingDescriptionInfo Parse(string json, ILogger logger = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("title", out var titleElement) || titleElement.ValueKind != JsonValueKind.String)
        {
            logger?.LogWarning("[WotCon] Thing Description missing 'title' field");
            return null;
        }

        var assetName = titleElement.GetString();
        if (string.IsNullOrWhiteSpace(assetName))
        {
            logger?.LogWarning("[WotCon] Thing Description title is empty");
            return null;
        }

        var properties = new Dictionary<string, ThingPropertyInfo>();

        if (root.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propertiesElement.EnumerateObject())
            {
                var propName = prop.Name;
                var propValue = prop.Value;

                if (propValue.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var propertyInfo = new ThingPropertyInfo { Name = propName };

                if (propValue.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                {
                    propertyInfo.Type = typeElement.GetString();
                }

                if (propValue.TryGetProperty("format", out var formatElement) && formatElement.ValueKind == JsonValueKind.String)
                {
                    propertyInfo.Format = formatElement.GetString();
                }

                if (propValue.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
                {
                    propertyInfo.Description = descElement.GetString();
                }

                if (propValue.TryGetProperty("unit", out var unitElement) && unitElement.ValueKind == JsonValueKind.String)
                {
                    propertyInfo.Unit = unitElement.GetString();
                }

                if (propValue.TryGetProperty("readOnly", out var roElement) && roElement.ValueKind == JsonValueKind.True)
                {
                    propertyInfo.ReadOnly = true;
                }

                if (propValue.TryGetProperty("writeOnly", out var woElement) && woElement.ValueKind == JsonValueKind.True)
                {
                    propertyInfo.WriteOnly = true;
                }

                // TD "observable" defaults to true; only flip it off when the payload explicitly says false.
                if (propValue.TryGetProperty("observable", out var obsElement) && obsElement.ValueKind == JsonValueKind.False)
                {
                    propertyInfo.Observable = false;
                }

                if (propValue.TryGetProperty("items", out var itemsElement) &&
                    itemsElement.ValueKind == JsonValueKind.Object &&
                    itemsElement.TryGetProperty("type", out var itemsTypeElement) &&
                    itemsTypeElement.ValueKind == JsonValueKind.String)
                {
                    propertyInfo.ItemsType = itemsTypeElement.GetString();
                }

                properties[propName] = propertyInfo;
            }
        }

        return new ThingDescriptionInfo
        {
            Name = assetName,
            Properties = properties,
        };
    }

    /// <summary>
    /// Maps the W3C WoT primitive JSON type names onto OPC UA built-in data types. Anything
    /// unrecognised (including <c>object</c> and <c>array</c>) currently falls back to
    /// <see cref="DataTypeIds.String"/> until structured-type support lands.
    /// </summary>
    public static NodeId GetBuiltInTypeFromJson(string jsonType)
    {
        return jsonType?.ToLowerInvariant() switch
        {
            "number" => DataTypeIds.Double,
            "integer" => DataTypeIds.Int32,
            "boolean" => DataTypeIds.Boolean,
            "string" => DataTypeIds.String,
            _ => DataTypeIds.String,
        };
    }

    /// <summary>
    /// Resolves an OPC UA (DataType, ValueRank) pair for a TD property. Honours <c>type: "array"</c>
    /// (one-dimensional, element type from <c>items.type</c>) and <c>type: "string", format: "date-time"</c>
    /// (mapped to <see cref="DataTypeIds.DateTime"/>). Everything else falls through to
    /// <see cref="GetBuiltInTypeFromJson"/> at scalar rank.
    /// </summary>
    public static (NodeId DataType, int ValueRank) GetUaType(ThingPropertyInfo property)
    {
        var jsonType = property?.Type?.ToLowerInvariant();

        if (string.Equals(jsonType, "array", System.StringComparison.Ordinal))
        {
            return (GetBuiltInTypeFromJson(property?.ItemsType), ValueRanks.OneDimension);
        }

        if (string.Equals(jsonType, "string", System.StringComparison.Ordinal) &&
            string.Equals(property?.Format, "date-time", System.StringComparison.OrdinalIgnoreCase))
        {
            return (DataTypeIds.DateTime, ValueRanks.Scalar);
        }

        return (GetBuiltInTypeFromJson(jsonType), ValueRanks.Scalar);
    }
}
