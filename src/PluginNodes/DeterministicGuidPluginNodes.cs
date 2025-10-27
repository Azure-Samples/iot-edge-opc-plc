namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Nodes with deterministic GUIDs as ID.
/// </summary>
public class DeterministicGuidPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private readonly DeterministicGuid _deterministicGuid = new ();
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint>[] _nodes;

    private static uint NodeCount { get; set; } = 1;
    private uint NodeRate { get; set; } = 1000; // ms.
    private NodeType NodeType { get; set; } = NodeType.UInt;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "gn|guidnodes=",
            $"number of nodes with deterministic GUID IDs.\nDefault: {NodeCount}",
            (uint i) => NodeCount = i);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        FolderState folder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Deterministic GUID",
            name: "Deterministic GUID",
            NamespaceType.OpcPlcApplications);

        AddNodes(folder);
    }

    public void StartSimulation()
    {
        foreach (var node in _nodes)
        {
            node.Start(value => value + 1, periodMs: 1000);
        }
    }

    public void StopSimulation()
    {
        foreach (var node in _nodes)
        {
            node.Stop();
        }
    }

    private void AddNodes(FolderState folder)
    {
        _nodes = new SimulatedVariableNode<uint>[NodeCount];
        var nodes = new List<NodeWithIntervals>((int)NodeCount);

        if (NodeCount > 0)
        {
            _logger.LogInformation($"Creating {NodeCount} GUID node(s) of type: {NodeType}");
            _logger.LogInformation($"Node values will change every {NodeRate:N0} ms");
        }

        for (int i = 0; i < NodeCount; i++)
        {
            Guid id = _deterministicGuid.NewGuid();

            BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
                folder,
                path: id,
                name: id.ToString(),
                new NodeId((uint)BuiltInType.UInt32),
                ValueRanks.Scalar,
                AccessLevels.CurrentReadOrWrite,
                "Constantly increasing value",
                NamespaceType.OpcPlcApplications,
                defaultValue: (uint)0);

            _nodes[i] = _plcNodeManager.CreateVariableNode<uint>(variable);

            // Add to node list for creation of pn.json.
            nodes.Add(PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager));
        }

        Nodes = nodes;
    }
}
