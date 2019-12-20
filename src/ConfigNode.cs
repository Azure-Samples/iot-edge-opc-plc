using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcPlc
{
    /// <summary>
    /// Defines the configuration folder, which holds the list of nodes.
    /// </summary>
    public class ConfigFolder
    {
        public ushort NamespaceId { get; set; }

        public string Name { get; set; }

        public List<ConfigNode> NodeList { get; set; }
    }

    /// <summary>
    /// Used to define the node, which will be published by the server.
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
}
