namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Nodes that are configured via *.NodeSet2.xml file(s).
/// </summary>
public partial class NodeSet2PluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private List<string> _nodesFileNames;
    private PlcNodeManager _plcNodeManager;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "ns2|nodeset2file=",
            "the *.NodeSet2.xml file that contains the nodes to be created in the OPC UA address space (multiple comma separated filenames supported).",
            (string s) => _nodesFileNames = CliHelper.ParseListOfFileNames(s, "ns2"));
    }

    public async ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _plcNodeManager = plcNodeManager;

        if (_nodesFileNames?.Any() ?? false)
        {
            await AddNodesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void StartSimulation()
    {
    }

    public void StopSimulation()
    {
    }

    private async ValueTask AddNodesAsync(CancellationToken cancellationToken)
    {
        foreach (var file in _nodesFileNames)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = File.OpenRead(file);

                // Load complex types from NodeSet2 file.
                await _plcNodeManager.LoadPredefinedNodesAsync(
                    context => LoadPredefinedNodes(context, stream), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogErrorLoadingNodeSet2File(e, file, e.Message);
            }
        }

        LogCompletedProcessingNodeSet2Files();
    }

    /// <summary>
    /// Loads a node set from a file and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context, Stream stream)
    {
        var predefinedNodes = new NodeStateCollection();
        var namespaces = new Dictionary<string, string>();

        var importedNodeSet = Opc.Ua.Export.UANodeSet.Read(stream);

        if (importedNodeSet.NamespaceUris != null)
        {
            foreach (var namespaceUri in importedNodeSet.NamespaceUris)
            {
                namespaces[namespaceUri] = namespaceUri;
            }
        }
        importedNodeSet.Import(_plcNodeManager.SystemContext, predefinedNodes);

        // Add to node list for creation of pn.json.
        Nodes ??= new List<NodeWithIntervals>();
        Nodes = Nodes.Append(PluginNodesHelper.GetNodeWithIntervals(predefinedNodes[0].NodeId, _plcNodeManager)).ToList();

        return predefinedNodes;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading NodeSet2 file {File}: {Error}")]
    partial void LogErrorLoadingNodeSet2File(Exception exception, string file, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed processing NodeSet2 file(s)")]
    partial void LogCompletedProcessingNodeSet2Files();
}
