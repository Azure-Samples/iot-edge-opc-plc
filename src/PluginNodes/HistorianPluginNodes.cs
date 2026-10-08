namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class HistorianPluginNodes(TimeService timeService, ILogger logger)
    : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _manager;
    private OpcPlc.ITimer _timer;

    public bool Enabled { get; private set; }

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "hn|historian",
            "enable one historical-access node (Int32). Default: disabled",
            value => Enabled = value != null);
    }

    public async ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enabled)
        {
            return;
        }

        _manager = plcNodeManager;
        FolderState folder = plcNodeManager.CreateFolder(
            telemetryFolder, "Historian", "Historian", NamespaceType.OpcPlcApplications);
        BaseDataVariableState variable = plcNodeManager.CreateBaseVariable(
            folder, "HistorianInt32", "HistorianInt32", DataTypeIds.Int32, ValueRanks.Scalar,
            AccessLevels.CurrentRead | AccessLevels.HistoryRead,
            "Historical samples", NamespaceType.OpcPlcApplications, 0);
        variable.Historizing = true;
        await plcNodeManager.EnableHistoryAsync(variable, cancellationToken).ConfigureAwait(false);
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, plcNodeManager)
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
}
