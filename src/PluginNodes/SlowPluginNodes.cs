namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using static OpcPlc.Program;

    /// <summary>
    /// Nodes with slow changing values.
    /// </summary>
    public class SlowPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private uint NodeCount { get; set; } = 1;
        private uint NodeRate { get; set; } = 10000; // ms.
        private NodeType NodeType { get; set; } = NodeType.UInt;
        private string NodeMinValue { get; set; }
        private string NodeMaxValue { get; set; }
        private bool NodeRandomization { get; set; } = false;
        private string NodeStepSize { get; set; } = "1";
        private uint NodeSamplingInterval { get; set; } // ms.

        private PlcNodeManager _plcNodeManager;
        private SlowFastCommon _slowFastCommon;
        protected BaseDataVariableState[] _nodes = null;
        protected BaseDataVariableState[] _badNodes = null;
        private ITimer _nodeGenerator;
        private bool _updateNodes = true;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "sn|slownodes=",
                $"number of slow nodes\nDefault: {NodeCount}",
                (uint i) => NodeCount = i);

            optionSet.Add(
                "sr|slowrate=",
                $"rate in seconds to change slow nodes\nDefault: {NodeRate / 1000}",
                (uint i) => NodeRate = i * 1000);

            optionSet.Add(
                "st|slowtype=",
                $"data type of slow nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeType}",
                (string s) => NodeType = SlowFastCommon.ParseNodeType(s));

            optionSet.Add(
                "stl|slowtypelowerbound=",
                $"lower bound of data type of slow nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: min value of node type.",
                (string s) => NodeMinValue = s);

            optionSet.Add(
                "stu|slowtypeupperbound=",
                $"upper bound of data type of slow nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: max value of node type.",
                (string s) => NodeMaxValue = s);

            optionSet.Add(
                "str|slowtyperandomization=",
                $"randomization of slow nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeRandomization}",
                (string s) => NodeRandomization = bool.Parse(s));

            optionSet.Add(
                "sts|slowtypestepsize=",
                $"step or increment size of slow nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeStepSize}",
                (string s) => NodeStepSize = SlowFastCommon.ParseStepSize(s));

            optionSet.Add(
                "ssi|slownodesamplinginterval=",
                $"rate in milliseconds to sample slow nodes\nDefault: {NodeSamplingInterval}",
                (uint i) => NodeSamplingInterval = i);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            _slowFastCommon = new SlowFastCommon(_plcNodeManager);

            FolderState folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Slow",
                name: "Slow",
                NamespaceType.OpcPlcApplications);

            // Used for methods to limit the number of updates to a fixed count.
            FolderState simulatorFolder = _plcNodeManager.CreateFolder(
                telemetryFolder.Parent, // Root.
                path: "SimulatorConfiguration",
                name: "SimulatorConfiguration",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder, simulatorFolder);
            AddMethods(methodsFolder);
        }

        private void AddMethods(FolderState methodsFolder)
        {
            string stopMethod = "StopUpdateSlowNodes";
            string startMethod = "StartUpdateSlowNodes";

            MethodState stopUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: stopMethod,
                name: stopMethod,
                "Stop the increase of value of slow nodes",
                NamespaceType.OpcPlcApplications);

            stopUpdateMethod.OnCallMethod += (context, method, inputArguments, outputArguments) =>
            {
                _updateNodes = false;
                Logger.Debug($"{stopMethod} method called");
                return ServiceResult.Good;
            };

            MethodState startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: startMethod,
                name: startMethod,
                "Start the increase of value of slow nodes",
                NamespaceType.OpcPlcApplications);

            startUpdateMethod.OnCallMethod += (context, method, inputArguments, outputArguments) =>
            {
                _updateNodes = true;
                Logger.Debug($"{startMethod} method called");
                return ServiceResult.Good;
            };
        }

        public void StartSimulation()
        {
            _nodeGenerator = TimeService.NewTimer(UpdateNodes, NodeRate);
        }

        public void StopSimulation()
        {
            if (_nodeGenerator != null)
            {
                _nodeGenerator.Enabled = false;
            }
        }

        private void AddNodes(FolderState folder, FolderState simulatorFolder)
        {
            (_nodes, _badNodes) = _slowFastCommon.CreateNodes(NodeType, "Slow", NodeCount, folder, simulatorFolder, NodeRandomization, NodeStepSize, NodeMinValue, NodeMaxValue, NodeRate, NodeSamplingInterval);

            ExposeNodesWithIntervals();
        }

        /// <summary>
        /// Expose node information for dumping pn.json.
        /// </summary>
        private void ExposeNodesWithIntervals()
        {
            var nodes = new List<NodeWithIntervals>();

            foreach (var node in _nodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            foreach (var node in _badNodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            Nodes = nodes;
        }

        private void UpdateNodes(object state, ElapsedEventArgs elapsedEventArgs)
        {
            _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
        }
    }
}
