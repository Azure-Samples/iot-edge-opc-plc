namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Node with a value that shows a negative trend.
/// </summary>
public class NegTrendPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled = true;
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
            $"do not generate negative trend data.\nDefault: {!_isEnabled}",
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
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _logger.LogTrace($"First neg trend anomaly phase: {_negTrendAnomalyPhase}");

            _node.Start(NegTrendGenerator, _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
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
            path: "NegativeTrendData",
            name: "NegativeTrendData",
            new NodeId((uint)BuiltInType.Double),
            ValueRanks.Scalar,
            AccessLevels.CurrentRead,
            "Value with a slow negative trend",
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
            path: "ResetNegTrend",
            name: "ResetNegTrend",
            "Reset the negative trend values to their baseline value",
            NamespaceType.OpcPlcApplications);

        SetResetTrendMethodProperties(ref resetTrendMethod);
    }

    /// <summary>
    /// Generates a sine wave with spikes at a configurable cycle in the phase.
    /// Called each SimulationCycleLength ms.
    /// </summary>
    private double NegTrendGenerator(double value)
    {
        // calculate next value
        double nextValue = TREND_BASEVALUE;
        if (_isEnabled && _negTrendPhase >= _negTrendAnomalyPhase)
        {
            nextValue = TREND_BASEVALUE - ((_negTrendPhase - _negTrendAnomalyPhase) / 10d);
            _logger.LogTrace("Generate negtrend anomaly");
        }

        // end of cycle: reset cycle count and calc next anomaly cycle
        if (--_negTrendCycleInPhase == 0)
        {
            _negTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _negTrendPhase++;
            _logger.LogTrace($"Neg trend phase: {_negTrendPhase}, data: {nextValue}");
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
        _logger.LogDebug("ResetNegTrend method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method implementation to reset the trend data.
    /// </summary>
    public void ResetTrendData()
    {
        _negTrendAnomalyPhase = _random.Next(10);
        _negTrendCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
        _negTrendPhase = 0;
    }
}
