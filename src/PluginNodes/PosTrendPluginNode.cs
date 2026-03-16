namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Node with a value that shows a positive trend.
/// </summary>
public partial class PosTrendPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled = true;
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
            $"do not generate positive trend data.\nDefault: {!_isEnabled}",
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
            AddMethods(methodsFolder);
        }
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            LogFirstPosTrendAnomalyPhase(_posTrendAnomalyPhase);

            _node.Start(PosTrendGenerator, _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
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
        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            folder,
            path: "PositiveTrendData",
            name: "PositiveTrendData",
            new NodeId((uint)BuiltInType.Double),
            ValueRanks.Scalar,
            AccessLevels.CurrentRead,
            "Value with a slow positive trend",
            NamespaceType.OpcPlcApplications);

        _node = _plcNodeManager.CreateVariableNode<double>(variable);

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        };
    }

    private void AddMethods(FolderState methodsFolder)
    {
        MethodState resetTrendMethod = _plcNodeManager.CreateMethod(
            methodsFolder,
            path: "ResetPosTrend",
            name: "ResetPosTrend",
            "Reset the positive trend values to their baseline value",
            NamespaceType.OpcPlcApplications);

        SetResetTrendMethodProperties(ref resetTrendMethod);
    }

    /// <summary>
    /// Generates a sine wave with spikes at a configurable cycle in the phase.
    /// Called each SimulationCycleLength ms.
    /// </summary>
    private double PosTrendGenerator(double value)
    {
        // calculate next value
        double nextValue = TREND_BASEVALUE;
        if (_isEnabled && _posTrendPhase >= _posTrendAnomalyPhase)
        {
            nextValue = TREND_BASEVALUE + ((_posTrendPhase - _posTrendAnomalyPhase) / 10d);
            LogGeneratePosTrendAnomaly();
        }

        // end of cycle: reset cycle count and calc next anomaly cycle
        if (--_posTrendCycleInPhase == 0)
        {
            _posTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _posTrendPhase++;
            LogPosTrendPhase(_posTrendPhase, nextValue);
        }

        return nextValue;
    }

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
        LogResetPosTrendMethodCalled();
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method implementation to reset the trend data.
    /// </summary>
    public void ResetTrendData()
    {
        _posTrendAnomalyPhase = _random.Next(10);
        _posTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
        _posTrendPhase = 0;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "First pos trend anomaly phase: {PosTrendAnomalyPhase}")]
    partial void LogFirstPosTrendAnomalyPhase(int posTrendAnomalyPhase);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Generate postrend anomaly")]
    partial void LogGeneratePosTrendAnomaly();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Pos trend phase: {PosTrendPhase}, data: {NextValue}")]
    partial void LogPosTrendPhase(int posTrendPhase, double nextValue);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ResetPosTrend method called")]
    partial void LogResetPosTrendMethodCalled();
}
