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
/// Nodes that are configured via binary *.PredefinedNodes.uanodes file(s).
/// To produce a binary *.PredefinedNodes.uanodes file from an XML NodeSet file, run the following command:
/// ModelCompiler.cmd <XML_NodeSet_FileName_Without_Extension>
/// </summary>
public partial class UaNodesPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{

    private List<string> _nodesFileNames;
    private PlcNodeManager _plcNodeManager;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "unf|uanodesfile=",
            "the binary *.PredefinedNodes.uanodes file that contains the nodes to be created in the OPC UA address space (multiple comma separated filenames supported), use ModelCompiler.cmd <ModelDesign> to compile.",
            (string s) => _nodesFileNames = CliHelper.ParseListOfFileNames(s, "unf"));
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
        // No simulation.
    }

    public void StopSimulation()
    {
        // No simulation.
    }

    private async ValueTask AddNodesAsync(CancellationToken cancellationToken)
    {
        foreach (var file in _nodesFileNames)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = File.OpenRead(file);

                // Load complex types from binary uanodes file.
                await _plcNodeManager.LoadPredefinedNodesAsync(
                    context => LoadPredefinedNodes(context, stream), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogErrorLoadingUaNodesFile(e, file, e.Message);
            }
        }

        LogCompletedProcessingUaNodesFiles();
    }

    /// <summary>
    /// Loads a node set from a file and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context, Stream stream)
    {
        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinary(context, stream, updateTables: true);

        // Add to node list for creation of pn.json.
        Nodes ??= new List<NodeWithIntervals>();
        Nodes = Nodes.Append(PluginNodesHelper.GetNodeWithIntervals(predefinedNodes[0].NodeId, _plcNodeManager)).ToList();

        return predefinedNodes;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading binary uanodes file {File}: {Error}")]
    partial void LogErrorLoadingUaNodesFile(Exception exception, string file, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed processing binary uanodes file(s)")]
    partial void LogCompletedProcessingUaNodesFiles();
}
