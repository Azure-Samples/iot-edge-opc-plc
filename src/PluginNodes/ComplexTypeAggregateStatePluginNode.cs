namespace OpcPlc.PluginNodes;

using AggregateStateModel;
using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using static OpcPlc.Program;

/// <summary>
/// Complex type Aggregate State node.
/// </summary>
public class ComplexTypeAggregateStatePluginNode : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    private AggregateStateState _node;
    private ITimer _nodeGenerator;
    private float[] _pressureValues = new[] { 0, 0.5f, 100.0f };
    private ulong _pressureIndex = 0;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "ctas|complextypeaggregatestate",
            $"add complex type (aggregate state) to address space.\nDefault: {_isEnabled}",
            (string s) => _isEnabled = s != null);
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
        if (_isEnabled)
        {
            _nodeGenerator = TimeService.NewTimer(UpdateAggregateState, 5000);
        }
    }

    public void StopSimulation()
    {
        if (_nodeGenerator != null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes(FolderState methodsFolder)
    {
        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

        var passiveNode = (BaseObjectState)_plcNodeManager.FindPredefinedNode(
            new NodeId(AggregateStateModel.Objects.AggregateState1, 
            _plcNodeManager.NamespaceIndexes[(int)NamespaceType.AggregateState]), 
            typeof(BaseObjectState));

        // Convert to node that can be manipulated within the server.
        _node = new AggregateStateState(null);
        _node.Create(_plcNodeManager.SystemContext, passiveNode);

        _plcNodeManager.AddPredefinedNode(_node);

        Nodes = new List<NodeWithIntervals>
        {
            new NodeWithIntervals
            {
                NodeId = "AggregateState",
                Namespace = OpcPlc.Namespaces.OpcPlcAggregateState,
            },
        };
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinaryResource(context,
            "AggregateState/AggregateStateModel.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
            typeof(PlcNodeManager).GetTypeInfo().Assembly,
            updateTables: true);

        return predefinedNodes;
    }

    public void UpdateAggregateState(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _pressureIndex++;

        var newValue = new AggregateStateDataType
        {
            Temperature = 0,
            Pressure = _pressureValues[_pressureIndex % 3],
        };

        _node.AggregateState.Value = newValue;
        _node.AggregateState.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
    }
}
