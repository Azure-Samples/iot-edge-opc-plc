namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using OpcPlc.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using static OpcPlc.Program;

    public class DeterministicGuidNodes : INodes<uint>
    {
        // Command line option.
        public string Prototype { get; } = "gn|guidnodes=";
        public string Description { get; } = $"number of nodes with deterministic GUID IDs\nDefault: {NodeCount}";
        public Action<uint> Action { get; } = (uint i) => NodeCount = i;
        public bool IsEnabled { get => NodeCount > 0; }
        public IReadOnlyCollection<string> NodeIDs { get; private set; }

        // Node count, rate and type.
        private static uint NodeCount { get; set; } = 1;
        private uint NodeRate { get; set; } = 1000; // ms.
        private NodeType NodeType { get; set; } = NodeType.UInt;

        private PlcNodeManager _plcNodeManager;
        private BaseDataVariableState[] _nodes;
        private ITimer _timer;

        public void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            FolderState folder = _plcNodeManager.CreateFolder(
                parentFolder,
                path: "Deterministic GUIDs",
                name: "Deterministic GUIDs",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder);
        }

        public void StartSimulation(PlcServer server)
        {
            if (NodeCount > 0)
            {
                _timer = server.TimeService.NewTimer(UpdateNodes, NodeRate);
            }
        }

        public void StopSimulation()
        {
            if (_timer != null)
            {
                _timer.Enabled = false;
            }
        }

        private void UpdateNodes(object state, ElapsedEventArgs elapsedEventArgs)
        {
            if (_nodes != null)
            {
                _plcNodeManager.UpdateNodes(_nodes, NodeType, StatusCodes.Good, addBadValue: false);
            }
        }

        private void AddNodes(FolderState folder)
        {
            _nodes = new BaseDataVariableState[NodeCount];
            var nodeIDs = new List<string>((int)NodeCount);

            if (NodeCount > 0)
            {
                Logger.Information($"Creating {NodeCount} GUID node(s) of type: {NodeType}");
                Logger.Information($"Node values will change every {NodeRate} ms");
            }

            for (int i = 0; i < NodeCount; i++)
            {
                var (dataType, valueRank, defaultValue, stepTypeSize, minTypeValue, maxTypeValue) =
                    PlcNodeManager.GetNodeType(NodeType, stepSize: "1", minValue: null, maxValue: null);

                string id = DeterministicGuid.NewGuid().ToString();
                _nodes[i] = _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: id,
                    name: id,
                    dataType,
                    valueRank,
                    AccessLevels.CurrentReadOrWrite,
                    "Constantly increasing value(s)",
                    NamespaceType.OpcPlcApplications,
                    randomize: false,
                    stepTypeSize,
                    minTypeValue,
                    maxTypeValue,
                    defaultValue);

                nodeIDs.Add(id);
            }

            NodeIDs = nodeIDs;
        }
    }
}
