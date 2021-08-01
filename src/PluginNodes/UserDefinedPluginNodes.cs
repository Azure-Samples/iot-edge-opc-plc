namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using static OpcPlc.Program;
    using Newtonsoft.Json;

    /// <summary>
    /// Nodes that are configuration via JSON file.
    /// </summary>
    public class UserDefinedPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; } = new List<string>();

        private static string _nodesFileName;
        private PlcNodeManager _plcNodeManager;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "nf|nodesfile=",
                "the filename which contains the list of nodes to be created in the OPC UA address space.",
                (string f) => _nodesFileName = f);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (!string.IsNullOrEmpty(_nodesFileName))
            {
                AddNodes((FolderState)telemetryFolder.Parent); // Root.
            }
        }

        public void StartSimulation()
        {
        }

        public void StopSimulation()
        {
        }

        private void AddNodes(FolderState folder)
        {
            if (!File.Exists(_nodesFileName))
            {
                string error = $"The user node configuration file {_nodesFileName} does not exist.";
                Logger.Error(error);
                throw new Exception(error);
            }

            var nodeIDs = new List<string>();
            string json = File.ReadAllText(_nodesFileName);

            var cfgFolder = JsonConvert.DeserializeObject<ConfigFolder>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            });

            Logger.Information($"Processing node information configured in {_nodesFileName}");
            Logger.Debug($"Create folder {cfgFolder.Folder}");
            FolderState userNodesFolder = _plcNodeManager.CreateFolder(
                folder,
                cfgFolder.Folder,
                cfgFolder.Folder,
                NamespaceType.OpcPlcApplications);

            foreach (var node in cfgFolder.NodeList)
            {
                if (node.NodeId.GetType() != Type.GetType("System.Int64") && node.NodeId.GetType() != Type.GetType("System.String"))
                {
                    Logger.Error($"The type of the node configuration for node with name {node.Name} ({node.NodeId.GetType()}) is not supported. Only decimal and string are supported. Default to string.");
                    node.NodeId = node.NodeId.ToString();
                }

                string typedNodeId = $"{(node.NodeId.GetType() == Type.GetType("System.Int64") ? "i=" : "s=")}{node.NodeId.ToString()}";
                if (string.IsNullOrEmpty(node.Name))
                {
                    node.Name = typedNodeId;
                }

                if (string.IsNullOrEmpty(node.Description))
                {
                    node.Description = node.Name;
                }

                Logger.Debug($"Create node with Id '{typedNodeId}' and BrowseName '{node.Name}' in namespace with index '{_plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]}'");
                CreateBaseVariable(userNodesFolder, node);
                nodeIDs.Add(node.NodeId.ToString());
            }

            Logger.Information("Processing node information completed.");

            NodeIDs = nodeIDs;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        public void CreateBaseVariable(NodeState parent, ConfigNode node)
        {
            if (!Enum.TryParse(node.DataType, out BuiltInType nodeDataType))
            {
                Logger.Error($"Value '{node.DataType}' of node '{node.NodeId}' cannot be parsed. Defaulting to 'Int32'");
                node.DataType = "Int32";
            }

            // We have to hard code conversion here, because AccessLevel is defined as byte in OPCUA lib.
            byte accessLevel;
            try
            {
                accessLevel = (byte)(typeof(AccessLevels).GetField(node.AccessLevel).GetValue(null));
            }
            catch
            {
                Logger.Error($"AccessLevel '{node.AccessLevel}' of node '{node.Name}' is not supported. Defaulting to 'CurrentReadOrWrite'");
                node.AccessLevel = "CurrentRead";
                accessLevel = AccessLevels.CurrentReadOrWrite;
            }

            _plcNodeManager.CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, NamespaceType.OpcPlcApplications);
        }
    }
}
