namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OpcPlc.Program;

/// <summary>
/// Nodes that are configured via binary *.PredefinedNodes.uanodes file.
/// To produce a binary *.PredefinedNodes.uanodes file from an XML NodeSet file, run the following command:
/// ModelCompiler.cmd <XML_NodeSet_FileName_Without_Extension>
/// </summary>
public class UaNodesPluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static List<string> _nodesFileNames;
    private PlcNodeManager _plcNodeManager;
    private Stream _uanodesFile;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "unf|uanodesfile=",
            "the binary *.PredefinedNodes.uanodes file that contains the nodes to be created in the OPC UA address space (multiple comma separated filenames supported), use ModelCompiler.cmd <ModelDesign> to compile.",
            (string s) => _nodesFileNames = CliHelper.ParseListOfFileNames(s, "unf"));
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_nodesFileNames.Any())
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
            _uanodesFile = File.OpenRead(file);

            // Load complex types from binary uanodes file.
            _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);
        }

        Logger.Information("Completed processing binary uanodes file(s)");
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
        Nodes ??= new List<NodeWithIntervals>();
        Nodes = Nodes.Append(PluginNodesHelper.GetNodeWithIntervals(predefinedNodes[0].NodeId, _plcNodeManager)).ToList();

        return predefinedNodes;
    }
}
