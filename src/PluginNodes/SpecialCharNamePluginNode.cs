namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Web;

    /// <summary>
    /// Node with special chars in name and ID.
    /// </summary>
    public class SpecialCharNamePluginNode : IPluginNodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; } = new List<string>();

        private static bool _isEnabled;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _node;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "scn|specialcharname",
                $"add node with special characters in name.\nDefault: {_isEnabled}",
                (string p) => _isEnabled = p != null);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (_isEnabled)
            {
                FolderState folder = _plcNodeManager.CreateFolder(
                    (FolderState)telemetryFolder.Parent, // Root.
                    path: "Special Nodes",
                    name: "Special Nodes",
                    NamespaceType.OpcPlcApplications);

                AddNodes(folder);
            }
        }

        public void StartSimulation()
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
