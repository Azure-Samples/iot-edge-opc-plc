namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Timers;

/// <summary>
/// Stacklight simulation with 3 lamp elements (Red, Yellow, Green) driven by StacklightMode.
/// Uses proper OPC UA IA companion specification type definitions (StacklightType) so
/// that OPC UA clients performing DI discovery can detect this as a typed device.
/// StacklightType implements IDeviceHealthType from the DI companion spec.
/// </summary>
public partial class StacklightPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    private OpcPlc.ITimer _nodeGenerator;

    // Stacklight properties.
    private BaseDataVariableState _stacklightModeNode;

    // Lamp nodes: [0] = Red, [1] = Yellow, [2] = Green.
    private readonly BaseDataVariableState[] _signalOnNodes = new BaseDataVariableState[3];
    private readonly BaseDataVariableState[] _signalColorNodes = new BaseDataVariableState[3];
    private readonly BaseDataVariableState[] _signalModeNodes = new BaseDataVariableState[3];

    // Simulator lamp mode values (mapped to StacklightOperationMode enum for the control variable).
    private const int LampModeRed = 0;
    private const int LampModeYellow = 1;
    private const int LampModeGreen = 2;

    // SignalColor enum values (IA spec DataType ns=IA;i=3004).
    private const int SignalColorRed = 1;
    private const int SignalColorGreen = 2;
    private const int SignalColorYellow = 4;

    // SignalModeLight enum values (IA spec DataType ns=IA;i=3005).
    private const int SignalModeContinuous = 0;

    // IA companion spec type NodeId numeric identifiers.
    private const uint IaStacklightTypeId = 1010;
    private const uint IaStackElementLightTypeId = 1006;
    private const uint IaStacklightOperationModeId = 3002;
    private const uint IaSignalColorId = 3004;
    private const uint IaSignalModeLightId = 3005;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "sl|stacklight",
            $"add stacklight simulation to address space.\nDefault: {_isEnabled}",
            (string s) => _isEnabled = s != null);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;
        AddNodes(telemetryFolder);
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _nodeGenerator = _timeService.NewTimer(UpdateStacklight, intervalInMilliseconds: 1000);
        }
    }

    public void StopSimulation()
    {
        if (_nodeGenerator is not null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes(FolderState telemetryFolder)
    {
        ushort iaNamespaceIndex = (ushort)_plcNodeManager.Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.IA);
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        // Create stacklight object using IA StacklightType (implements IDeviceHealthType for DI discovery).
        var stacklightObject = new BaseObjectState(telemetryFolder)
        {
            SymbolicName = "Stacklight",
            NodeId = new NodeId("Stacklight", appNamespaceIndex),
            BrowseName = new QualifiedName("Stacklight", appNamespaceIndex),
            DisplayName = new LocalizedText("en", "Stacklight"),
            TypeDefinitionId = new NodeId(IaStacklightTypeId, iaNamespaceIndex),
            ReferenceTypeId = ReferenceTypes.Organizes,
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None,
        };
        telemetryFolder.AddChild(stacklightObject);

        // StacklightMode property (IA BrowseName, IA StacklightOperationMode DataType).
        _stacklightModeNode = new BaseDataVariableState(stacklightObject)
        {
            SymbolicName = "StacklightMode",
            NodeId = new NodeId("Stacklight_StacklightMode", appNamespaceIndex),
            BrowseName = new QualifiedName("StacklightMode", iaNamespaceIndex),
            DisplayName = new LocalizedText("en", "StacklightMode"),
            Description = new LocalizedText("Controls the active lamp (0=Red, 1=Yellow, 2=Green)."),
            TypeDefinitionId = VariableTypeIds.PropertyType,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            DataType = new NodeId(IaStacklightOperationModeId, iaNamespaceIndex),
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = LampModeRed,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };
        _stacklightModeNode.OnSimpleWriteValue = OnWriteStacklightMode;
        stacklightObject.AddChild(_stacklightModeNode);

        AddLampElement(stacklightObject, 0, "Red", SignalColorRed, iaNamespaceIndex, appNamespaceIndex);
        AddLampElement(stacklightObject, 1, "Yellow", SignalColorYellow, iaNamespaceIndex, appNamespaceIndex);
        AddLampElement(stacklightObject, 2, "Green", SignalColorGreen, iaNamespaceIndex, appNamespaceIndex);

        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[0].NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[1].NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[2].NodeId, _plcNodeManager),
        };

        ApplyStacklightMode();
    }

    private void AddLampElement(BaseObjectState parent, int index, string colorName, int signalColor, ushort iaNamespaceIndex, ushort appNamespaceIndex)
    {
        string lampId = $"Stacklight_Lamp_{colorName}";

        // Lamp object using IA StackElementLightType, connected via HasOrderedComponent.
        var lampObject = new BaseObjectState(parent)
        {
            SymbolicName = $"Lamp_{colorName}",
            NodeId = new NodeId(lampId, appNamespaceIndex),
            BrowseName = new QualifiedName($"Lamp_{colorName}", appNamespaceIndex),
            DisplayName = new LocalizedText("en", $"Lamp_{colorName}"),
            TypeDefinitionId = new NodeId(IaStackElementLightTypeId, iaNamespaceIndex),
            ReferenceTypeId = ReferenceTypeIds.HasOrderedComponent,
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            EventNotifier = EventNotifiers.None,
        };
        parent.AddChild(lampObject);

        // NumberInList property (mandatory on StackElementType, base OPC UA BrowseName).
        var numberInList = new BaseDataVariableState(lampObject)
        {
            SymbolicName = "NumberInList",
            NodeId = new NodeId($"{lampId}_NumberInList", appNamespaceIndex),
            BrowseName = new QualifiedName("NumberInList"),
            DisplayName = new LocalizedText("en", "NumberInList"),
            TypeDefinitionId = VariableTypeIds.PropertyType,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            DataType = DataTypeIds.UInteger,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = (uint)(index + 1),
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };
        lampObject.AddChild(numberInList);

        // SignalOn property (IA BrowseName, optional on StackElementType).
        _signalOnNodes[index] = new BaseDataVariableState(lampObject)
        {
            SymbolicName = "SignalOn",
            NodeId = new NodeId($"{lampId}_SignalOn", appNamespaceIndex),
            BrowseName = new QualifiedName("SignalOn", iaNamespaceIndex),
            DisplayName = new LocalizedText("en", "SignalOn"),
            Description = new LocalizedText("Indicates if the lamp is currently switched on."),
            TypeDefinitionId = VariableTypeIds.PropertyType,
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            DataType = DataTypeIds.Boolean,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = false,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };
        lampObject.AddChild(_signalOnNodes[index]);

        // SignalColor variable (IA BrowseName, IA SignalColor DataType, optional on StackElementLightType).
        _signalColorNodes[index] = new BaseDataVariableState(lampObject)
        {
            SymbolicName = "SignalColor",
            NodeId = new NodeId($"{lampId}_SignalColor", appNamespaceIndex),
            BrowseName = new QualifiedName("SignalColor", iaNamespaceIndex),
            DisplayName = new LocalizedText("en", "SignalColor"),
            Description = new LocalizedText("Indicates the colour the lamp has when switched on."),
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            DataType = new NodeId(IaSignalColorId, iaNamespaceIndex),
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = signalColor,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };
        lampObject.AddChild(_signalColorNodes[index]);

        // SignalMode variable (IA BrowseName, IA SignalModeLight DataType, optional on StackElementLightType).
        _signalModeNodes[index] = new BaseDataVariableState(lampObject)
        {
            SymbolicName = "SignalMode",
            NodeId = new NodeId($"{lampId}_SignalMode", appNamespaceIndex),
            BrowseName = new QualifiedName("SignalMode", iaNamespaceIndex),
            DisplayName = new LocalizedText("en", "SignalMode"),
            Description = new LocalizedText("Shows in what way the lamp is used."),
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            DataType = new NodeId(IaSignalModeLightId, iaNamespaceIndex),
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = AccessLevels.CurrentReadOrWrite,
            Value = SignalModeContinuous,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };
        lampObject.AddChild(_signalModeNodes[index]);
    }

    private void UpdateStacklight(object state, ElapsedEventArgs elapsedEventArgs)
    {
        ApplyStacklightMode();
    }

    private void ApplyStacklightMode()
    {
        int stacklightMode = Convert.ToInt32(_stacklightModeNode?.Value ?? LampModeRed);

        switch (stacklightMode)
        {
            case LampModeRed:
                SetLampState(0, signalOn: true, SignalModeContinuous);
                SetLampState(1, signalOn: false, SignalModeContinuous);
                SetLampState(2, signalOn: false, SignalModeContinuous);
                break;
            case LampModeYellow:
                SetLampState(0, signalOn: false, SignalModeContinuous);
                SetLampState(1, signalOn: true, SignalModeContinuous);
                SetLampState(2, signalOn: false, SignalModeContinuous);
                break;
            case LampModeGreen:
                SetLampState(0, signalOn: false, SignalModeContinuous);
                SetLampState(1, signalOn: false, SignalModeContinuous);
                SetLampState(2, signalOn: true, SignalModeContinuous);
                break;
            default:
                SetLampState(0, signalOn: false, SignalModeContinuous);
                SetLampState(1, signalOn: false, SignalModeContinuous);
                SetLampState(2, signalOn: false, SignalModeContinuous);
                break;
        }
    }

    private ServiceResult OnWriteStacklightMode(ISystemContext context, NodeState node, ref object value)
    {
        try
        {
            _stacklightModeNode.Value = value;
            _stacklightModeNode.Timestamp = _timeService.Now();
            _stacklightModeNode.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);

            ApplyStacklightMode();
            return ServiceResult.Good;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing StacklightMode variable");
            return ServiceResult.Create(ex, StatusCodes.Bad, "Error writing StacklightMode variable.");
        }
    }

    /// <summary>
    /// Gets the current state of all lamps as a JSON-serializable structure.
    /// </summary>
    public StacklightState GetState()
    {
        if (_signalOnNodes[0] is null)
        {
            return null;
        }

        return new StacklightState
        {
            StacklightMode = Convert.ToInt32(_stacklightModeNode?.Value ?? 0),
            Lamps =
            [
                GetLampState(0, "Red"),
                GetLampState(1, "Yellow"),
                GetLampState(2, "Green"),
            ],
        };
    }

    private LampState GetLampState(int index, string name)
    {
        return new LampState
        {
            Name = name,
            SignalOn = (bool)(_signalOnNodes[index]?.Value ?? false),
            SignalColor = Convert.ToInt32(_signalColorNodes[index]?.Value ?? 0),
            SignalMode = Convert.ToInt32(_signalModeNodes[index]?.Value ?? 0),
        };
    }

    public sealed class StacklightState
    {
        public int StacklightMode { get; set; }

        public LampState[] Lamps { get; set; }
    }

    public sealed class LampState
    {
        public string Name { get; set; }

        public bool SignalOn { get; set; }

        public int SignalColor { get; set; }

        public int SignalMode { get; set; }
    }

    private void SetLampState(int index, bool signalOn, int signalMode)
    {
        _signalOnNodes[index].Value = signalOn;
        _signalOnNodes[index].Timestamp = _timeService.Now();
        _signalOnNodes[index].ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);

        _signalModeNodes[index].Value = signalMode;
        _signalModeNodes[index].Timestamp = _timeService.Now();
        _signalModeNodes[index].ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }
}
