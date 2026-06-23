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

    public Dictionary<string, ThingActionInfo> Actions { get; set; } = new();
}

/// <summary>
/// Single action entry inside a <see cref="ThingDescriptionInfo"/>. Maps onto an
/// OPC UA Method per OPC 10100-1 §6.3.9.
/// </summary>
internal sealed class ThingActionInfo
{
    public string Name { get; set; }

    public string Description { get; set; }

    /// <summary>Arguments derived from the TD action's <c>input</c> JSON Schema. Empty when there is no input.</summary>
    public List<ThingArgumentInfo> Input { get; set; } = new();

    /// <summary>Arguments derived from the TD action's <c>output</c> JSON Schema. Empty when there is no output.</summary>
    public List<ThingArgumentInfo> Output { get; set; } = new();
}

/// <summary>
/// One input or output argument derived from a TD action's <c>input</c> / <c>output</c>
/// JSON Schema. Carries the same primitive shape vocabulary as <see cref="ThingPropertyInfo"/>
/// (type / format / itemsType) so the property-side type mapping can be reused.
/// </summary>
internal sealed class ThingArgumentInfo
{
    public string Name { get; set; }

    /// <summary>JSON Schema primitive type ("string", "number", "integer", "boolean", "array").</summary>
    public string Type { get; set; }

    /// <summary>Optional JSON Schema format hint, e.g. "date-time" for ISO-8601 timestamps.</summary>
    public string Format { get; set; }

    public string Description { get; set; }

    /// <summary>For TD <c>type: "array"</c>, the JSON type of the element from <c>items.type</c>.</summary>
    public string ItemsType { get; set; }
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

        var actions = new Dictionary<string, ThingActionInfo>();

        if (root.TryGetProperty("actions", out var actionsElement) && actionsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var act in actionsElement.EnumerateObject())
            {
                var actionName = act.Name;
                var actionValue = act.Value;

                if (actionValue.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var actionInfo = new ThingActionInfo { Name = actionName };

                if (actionValue.TryGetProperty("description", out var actDescElement) && actDescElement.ValueKind == JsonValueKind.String)
                {
                    actionInfo.Description = actDescElement.GetString();
                }

                if (actionValue.TryGetProperty("input", out var inputElement) && inputElement.ValueKind == JsonValueKind.Object)
                {
                    actionInfo.Input = SchemaToArguments(inputElement);
                }

                if (actionValue.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Object)
                {
                    actionInfo.Output = SchemaToArguments(outputElement);
                }

                actions[actionName] = actionInfo;
            }
        }

        return new ThingDescriptionInfo
        {
            Name = assetName,
            Properties = properties,
            Actions = actions,
        };
    }

    /// <summary>
    /// Flattens a TD action <c>input</c> or <c>output</c> JSON Schema into a list of
    /// argument descriptors. Two shapes are recognised today (per OPC 10100-1 §6.3.9):
    /// <list type="bullet">
    /// <item><description>A JSON Schema <c>object</c> with <c>properties</c> — one argument
    /// per property, keyed by the property name.</description></item>
    /// <item><description>A top-level primitive schema (e.g. <c>{ "type": "integer" }</c>) —
    /// becomes a single argument named <c>value</c>.</description></item>
    /// </list>
    /// Nested object / structured types defer to the structured-types plan item.
    /// </summary>
    private static List<ThingArgumentInfo> SchemaToArguments(JsonElement schema)
    {
        var args = new List<ThingArgumentInfo>();

        var schemaType = schema.TryGetProperty("type", out var schemaTypeElement) &&
                         schemaTypeElement.ValueKind == JsonValueKind.String
            ? schemaTypeElement.GetString()
            : null;

        if (string.Equals(schemaType, "object", System.StringComparison.Ordinal) &&
            schema.TryGetProperty("properties", out var propsElement) &&
            propsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                args.Add(SchemaToArgument(prop.Name, prop.Value));
            }

            return args;
        }

        // Top-level primitive (or unknown) schema — surface a single "value" argument.
        if (!string.IsNullOrEmpty(schemaType))
        {
            args.Add(SchemaToArgument("value", schema));
        }

        return args;
    }

    private static ThingArgumentInfo SchemaToArgument(string name, JsonElement schema)
    {
        var arg = new ThingArgumentInfo { Name = name };

        if (schema.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
        {
            arg.Type = typeElement.GetString();
        }

        if (schema.TryGetProperty("format", out var formatElement) && formatElement.ValueKind == JsonValueKind.String)
        {
            arg.Format = formatElement.GetString();
        }

        if (schema.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
        {
            arg.Description = descElement.GetString();
        }

        if (schema.TryGetProperty("items", out var itemsElement) &&
            itemsElement.ValueKind == JsonValueKind.Object &&
            itemsElement.TryGetProperty("type", out var itemsTypeElement) &&
            itemsTypeElement.ValueKind == JsonValueKind.String)
        {
            arg.ItemsType = itemsTypeElement.GetString();
        }

        return arg;
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

    /// <summary>
    /// Resolves an OPC UA (DataType, ValueRank) pair for a TD action argument. Same vocabulary
    /// as <see cref="GetUaType(ThingPropertyInfo)"/>; kept as a separate overload so the two
    /// info DTOs stay decoupled.
    /// </summary>
    public static (NodeId DataType, int ValueRank) GetUaType(ThingArgumentInfo argument)
    {
        var jsonType = argument?.Type?.ToLowerInvariant();

        if (string.Equals(jsonType, "array", System.StringComparison.Ordinal))
        {
            return (GetBuiltInTypeFromJson(argument?.ItemsType), ValueRanks.OneDimension);
        }

        if (string.Equals(jsonType, "string", System.StringComparison.Ordinal) &&
            string.Equals(argument?.Format, "date-time", System.StringComparison.OrdinalIgnoreCase))
        {
            return (DataTypeIds.DateTime, ValueRanks.Scalar);
        }

        return (GetBuiltInTypeFromJson(jsonType), ValueRanks.Scalar);
    }
}
