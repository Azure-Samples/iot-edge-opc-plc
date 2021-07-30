namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using OpcPlc.Helpers;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Nodes with deterministic GUIDs as ID.
    /// </summary>
    public class DeterministicGuidNodes : INodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; } = new List<string>();

        private static uint NodeCount { get; set; } = 1;
        private uint NodeRate { get; set; } = 1000; // ms.
        private NodeType NodeType { get; set; } = NodeType.UInt;

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint>[] _nodes;

        public void AddOption(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "gn|guidnodes=",
                $"number of nodes with deterministic GUID IDs\nDefault: {NodeCount}",
                (uint i) => NodeCount = i);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            FolderState folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Deterministic GUIDs",
                name: "Deterministic GUIDs",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder);
        }

        public void StartSimulation()
        {
            foreach (var node in _nodes)
            {
                node.Start(value => value + 1, periodMs: 1000);
            }
        }

        public void StopSimulation()
        {
            foreach (var node in _nodes)
            {
                node.Stop();
            }
        }

        private void AddNodes(FolderState folder)
        {
            _nodes = new SimulatedVariableNode<uint>[NodeCount];
            var nodeIDs = new List<string>((int)NodeCount);

            if (NodeCount > 0)
            {
                Logger.Information($"Creating {NodeCount} GUID node(s) of type: {NodeType}");
                Logger.Information($"Node values will change every {NodeRate} ms");
            }

            for (int i = 0; i < NodeCount; i++)
            {
                string id = DeterministicGuid.NewGuid().ToString();

                _nodes[i] = _plcNodeManager.CreateVariableNode<uint>(
                    _plcNodeManager.CreateBaseVariable(
                        folder,
                        path: id,
                        name: id,
                        new NodeId((uint)BuiltInType.UInt32),
                        ValueRanks.Scalar,
                        AccessLevels.CurrentReadOrWrite,
                        "Constantly increasing value",
                        NamespaceType.OpcPlcApplications,
                        defaultValue: (uint)0));

                nodeIDs.Add(id);
            }

            NodeIDs = nodeIDs;
        }
    }
}
