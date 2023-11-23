namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OpcPlc.Program;

/// <summary>
/// Nodes that are configured via *.NodeSet2.xml file(s).
/// </summary>
public class NodeSet2PluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private List<string> _nodesFileNames;
    private PlcNodeManager _plcNodeManager;
    private Stream _nodes2File;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "ns2|nodeset2file=",
            "the *.NodeSet2.xml file that contains the nodes to be created in the OPC UA address space (multiple comma separated filenames supported)",
            (string s) => _nodesFileNames = CliHelper.ParseListOfFileNames(s, "ns2"));
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_nodesFileNames?.Any() ?? false)
        {
            AddNodes((FolderState)telemetryFolder.Parent); // Root.
        }
    }

    public void StartSimulation()
    {
    }

    public void StopSimulation()
    {
    }

    private void AddNodes(FolderState folder)
    {
        foreach (var file in _nodesFileNames)
        {
            try
            {
                _nodes2File = File.OpenRead(file);

                // Load complex types from NodeSet2 file.
                _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error loading NodeSet2 file {file}: {error}", file, e.Message);
            }
        }

        Logger.LogInformation("Completed processing NodeSet2 file(s)");
    }

    /// <summary>
    /// Loads a node set from a file and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();
        var namespaces = new Dictionary<string, string>();

        using (_nodes2File)
        {
            var importedNodeSet = Opc.Ua.Export.UANodeSet.Read(_nodes2File);

            if (importedNodeSet.NamespaceUris != null)
            {
                foreach (var namespaceUri in importedNodeSet.NamespaceUris)
                {
                    namespaces[namespaceUri] = namespaceUri;
                }
            }
            importedNodeSet.Import(_plcNodeManager.SystemContext, predefinedNodes);
        }

        // Add to node list for creation of pn.json.
        Nodes ??= new List<NodeWithIntervals>();
        Nodes = Nodes.Append(PluginNodesHelper.GetNodeWithIntervals(predefinedNodes[0].NodeId, _plcNodeManager)).ToList();

        return predefinedNodes;
    }
}
