namespace OpcPlc.PluginNodes.Models
{
    public class NodeWithIntervals
    {
        public string NodeId { get; set; }
        public uint PublishingInterval { get; set; }
        public uint SamplingInterval { get; set; }
    }
}
