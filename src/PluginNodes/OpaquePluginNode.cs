namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;

/// <summary>
/// Node with an opaque identifier (free-format byte string that might or might not be human interpretable).
/// </summary>
public class OpaquePluginNode(PlcSimulation plcSimulation, TimeService timeService, ILogger logger) : PluginNodeBase(plcSimulation, timeService, logger)
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

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        };
    }
}
