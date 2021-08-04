namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Nodes that change value every second to string containing single repeated uppercase letter.
    /// </summary>
    public class LongStringPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private static bool _isEnabled;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<string> _longStringIdNode10;
        private SimulatedVariableNode<string> _longStringIdNode50;
        private SimulatedVariableNode<byte[]> _longStringIdNode100;
        private SimulatedVariableNode<byte[]> _longStringIdNode200;
        private readonly Random _random = new Random();

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "lsn|longstringnodes",
                $"add nodes with string values of 10/50/100/200 kB.\nDefault: {_isEnabled}",
                (string s) => _isEnabled = s != null);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (_isEnabled)
            {
                FolderState folder = _plcNodeManager.CreateFolder(
                    telemetryFolder,
                    path: "Special",
                    name: "Special",
                    NamespaceType.OpcPlcApplications);

                AddNodes(folder);
            }
        }

        public void StartSimulation()
        {
            if (_isEnabled)
            {
                // Change value every second to string containing single repeated uppercase letter.
                const int A = 65, Z = 90 + 1;

                _longStringIdNode10.Start(value => new string((char)_random.Next(A, Z), 10 * 1024), periodMs: 1000);
                _longStringIdNode50.Start(value => new string((char)_random.Next(A, Z), 50 * 1024), periodMs: 1000);
                _longStringIdNode100.Start(value => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 100 * 1024)), periodMs: 1000);
                _longStringIdNode200.Start(value => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 200 * 1024)), periodMs: 1000);
            }
        }

        public void StopSimulation()
        {
            if (_isEnabled)
            {
                _longStringIdNode10.Stop();
                _longStringIdNode50.Stop();
                _longStringIdNode100.Stop();
                _longStringIdNode200.Stop();
            }
        }

        private void AddNodes(FolderState folder)
        {
            // 10 kB.
            string initialString = new string('A', 10 * 1024);
            _longStringIdNode10 = _plcNodeManager.CreateVariableNode<string>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString10kB",
                    name: "LongString10kB",
                    new NodeId((uint)BuiltInType.String),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.OpcPlcApplications,
                    initialString));

            // 50 kB.
            initialString = new string('A', 50 * 1024);
            _longStringIdNode50 = _plcNodeManager.CreateVariableNode<string>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString50kB",
                    name: "LongString50kB",
                    new NodeId((uint)BuiltInType.String),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.OpcPlcApplications,
                    initialString));

            // 100 kB.
            var initialByteArray = Encoding.UTF8.GetBytes(new string('A', 100 * 1024));
            _longStringIdNode100 = _plcNodeManager.CreateVariableNode<byte[]>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString100kB",
                    name: "LongString100kB",
                    new NodeId((uint)BuiltInType.ByteString),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.OpcPlcApplications,
                    initialByteArray));

            // 200 kB.
            initialByteArray = Encoding.UTF8.GetBytes(new string('A', 200 * 1024));
            _longStringIdNode200 = _plcNodeManager.CreateVariableNode<byte[]>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString200kB",
                    name: "LongString200kB",
                    new NodeId((uint)BuiltInType.Byte),
                    ValueRanks.OneDimension,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.OpcPlcApplications,
                    initialByteArray));

            Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals { NodeId = "LongString10kB" },
                new NodeWithIntervals { NodeId = "LongString50kB" },
                new NodeWithIntervals { NodeId = "LongString100kB" },
                new NodeWithIntervals { NodeId = "LongString200kB" },
            };
        }
    }
}
