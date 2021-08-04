namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Node with a sine wave value with a dip anomaly.
    /// </summary>
    public class DipPluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private static bool _isEnabled = true;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new Random();
        private int _dipCycleInPhase;
        private int _dipAnomalyCycle;
        private const double SimulationMaxAmplitude = 100.0;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "nd|nodips",
                $"do not generate dip data\nDefault: {!_isEnabled}",
                (string s) => _isEnabled = s == null);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (_isEnabled)
            {
                FolderState folder = _plcNodeManager.CreateFolder(
                    telemetryFolder,
                    path: "Anomaly",
                    name: "Anomaly",
                    NamespaceType.OpcPlcApplications);

                AddNodes(folder);
            }
        }

        public void StartSimulation()
        {
            if (_isEnabled)
            {
                _dipCycleInPhase = PlcSimulation.SimulationCycleCount;
                _dipAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
                Logger.Verbose($"First dip anomaly cycle: {_dipAnomalyCycle}");

                _node.Start(DipGenerator, PlcSimulation.SimulationCycleLength);
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
            _node = _plcNodeManager.CreateVariableNode<double>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "DipData",
                    name: "DipData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with random dips",
                    NamespaceType.OpcPlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals { NodeId = "DipData" },
            };
        }

        /// <summary>
        /// Generates a sine wave with dips at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double DipGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (_isEnabled && _dipCycleInPhase == _dipAnomalyCycle)
            {
                nextValue = SimulationMaxAmplitude * -10;
                Logger.Verbose("Generate dip anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / PlcSimulation.SimulationCycleCount) * _dipCycleInPhase);
            }
            Logger.Verbose($"Spike cycle: {_dipCycleInPhase} data: {nextValue}");

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_dipCycleInPhase == 0)
            {
                _dipCycleInPhase = PlcSimulation.SimulationCycleCount;
                _dipAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
                Logger.Verbose($"Next dip anomaly cycle: {_dipAnomalyCycle}");
            }

            return nextValue;
        }
    }
}
