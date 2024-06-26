namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Node with an opaque identifier (free-format byte string that might or might not be human interpretable).
/// as well as nodes of type NodeId and ExpandedNodeId with IdType of String, Numeric, Opaque, and Guid.
/// </summary>
public class OpaqueAndNodeIdPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint> _node;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        // on|opaquenode
        // Add node with an opaque identifier.
        // Enabled by default.
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        FolderState folder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Special",
            name: "Special",
            NamespaceType.OpcPlcApplications);

        AddNodes(folder);
    }

    public void StartSimulation()
    {
        _node.Start(value => value + 1, periodMs: 1000);
    }

    public void StopSimulation()
    {
        _node.Stop();
    }

    private void AddNodes(FolderState folder)
    {
        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            folder,
            path: new byte[] { (byte)'a', (byte)'b', (byte)'c' },
            name: "Opaque_abc",
            new NodeId((uint)BuiltInType.UInt32),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Constantly increasing value",
            NamespaceType.OpcPlcApplications,
            defaultValue: (uint)0);

        _node = _plcNodeManager.CreateVariableNode<uint>(variable);

        BaseDataVariableState stringNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticNodeIdString",
            "ScalarStaticNodeIdString",
            new NodeId((int)BuiltInType.NodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "String representation of the NodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new NodeId("this is a string node id", 3)
        );

        BaseDataVariableState numericNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticNodeIdNumeric",
            "ScalarStaticNodeIdNumeric",
            new NodeId((int)BuiltInType.NodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "UInt32 representation of the NodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new NodeId(42, 3)
        );

        BaseDataVariableState opaqueNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticNodeIdOpaque",
            "ScalarStaticNodeIdOpaque",
            new NodeId((int)BuiltInType.NodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Opaque representation of the NodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new NodeId(new byte[] { 0x01, 0x02, 0x03, 0x04 }, 3)
        );

        BaseDataVariableState guidNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticNodeIdGuid",
            "ScalarStaticNodeIdGuid",
            new NodeId((int)BuiltInType.NodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Guid representation of the NodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new NodeId(Guid.NewGuid(), 3)
        );

        BaseDataVariableState stringExpandedNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticExpandedNodeIdString",
            "ScalarStaticExpandedNodeIdString",
            new NodeId((int)BuiltInType.ExpandedNodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "String representation of the ExpandedNodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new ExpandedNodeId("this is a string expanded node id", 3, OpcPlc.Namespaces.OpcPlcApplications, 0)
        );

        BaseDataVariableState numericExpandedNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticExpandedNodeIdNumeric",
            "ScalarStaticExpandedNodeIdNumeric",
            new NodeId((int)BuiltInType.ExpandedNodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Numeric representation of the ExpandedNodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new ExpandedNodeId(444u, 3, OpcPlc.Namespaces.OpcPlcApplications, 0)
        );

        BaseDataVariableState guidExpandedNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticExpandedNodeIdGuid",
            "ScalarStaticExpandedNodeIdGuid",
            new NodeId((int)BuiltInType.ExpandedNodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Guid representation of the ExpandedNodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new ExpandedNodeId(Guid.NewGuid(), 3, OpcPlc.Namespaces.OpcPlcApplications, 0)
        );

        BaseDataVariableState opaqueExpandedNodeIdVariable = _plcNodeManager.CreateBaseVariable(
            folder,
            "ScalarStaticExpandedNodeIdOpaque",
            "ScalarStaticExpandedNodeIdOpaque",
            new NodeId((int)BuiltInType.ExpandedNodeId),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Opaque representation of the ExpandedNodeId",
            NamespaceType.OpcPlcApplications,
            defaultValue: new ExpandedNodeId(new byte[] { 0xCA, 0xFE}, 3, OpcPlc.Namespaces.OpcPlcApplications, 0)
        );

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(stringNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(numericNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(opaqueNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(guidNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(stringExpandedNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(numericExpandedNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(guidExpandedNodeIdVariable.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(opaqueExpandedNodeIdVariable.NodeId, _plcNodeManager),
        };
    }
}
