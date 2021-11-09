namespace OpcPlc;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

/// <summary>
/// Defines the configuration folder, which holds the list of nodes.
/// </summary>
public class ConfigFolder
{
    public string Folder { get; set; }

    public List<ConfigNode> NodeList { get; set; }
}

/// <summary>
/// Used to define the node, which will be published by the server.
/// </summary>
public class ConfigNode
{
    [JsonProperty(Required = Required.Always)]
    public dynamic NodeId { get; set; }

    public string Name { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue("Int32")]
    public string DataType { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue(-1)]
    public int ValueRank { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue("CurrentReadOrWrite")]
    public string AccessLevel { get; set; }

    public string Description { get; set; }

    public object Value { get; set; }
}
