namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;

/// <summary>
/// Client-managed heartbeat values exposed directly under the OPC UA Objects folder.
/// </summary>
public class HeartbeatsPluginNodes(TimeService timeService, ILogger logger)
    : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;

    public bool IsEnabled => _isEnabled;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "hb|heartbeats",
            $"add heartbeat nodes to the address space.\nDefault: {_isEnabled}",
            (string s) => _isEnabled = s is not null);
    }

    public void AddToAddressSpace(
        FolderState telemetryFolder,
        FolderState methodsFolder,
        PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            AddNodes();
        }
    }

    public void StartSimulation()
    {
        // Values are managed by OPC UA clients.
    }

    public void StopSimulation()
    {
        // No simulation to stop.
    }

    private void AddNodes()
    {
        FolderState heartbeatsFolder = _plcNodeManager.CreateFolder(
            parent: null,
            path: "heartbeats",
            name: "heartbeats",
            NamespaceType.OpcPlcApplications);

        BaseDataVariableState connectionKeepalive = CreateHeartbeatVariable(
            heartbeatsFolder,
            "heartbeats_connectionKeepalive",
            "connectionKeepalive");
        BaseDataVariableState datasetWriteCounter = CreateHeartbeatVariable(
            heartbeatsFolder,
            "heartbeats_datasetWriteCounter",
            "datasetWriteCounter");

        _plcNodeManager.AddNodeToObjects(heartbeatsFolder);

        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(connectionKeepalive.NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(datasetWriteCounter.NodeId, _plcNodeManager),
        };
    }

    private BaseDataVariableState CreateHeartbeatVariable(
        FolderState heartbeatsFolder,
        string nodeId,
        string name)
    {
        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            heartbeatsFolder,
            path: nodeId,
            name,
            DataTypeIds.UInt32,
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            name,
            NamespaceType.OpcPlcApplications,
            defaultValue: 0u);

        variable.BrowseName = new QualifiedName(name, variable.NodeId.NamespaceIndex);
        return variable;
    }
}
