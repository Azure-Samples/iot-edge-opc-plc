namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Diagnostics;

/// <summary>
/// Node that shows current working set memory consumption in MB.
/// </summary>
public class WorkingSetPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint> _node;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
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
        _node.Start(value => GetWorkingSetMB(), periodMs: 1000);
    }

    public void StopSimulation()
    {
        _node.Stop();
    }

    private void AddNodes(FolderState folder)
    {
        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            folder,
            path: "WorkingSetMB",
            name: "WorkingSetMB",
            new NodeId((uint)BuiltInType.UInt32),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Working set memory consumption in MB",
            NamespaceType.OpcPlcApplications,
            defaultValue: GetWorkingSetMB());

        _node = _plcNodeManager.CreateVariableNode<uint>(variable);

        // Add to node list for creation of pn.json.
        Nodes =
        [
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        ];
    }

    private uint GetWorkingSetMB() => (uint)(Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024);
}
