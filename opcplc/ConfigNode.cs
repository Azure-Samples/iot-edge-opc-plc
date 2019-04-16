using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcPlc
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigFolder
    {
        public ushort NamespaceId { get; set; }

        public string Name { get; set; }

        public List<ConfigNode> NodeList { get; set; }
    }

    /// <summary>
    /// Used to define the node, which will be exposed by server.
    /// </summary>
    public class ConfigNode
    {
        public string NodeId { get; set; }

        public string Name { get; set; }

        public string DataType { get; set; }

        public int ValueRank { get; set; }

        public string AccessLevel { get; set; }

        public string Description { get; set; }

    }

    public enum AccessLevel
    {
        None = 0,
        CurrentRead = 1,
        CurrentWrite = 2,
        CurrentReadOrWrite = 3,
        HistoryRead = 4,
        HistoryWrite = 8,
        HistoryReadOrWrite = 12,
        SemanticChange = 16,
        StatusWrite = 32,
        TimestampWrite = 64,
    }
}
