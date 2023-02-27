namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using static OpcPlc.Program;

/// <summary>
/// Nodes that are configured via binary *.PredefinedNodes.uanodes file.
/// To produce a binary *.PredefinedNodes.uanodes file from an XML NodeSet file, run the following command:
/// ModelCompiler.cmd <XML_NodeSet_FileName_Without_Extension>
/// </summary>
public class UaNodesPluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static string _nodesFileName;
    private PlcNodeManager _plcNodeManager;
    private Stream _uanodesFile;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "unf|uanodesfile=",
            "the binary *.PredefinedNodes.uanodes file that contains the nodes to be created in the OPC UA address space, use ModelCompiler.cmd <ModelDesign> to compile.",
            (string s) => _nodesFileName = s);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (!string.IsNullOrEmpty(_nodesFileName))
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
        if (!File.Exists(_nodesFileName))
        {
            string error = $"The file {_nodesFileName} does not exist.";
            Logger.Error(error);
            throw new Exception(error);
        }

        _uanodesFile = File.OpenRead(_nodesFileName);

        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

        var nodes = new List<NodeWithIntervals>();

        Logger.Information("Completed processing binary uanodes file");
    }

    /// <summary>
    /// Loads a node set from a file and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinary(context,
            _uanodesFile,
            updateTables: true);

        _uanodesFile.Close();

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(predefinedNodes[0].NodeId, _plcNodeManager),
        };

        return predefinedNodes;
    }
}
