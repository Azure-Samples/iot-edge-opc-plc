namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Node with a value that shows a negative trend.
    /// </summary>
    public class NegTrendPluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private static bool _isEnabled = true;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new Random();
        private int _negTrendCycleInPhase;
        private int _negTrendPhase;
        private int _negTrendAnomalyPhase;
        private const double TREND_BASEVALUE = 100.0;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "nn|nonegtrend",
                $"do not generate negative trend data\nDefault: {!_isEnabled}",
                (string p) => _isEnabled = p == null);
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
                AddMethods(methodsFolder);
            }
        }

        public void StartSimulation()
        {
            if (_isEnabled)
            {
                _negTrendAnomalyPhase = _random.Next(10);
                _negTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
                Logger.Verbose($"First neg trend anomaly phase: {_negTrendAnomalyPhase}");

                _node.Start(NegTrendGenerator, PlcSimulation.SimulationCycleLength);
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
                    path: "NegativeTrendData",
                    name: "NegativeTrendData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with a slow negative trend",
                    NamespaceType.OpcPlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals { NodeId = "NegativeTrendData" },
            };
        }

        private void AddMethods(FolderState methodsFolder)
        {
            MethodState resetTrendMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "ResetNegTrend",
                name: "ResetNegTrend",
                "Reset the negative trend values to their baseline value",
                NamespaceType.OpcPlcApplications);

            SetResetTrendMethodProperties(ref resetTrendMethod);
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double NegTrendGenerator(double value)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (_isEnabled && _negTrendPhase >= _negTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE - ((_negTrendPhase - _negTrendAnomalyPhase) / 10d);
                Logger.Verbose("Generate negtrend anomaly");
            }

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_negTrendCycleInPhase == 0)
            {
                _negTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
                _negTrendPhase++;
                Logger.Verbose($"Neg trend phase: {_negTrendPhase}, data: {nextValue}");
            }

            return nextValue;
        }

        /// <summary>
        /// Sets properties of the ResetTrend method.
        /// </summary>
        private void SetResetTrendMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnResetTrendCall;
        }

        /// <summary>
        /// Method to reset the trend values. Executes synchronously.
        /// </summary>
        private ServiceResult OnResetTrendCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            ResetTrendData();
            Logger.Debug("ResetNegTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method implementation to reset the trend data.
        /// </summary>
        public void ResetTrendData()
        {
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
            _negTrendPhase = 0;
        }
    }
}
