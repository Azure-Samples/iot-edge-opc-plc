namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class LongIdNodes : INodes<string>
    {
        // Command line option.
        public string Prototype { get; } = "lid|longid";
        public string Description { get; } = $"add node with ID of 3950 chars.\nDefault: {_isEnabled}";
        public Action<string> Action { get; } = (string p) => _isEnabled = p != null;
        public bool IsEnabled { get => _isEnabled; }
        public IReadOnlyCollection<string> NodeIDs { get; private set; }

        private static bool _isEnabled;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _node;

        public void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (IsEnabled)
            {
                AddNodes(parentFolder);
            }
        }

        public void StartSimulation(PlcServer server)
        {
            if (IsEnabled)
            {
                _node.Start(value => value + 1, periodMs: 1000);
            }
        }

        public void StopSimulation()
        {
            if (IsEnabled)
            {
                _node.Stop();
            }
        }

        private void AddNodes(FolderState folder)
        {
            // Repeat A-Z until 3950 chars are collected.
            var id = new StringBuilder(4000);
            for (int i = 0; i < 3950; i++)
            {
                id.Append((char)(65 + (i % 26)));
            }

            _node = _plcNodeManager.CreateVariableNode<uint>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: id.ToString(),
                    name: "LongId3950",
                    new NodeId((uint)BuiltInType.UInt32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Constantly increasing value",
                    NamespaceType.OpcPlcApplications,
                    defaultValue: (uint)0));

            NodeIDs = new List<string>
            {
                id.ToString(),
            };
        }
    }
}
