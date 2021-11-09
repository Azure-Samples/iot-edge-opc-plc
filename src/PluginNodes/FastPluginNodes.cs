namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using static OpcPlc.Program;

/// <summary>
/// Nodes with fast changing values.
/// </summary>
public class FastPluginNodes : IPluginNodes
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
            "fn|fastnodes=",
            $"number of fast nodes\nDefault: {NodeCount}",
            (uint i) => NodeCount = i);

        optionSet.Add(
            "fr|fastrate=",
            $"rate in seconds to change fast nodes\nDefault: {NodeRate / 1000}",
            (uint i) => NodeRate = i * 1000);

        optionSet.Add(
            "ft|fasttype=",
            $"data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeType}",
            (string s) => NodeType = SlowFastCommon.ParseNodeType(s));

        optionSet.Add(
            "ftl|fasttypelowerbound=",
            $"lower bound of data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: min value of node type.",
            (string s) => NodeMinValue = s);

        optionSet.Add(
            "ftu|fasttypeupperbound=",
            $"upper bound of data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: max value of node type.",
            (string s) => NodeMaxValue = s);

        optionSet.Add(
            "ftr|fasttyperandomization=",
            $"randomization of fast nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeRandomization}",
            (string s) => NodeRandomization = bool.Parse(s));

        optionSet.Add(
            "fts|fasttypestepsize=",
            $"step or increment size of fast nodes value ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {NodeStepSize}",
            (string s) => NodeStepSize = SlowFastCommon.ParseStepSize(s));

        optionSet.Add(
            "fsi|fastnodesamplinginterval=",
            $"rate in milliseconds to sample fast nodes\nDefault: {NodeSamplingInterval}",
            (uint i) => NodeSamplingInterval = i);

        optionSet.Add(
            "vfr|veryfastrate=",
            $"rate in milliseconds to change fast nodes\nDefault: {NodeRate}",
            (uint i) => NodeRate = i);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;
        _slowFastCommon = new SlowFastCommon(_plcNodeManager);

        FolderState folder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Fast",
            name: "Fast",
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
        MethodState stopUpdateMethod = _plcNodeManager.CreateMethod(
            methodsFolder,
            path: "StopUpdateFastNodes",
            name: "StopUpdateFastNodes",
            "Stop the increase of value of fast nodes",
            NamespaceType.OpcPlcApplications);

        SetStopUpdateFastNodesProperties(ref stopUpdateMethod);

        MethodState startUpdateMethod = _plcNodeManager.CreateMethod(
            methodsFolder,
            path: "StartUpdateFastNodes",
            name: "StartUpdateFastNodes",
            "Start the increase of value of fast nodes",
            NamespaceType.OpcPlcApplications);

        SetStartUpdateFastNodesProperties(ref startUpdateMethod);
    }

    public void StartSimulation()
    {
        // Only use the fast timers when we need to go really fast,
        // since they consume more resources and create an own thread.
        _nodeGenerator = NodeRate >= 50 || !Stopwatch.IsHighResolution ?
            TimeService.NewTimer(UpdateNodes, NodeRate) :
            TimeService.NewFastTimer(UpdateVeryFastNodes, NodeRate);
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
        (_nodes, _badNodes) = _slowFastCommon.CreateNodes(NodeType, "Fast", NodeCount, folder, simulatorFolder, NodeRandomization, NodeStepSize, NodeMinValue, NodeMaxValue, NodeRate, NodeSamplingInterval);

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
                Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                PublishingInterval = NodeRate,
                SamplingInterval = NodeSamplingInterval,
            });
        }

        foreach (var node in _badNodes)
        {
            nodes.Add(new NodeWithIntervals
            {
                NodeId = node.NodeId.Identifier.ToString(),
                Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                PublishingInterval = NodeRate,
                SamplingInterval = NodeSamplingInterval,
            });
        }

        Nodes = nodes;
    }

    private void SetStopUpdateFastNodesProperties(ref MethodState method)
    {
        method.OnCallMethod += OnStopUpdateFastNodes;
    }

    private void SetStartUpdateFastNodesProperties(ref MethodState method)
    {
        method.OnCallMethod += OnStartUpdateFastNodes;
    }

    /// <summary>
    /// Method to stop updating the fast nodes.
    /// </summary>
    private ServiceResult OnStopUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        _updateNodes = false;
        Logger.Debug("StopUpdateFastNodes method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method to start updating the fast nodes.
    /// </summary>
    private ServiceResult OnStartUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        _updateNodes = true;
        Logger.Debug("StartUpdateFastNodes method called");
        return ServiceResult.Good;
    }

    private void UpdateNodes(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
    }

    private void UpdateVeryFastNodes(object state, FastTimerElapsedEventArgs elapsedEventArgs)
    {
        _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
    }
}
