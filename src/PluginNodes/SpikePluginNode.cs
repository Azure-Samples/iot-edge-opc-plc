namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Node with a sine wave value with a spike anomaly.
    /// </summary>
    public class SpikePluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private static bool _isEnabled = true;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new Random();
        private int _spikeCycleInPhase;
        private int _spikeAnomalyCycle;
        private const double SimulationMaxAmplitude = 100.0;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "ns|nospikes",
                $"do not generate spike data\nDefault: {!_isEnabled}",
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
                _spikeCycleInPhase = PlcSimulation.SimulationCycleCount;
                _spikeAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
                Logger.Verbose($"First spike anomaly cycle: {_spikeAnomalyCycle}");

                _node.Start(SpikeGenerator, PlcSimulation.SimulationCycleLength);
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
                    path: "SpikeData",
                    name: "SpikeData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with random spikes",
                    NamespaceType.OpcPlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals { NodeId = "SpikeData" },
            };
        }

        /// <summary>
        /// Generates a sine wave with spikes at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double SpikeGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (_isEnabled && _spikeCycleInPhase == _spikeAnomalyCycle)
            {
                // todo calculate
                nextValue = SimulationMaxAmplitude * 10;
                Logger.Verbose("Generate spike anomaly");
            }
            else
            {
                nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / PlcSimulation.SimulationCycleCount) * _spikeCycleInPhase);
            }
            Logger.Verbose($"Spike cycle: {_spikeCycleInPhase} data: {nextValue}");

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_spikeCycleInPhase == 0)
            {
                _spikeCycleInPhase = PlcSimulation.SimulationCycleCount;
                _spikeAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
                Logger.Verbose($"Next spike anomaly cycle: {_spikeAnomalyCycle}");
            }

            return nextValue;
        }
    }
}
