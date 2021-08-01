namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using static OpcPlc.Program;

    /// <summary>
    /// Node with a value that shows a positive trend.
    /// </summary>
    public class PosTrendPluginNode : IPluginNodes
    {
        public IReadOnlyCollection<string> NodeIDs { get; private set; } = new List<string>();

        private static bool _isEnabled = true;
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new Random();
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private int _posTrendAnomalyPhase;
        private const double TREND_BASEVALUE = 100.0;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "np|nopostrend",
                $"do not generate positive trend data\nDefault: {!_isEnabled}",
                (string p) => _isEnabled = p == null);
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
                AddMethods(methodsFolder);
            }
        }

        public void StartSimulation()
        {
            if (_isEnabled)
            {
                _posTrendAnomalyPhase = _random.Next(10);
                _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
                Logger.Verbose($"First pos trend anomaly phase: {_posTrendAnomalyPhase}");

                _node.Start(PosTrendGenerator, PlcSimulation.SimulationCycleLength);
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
                    path: "PositiveTrendData",
                    name: "PositiveTrendData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with a slow positive trend",
                    NamespaceType.OpcPlcApplications));

            NodeIDs = new List<string>
            {
                "PositiveTrendData",
            };
        }

        private void AddMethods(FolderState methodsFolder)
        {
            MethodState resetTrendMethod = _plcNodeManager.CreateMethod(methodsFolder, "ResetPosTrend", "ResetPosTrend", "Reset the positive trend values to their baseline value", NamespaceType.OpcPlcApplications);
            SetResetTrendMethodProperties(ref resetTrendMethod);
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private double PosTrendGenerator(double value)
        {
            // calculate next value
            double nextValue = TREND_BASEVALUE;
            if (_isEnabled && _posTrendPhase >= _posTrendAnomalyPhase)
            {
                nextValue = TREND_BASEVALUE + ((_posTrendPhase - _posTrendAnomalyPhase) / 10d);
                Logger.Verbose("Generate postrend anomaly");
            }

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_posTrendCycleInPhase == 0)
            {
                _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
                _posTrendPhase++;
                Logger.Verbose($"Pos trend phase: {_posTrendPhase}, data: {nextValue}");
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
            Logger.Debug("ResetPosTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method implementation to reset the trend data.
        /// </summary>
        public void ResetTrendData()
        {
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
            _posTrendPhase = 0;
        }
    }
}
