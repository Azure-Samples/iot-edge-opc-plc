namespace OpcPlc.PluginNodes.Models;

public class NodeWithIntervals
{
    public string NodeId { get; set; }
    public string NodeIdTypePrefix { get; set; } = "s"; // Default type is string.
    public string Namespace { get; set; }
    public uint PublishingInterval { get; set; }
    public uint SamplingInterval { get; set; }
}
