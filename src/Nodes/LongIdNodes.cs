namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Node with ID of 3950 chars.
    /// </summary>
    public class LongIdNodes : INodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; }

        private static bool _isEnabled;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _node;

        public void AddOption(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "lid|longid",
                $"add node with ID of 3950 chars.\nDefault: {_isEnabled}",
                (string p) => _isEnabled = p != null);
        }

        public void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (_isEnabled)
            {
                AddNodes(parentFolder);
            }
        }

        public void StartSimulation(PlcServer server)
        {
            if (_isEnabled)
            {
                _node.Start(value => value + 1, periodMs: 1000);
            }
        }

        public void StopSimulation()
        {
            if (_isEnabled)
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
