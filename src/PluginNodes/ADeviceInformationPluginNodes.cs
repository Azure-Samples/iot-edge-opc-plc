namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Device Information Companion spec.
/// https://opcfoundation.org/developer-tools/documents/view/197
/// The prefix "A" in the class name is used to ensure that this plugin is loaded first.
/// </summary>
public class ADeviceInformationPluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        _isEnabled = true;
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            AddNodes(methodsFolder);
        }
    }

    public void StartSimulation()
    {
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);
    }

    public void StopSimulation()
    {
    }

    private void AddNodes(FolderState methodsFolder)
    {
        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinaryResource(context,
            "CompanionSpecs/DI/Opc.Ua.DI.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
            typeof(PlcNodeManager).GetTypeInfo().Assembly,
            updateTables: true);

        return predefinedNodes;
    }
}
