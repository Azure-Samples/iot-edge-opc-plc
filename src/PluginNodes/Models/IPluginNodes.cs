namespace OpcPlc.PluginNodes.Models;

using Opc.Ua;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IPluginNodes
{
    IReadOnlyCollection<NodeWithIntervals> Nodes { get; }

    void AddOptions(Mono.Options.OptionSet optionSet);

    ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder,
        FolderState methodsFolder,
        PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default);

    void OnAddressSpaceReady()
    {
    }

    void StartSimulation();

    void StopSimulation();
}
