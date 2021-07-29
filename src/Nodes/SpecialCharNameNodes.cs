namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Web;

    public class SpecialCharNameNodes : INodes<string>
    {
        // Command line option.
        public string Prototype { get; } = "scn|specialcharname";
        public string Description { get; } = $"add node with special characters in name.\nDefault: {_isEnabled}";
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
            string SpecialChars = HttpUtility.HtmlDecode(@"&quot;!&#167;$%&amp;/()=?`&#180;\+~*&#39;#_-:.;,&lt;&gt;|@^&#176;€&#181;{[]}");

            _node = _plcNodeManager.CreateVariableNode<uint>(
                    _plcNodeManager.CreateBaseVariable(
                        folder,
                        path: "Special_" + SpecialChars,
                        name: SpecialChars,
                        new NodeId((uint)BuiltInType.UInt32),
                        ValueRanks.Scalar,
                        AccessLevels.CurrentReadOrWrite,
                        "Constantly increasing value",
                        NamespaceType.OpcPlcApplications,
                        defaultValue: (uint)0));

            NodeIDs = new List<string>
            {
                "Special_" + SpecialChars,
            };
        }
    }
}
