namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.DI;

/// <summary>
/// Device Information Companion spec.
/// https://opcfoundation.org/developer-tools/documents/view/197
/// </summary>
public class ADeviceInformationPluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static bool _isEnabled;
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
