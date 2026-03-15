namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Node with a sine wave value with a dip anomaly.
/// </summary>
public partial class DipPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled = true;
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
            $"do not generate dip data.\nDefault: {!_isEnabled}",
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
            _dipCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _dipAnomalyCycle = _random.Next(_plcNodeManager.PlcSimulationInstance.SimulationCycleCount);
            LogFirstDipAnomalyCycle(_dipAnomalyCycle);

            _node.Start(DipGenerator, _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
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
            path: "DipData",
            name: "DipData",
            new NodeId((uint)BuiltInType.Double),
            ValueRanks.Scalar,
            AccessLevels.CurrentRead,
            "Value with random dips",
            NamespaceType.OpcPlcApplications);

        _node = _plcNodeManager.CreateVariableNode<double>(variable);

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        };
    }

    /// <summary>
    /// Generates a sine wave with dips at a random cycle in the phase.
    /// Called each SimulationCycleLength ms.
    /// </summary>
    private double DipGenerator(double value)
    {
        // calculate next value
        double nextValue;
        if (_isEnabled && _dipCycleInPhase == _dipAnomalyCycle)
        {
            nextValue = SimulationMaxAmplitude * -10;
            LogGenerateDipAnomaly();
        }
        else
        {
            nextValue = SimulationMaxAmplitude * Math.Sin(((2 * Math.PI) / _plcNodeManager.PlcSimulationInstance.SimulationCycleCount) * _dipCycleInPhase);
        }
        LogDipCycleData(_dipCycleInPhase, nextValue);

        // end of cycle: reset cycle count and calc next anomaly cycle
        if (--_dipCycleInPhase == 0)
        {
            _dipCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _dipAnomalyCycle = _random.Next(_plcNodeManager.PlcSimulationInstance.SimulationCycleCount);
            LogNextDipAnomalyCycle(_dipAnomalyCycle);
        }

        return nextValue;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "First dip anomaly cycle: {DipAnomalyCycle}")]
    partial void LogFirstDipAnomalyCycle(int dipAnomalyCycle);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Generate dip anomaly")]
    partial void LogGenerateDipAnomaly();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Dip cycle: {DipCycleInPhase} data: {NextValue}")]
    partial void LogDipCycleData(int dipCycleInPhase, double nextValue);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Next dip anomaly cycle: {DipAnomalyCycle}")]
    partial void LogNextDipAnomalyCycle(int dipAnomalyCycle);
}
