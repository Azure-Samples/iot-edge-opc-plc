namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;

public class HistorianPluginNodes(TimeService timeService, ILogger logger)
    : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _manager;
    private ITimer _timer;

    public bool Enabled { get; private set; }

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "hn|historian",
            "enable one historical-access node (Int32). Default: disabled",
            value => Enabled = value != null);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        if (!Enabled)
        {
            return;
        }

        _manager = plcNodeManager;
        FolderState folder = plcNodeManager.CreateFolder(
            telemetryFolder, "Historian", "Historian", NamespaceType.OpcPlcApplications);
        Nodes = new List<NodeWithIntervals>
        {
            AddVariable(plcNodeManager, folder, "HistorianInt32", DataTypeIds.Int32, 0)
        };
    }

    public void StartSimulation()
    {
        if (Enabled)
        {
            _timer = _timeService.NewTimer((sender, args) => _manager.UpdateHistory(), 10000);
        }
    }

    public void StopSimulation()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private static NodeWithIntervals AddVariable(
        PlcNodeManager manager, FolderState folder, string name, NodeId dataType, object value)
    {
        BaseDataVariableState variable = manager.CreateBaseVariable(
            folder, name, name, dataType, ValueRanks.Scalar,
            AccessLevels.CurrentRead | AccessLevels.HistoryRead,
            "Historical samples", NamespaceType.OpcPlcApplications, value);
        variable.Historizing = true;
        manager.EnableHistory(variable);
        return PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, manager);
    }
}