namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Node with ID of 3950 chars.
/// </summary>
public class LongIdPluginNode : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint> _node;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        // lid|longid
        // Add node with ID of 3950 chars.
        _isEnabled = true;
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            FolderState folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Special",
                name: "Special",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder);
        }
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _node.Start(value => value + 1, periodMs: 1000);
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
        // Repeat A-Z until 3950 chars are collected.
        var id = new StringBuilder(4000);
        for (int i = 0; i < 3950; i++)
        {
            id.Append((char)(65 + (i % 26)));
        }

        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            folder,
            path: id.ToString(),
            name: "LongId3950",
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
            PluginNodesHelpers.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        };
    }
}
