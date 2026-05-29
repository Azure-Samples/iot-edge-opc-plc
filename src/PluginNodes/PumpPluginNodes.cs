namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Timers;

/// <summary>
/// Simulates two pumps based on a trimmed subset of the OPC UA Pumps companion specification.
/// Each pump is an instance of PumpType, carries a DI Identification object (so it is
/// discoverable through DI discovery and type-based discovery) and periodically raises
/// PumpEventType events.
/// </summary>
public partial class PumpPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private const int PumpCount = 2;

    // NodeIds of the pump types defined in the trimmed Pumps NodeSet.
    private const uint PumpTypeId = 1052;
    private const uint PumpIdentificationTypeId = 1005;
    private const uint PumpEventTypeId = 1100;

    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    private OpcPlc.ITimer _nodeGenerator;

    private ushort _pumpsNamespaceIndex;
    private ushort _diNamespaceIndex;

    private readonly BaseObjectState[] _pumpObjects = new BaseObjectState[PumpCount];
    private readonly BaseDataVariableState[] _flowRateNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _pressureNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _rotationalSpeedNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _motorTemperatureNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _deviceHealthNodes = new BaseDataVariableState[PumpCount];

    private readonly Random _random = new();
    private uint _eventCounter;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "pu|pumps",
            $"add pump simulation (2 pumps based on the OPC UA Pumps companion spec) to address space.\nDefault: {_isEnabled}",
            (string s) => _isEnabled = s != null);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            AddNodes(telemetryFolder);
        }
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _nodeGenerator = _timeService.NewTimer(UpdatePumps, intervalInMilliseconds: 1000);
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
        _pumpsNamespaceIndex = (ushort)_plcNodeManager.Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.Pumps);
        _diNamespaceIndex = (ushort)_plcNodeManager.Server.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI);

        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        FolderState pumpsFolder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Pumps",
            name: "Pumps",
            NamespaceType.OpcPlcApplications);

        var nodes = new List<NodeWithIntervals>();

        for (int i = 0; i < PumpCount; i++)
        {
            string pumpName = $"Pump{i + 1}";

            var pumpObject = new BaseObjectState(pumpsFolder) {
                NodeId = new NodeId(pumpName, appNamespaceIndex),
                BrowseName = new QualifiedName(pumpName, appNamespaceIndex),
                DisplayName = new LocalizedText("en", pumpName),
                Description = new LocalizedText("en", $"Pump #{i + 1}"),
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = new NodeId(PumpTypeId, _pumpsNamespaceIndex),
                EventNotifier = EventNotifiers.SubscribeToEvents,
            };

            AddIdentification(pumpObject, pumpName, i);
            _deviceHealthNodes[i] = AddDeviceHealth(pumpObject, pumpName);

            _flowRateNodes[i] = AddTelemetry(pumpObject, pumpName, "FlowRate", appNamespaceIndex, defaultValue: 50.0);
            _pressureNodes[i] = AddTelemetry(pumpObject, pumpName, "Pressure", appNamespaceIndex, defaultValue: 2.0);
            _rotationalSpeedNodes[i] = AddTelemetry(pumpObject, pumpName, "RotationalSpeed", appNamespaceIndex, defaultValue: 1500.0);
            _motorTemperatureNodes[i] = AddTelemetry(pumpObject, pumpName, "MotorTemperature", appNamespaceIndex, defaultValue: 40.0);

            pumpsFolder.AddChild(pumpObject);
            _plcNodeManager.AddPredefinedNode(pumpObject);

            _pumpObjects[i] = pumpObject;

            nodes.Add(PluginNodesHelper.GetNodeWithIntervals(_flowRateNodes[i].NodeId, _plcNodeManager));
        }

        Nodes = nodes;
    }

    private void AddIdentification(BaseObjectState pumpObject, string pumpName, int index)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var identification = new BaseObjectState(pumpObject) {
            NodeId = new NodeId($"{pumpName}_Identification", appNamespaceIndex),
            BrowseName = new QualifiedName(Opc.Ua.DI.BrowseNames.Identification, _diNamespaceIndex),
            DisplayName = new LocalizedText("en", Opc.Ua.DI.BrowseNames.Identification),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = new NodeId(PumpIdentificationTypeId, _pumpsNamespaceIndex),
        };

        AddProperty(identification, $"{pumpName}_Manufacturer", Opc.Ua.DI.BrowseNames.Manufacturer, DataTypeIds.LocalizedText, new LocalizedText("en", "Contoso Pumps"));
        AddProperty(identification, $"{pumpName}_Model", Opc.Ua.DI.BrowseNames.Model, DataTypeIds.LocalizedText, new LocalizedText("en", $"CP-{1000 + index}"));
        AddProperty(identification, $"{pumpName}_SerialNumber", Opc.Ua.DI.BrowseNames.SerialNumber, DataTypeIds.String, $"SN-{index + 1:D6}");

        pumpObject.AddChild(identification);
    }

    private void AddProperty(BaseObjectState parent, string nodeId, string browseName, NodeId dataType, object value)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var property = new PropertyState(parent) {
            NodeId = new NodeId(nodeId, appNamespaceIndex),
            BrowseName = new QualifiedName(browseName, _diNamespaceIndex),
            DisplayName = new LocalizedText("en", browseName),
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            TypeDefinitionId = VariableTypeIds.PropertyType,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = value,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };

        parent.AddChild(property);
    }

    private BaseDataVariableState AddDeviceHealth(BaseObjectState pumpObject, string pumpName)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var deviceHealth = new BaseDataVariableState(pumpObject) {
            NodeId = new NodeId($"{pumpName}_DeviceHealth", appNamespaceIndex),
            BrowseName = new QualifiedName(Opc.Ua.DI.BrowseNames.DeviceHealth, _diNamespaceIndex),
            DisplayName = new LocalizedText("en", Opc.Ua.DI.BrowseNames.DeviceHealth),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            DataType = new NodeId(Opc.Ua.DI.DataTypes.DeviceHealthEnumeration, _diNamespaceIndex),
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = Opc.Ua.DI.DeviceHealthEnumeration.NORMAL,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };

        pumpObject.AddChild(deviceHealth);

        return deviceHealth;
    }

    private BaseDataVariableState AddTelemetry(BaseObjectState pumpObject, string pumpName, string name, ushort appNamespaceIndex, double defaultValue)
    {
        var telemetry = new BaseDataVariableState(pumpObject) {
            NodeId = new NodeId($"{pumpName}_{name}", appNamespaceIndex),
            BrowseName = new QualifiedName(name, appNamespaceIndex),
            DisplayName = new LocalizedText("en", name),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            DataType = DataTypeIds.Double,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = defaultValue,
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };

        pumpObject.AddChild(telemetry);

        return telemetry;
    }

    private void UpdatePumps(object state, ElapsedEventArgs elapsedEventArgs)
    {
        for (int i = 0; i < PumpCount; i++)
        {
            double flowRate = 40.0 + (_random.NextDouble() * 20.0);
            double pressure = 1.5 + (_random.NextDouble() * 1.5);
            double rotationalSpeed = 1400.0 + (_random.NextDouble() * 200.0);
            double motorTemperature = 35.0 + (_random.NextDouble() * 20.0);

            SetValue(_flowRateNodes[i], flowRate);
            SetValue(_pressureNodes[i], pressure);
            SetValue(_rotationalSpeedNodes[i], rotationalSpeed);
            SetValue(_motorTemperatureNodes[i], motorTemperature);

            RaisePumpEvent(i, flowRate, pressure);
        }
    }

    private void SetValue(BaseDataVariableState node, double value)
    {
        node.Value = value;
        node.Timestamp = _timeService.Now();
        node.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private void RaisePumpEvent(int index, double flowRate, double pressure)
    {
        BaseObjectState pumpObject = _pumpObjects[index];
        if (pumpObject is null)
        {
            return;
        }

        string pumpId = $"Pump{index + 1}";

        var pumpEventTypeId = new NodeId(PumpEventTypeId, _pumpsNamespaceIndex);

        var pumpEvent = new BaseEventState(pumpObject);
        pumpEvent.Initialize(
            _plcNodeManager.SystemContext,
            source: pumpObject,
            EventSeverity.Medium,
            new LocalizedText("en", $"{pumpId} telemetry: flow={flowRate:F1}, pressure={pressure:F2}"));

        // Set the concrete event type (after Initialize) so that OfType event filters match.
        pumpEvent.TypeDefinitionId = pumpEventTypeId;

        pumpEvent.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.EventType, pumpEventTypeId, copy: false);
        pumpEvent.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.SourceNode, pumpObject.NodeId, copy: false);
        pumpEvent.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.SourceName, pumpId, copy: false);

        AddEventField(pumpEvent, "PumpId", DataTypeIds.String, pumpId);
        AddEventField(pumpEvent, "FlowRate", DataTypeIds.Double, flowRate);
        AddEventField(pumpEvent, "Pressure", DataTypeIds.Double, pressure);

        _eventCounter++;

        // Report the event to all clients subscribed to the Server object for events.
        _plcNodeManager.Server.ReportEvent(pumpEvent);
    }

    private void AddEventField(BaseEventState pumpEvent, string browseName, NodeId dataType, object value)
    {
        var field = new PropertyState(pumpEvent) {
            BrowseName = new QualifiedName(browseName, _pumpsNamespaceIndex),
            DisplayName = new LocalizedText("en", browseName),
            ReferenceTypeId = ReferenceTypeIds.HasProperty,
            TypeDefinitionId = VariableTypeIds.PropertyType,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            Value = value,
        };

        pumpEvent.AddChild(field);
    }
}
