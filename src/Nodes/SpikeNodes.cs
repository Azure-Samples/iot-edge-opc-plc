﻿namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Node with a sine wave value with a spike anomaly.
    /// </summary>
    public class SpikeNodes : INodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; } = new List<string>();

        private static bool _isEnabled = true;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new Random();
        private int _spikeCycleInPhase;
        private int _spikeAnomalyCycle;

        public void AddOption(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "ns|nospikes",
                $"do not generate spike data\nDefault: {!_isEnabled}",
                (string p) => _isEnabled = p == null);
        }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;

            if (_isEnabled)
            {
                AddNodes(telemetryFolder);
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

            NodeIDs = new List<string>
            {
                "SpikeData",
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
                nextValue = PlcSimulation.SimulationMaxAmplitude * 10;
                Logger.Verbose("Generate spike anomaly");
            }
            else
            {
                nextValue = PlcSimulation.SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / PlcSimulation.SimulationCycleCount) * _spikeCycleInPhase);
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
