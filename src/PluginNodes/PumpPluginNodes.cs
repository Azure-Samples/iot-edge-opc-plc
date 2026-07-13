namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.CompanionSpecs.Pumps;
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

    // NodeIds of the pump Configuration group types (Pumps namespace).
    private const uint ConfigurationGroupTypeId = 1024;
    private const uint DesignTypeId = 1020;
    private const uint ImplementationTypeId = 1023;
    private const uint SystemRequirementsTypeId = 1022;

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

    // Type-conformant variables whose BrowseNames match real PumpType members (Pumps namespace).
    private readonly BaseDataVariableState[] _volumeFlowRateNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _ratedDifferentialPressureNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _maximumOutletPressureNodes = new BaseDataVariableState[PumpCount];
    private readonly BaseDataVariableState[] _maximumInletPressureNodes = new BaseDataVariableState[PumpCount];

    private readonly BaseObjectState[] _pumpEventsFolders = new BaseObjectState[PumpCount];

    private readonly Random _random = new();
    private uint _eventCounter;

    /// <summary>
    /// Gets a value indicating whether the pump simulation is enabled via the --pumps option.
    /// </summary>
    public bool IsEnabled => _isEnabled;

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

        var nodes = new List<NodeWithIntervals>();

        for (int i = 0; i < PumpCount; i++)
        {
            string pumpName = $"Pump{i + 1}";

            // Create the pump object parented under the DI DeviceSet folder (see AddNodeToDeviceSet below)
            // rather than the Telemetry folder, so DI-aware clients discover it via the standard device topology.
            var pumpObject = new BaseObjectState(parent: null) {
                SymbolicName = pumpName,
                NodeId = new NodeId(pumpName, appNamespaceIndex),
                BrowseName = new QualifiedName(pumpName, appNamespaceIndex),
                DisplayName = new LocalizedText("en", pumpName),
                Description = new LocalizedText("en", $"Pump #{i + 1}"),
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = new NodeId(PumpTypeId, _pumpsNamespaceIndex),
            };

            AddIdentification(pumpObject, pumpName, i);
            _deviceHealthNodes[i] = AddDeviceHealth(pumpObject, pumpName);

            // Static Configuration group (Design, Implementation, SystemRequirements) mirroring
            // the PumpType.Configuration functional group in the Pumps companion specification.
            // The simulated pump variables live inside Configuration/SystemRequirements.
            AddConfiguration(pumpObject, pumpName, i);

            // Events folder: the event notifier that generates PumpEventType (mirrors PumpType.Events).
            _pumpEventsFolders[i] = AddEventsFolder(pumpObject, pumpName);

            // Link the pump under the DI DeviceSet folder and register it in the address space.
            _plcNodeManager.AddNodeToDeviceSet(pumpObject);

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

    /// <summary>
    /// Adds the static Configuration functional group (typed as ConfigurationGroupType) with the
    /// Design, Implementation and SystemRequirements sub-groups, mirroring PumpType.Configuration
    /// in the Pumps companion specification. Only SystemRequirements is populated with members;
    /// Design and Implementation are exposed as (empty) group folders since their members are
    /// optional in the specification.
    /// </summary>
    private void AddConfiguration(BaseObjectState pumpObject, string pumpName, int index)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var configuration = new BaseObjectState(pumpObject) {
            NodeId = new NodeId($"{pumpName}_Configuration", appNamespaceIndex),
            BrowseName = new QualifiedName("Configuration", _diNamespaceIndex),
            DisplayName = new LocalizedText("en", "Configuration"),
            Description = new LocalizedText("en", "Static design, system requirements, and implementation data of the pump."),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = new NodeId(ConfigurationGroupTypeId, _pumpsNamespaceIndex),
        };

        AddConfigurationGroup(configuration, pumpName, "Design", DesignTypeId);
        AddConfigurationGroup(configuration, pumpName, "Implementation", ImplementationTypeId);

        BaseObjectState systemRequirements = AddConfigurationGroup(configuration, pumpName, "SystemRequirements", SystemRequirementsTypeId);
        Dictionary<string, BaseDataVariableState> members = AddSystemRequirements(systemRequirements, pumpName);

        // The simulated pump variables live inside SystemRequirements. Variables whose BrowseName
        // already exists there (imported from the Pumps NodeSet, e.g. MaximumInletPressure and
        // MaximumOutletPressure) are reused instead of being added again, to avoid duplication.
        _flowRateNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "FlowRate", appNamespaceIndex, defaultValue: 50.0);
        _pressureNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "Pressure", appNamespaceIndex, defaultValue: 2.0);
        _rotationalSpeedNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "RotationalSpeed", appNamespaceIndex, defaultValue: 1500.0);
        _motorTemperatureNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "MotorTemperature", appNamespaceIndex, defaultValue: 40.0);

        // Type-conformant variables whose BrowseNames match real PumpType members (Pumps namespace).
        _volumeFlowRateNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "VolumeFlowRate", _pumpsNamespaceIndex, defaultValue: 50.0);
        _ratedDifferentialPressureNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "RatedDifferentialPressure", _pumpsNamespaceIndex, defaultValue: 2.0);
        _maximumOutletPressureNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "MaximumOutletPressure", _pumpsNamespaceIndex, defaultValue: 3.0);
        _maximumInletPressureNodes[index] = GetOrAddSystemRequirement(systemRequirements, members, pumpName, "MaximumInletPressure", _pumpsNamespaceIndex, defaultValue: 1.0);

        pumpObject.AddChild(configuration);
    }

    /// <summary>
    /// Adds a Configuration sub-group (Design, Implementation or SystemRequirements) under the
    /// Configuration object and returns it so members can be attached.
    /// </summary>
    private BaseObjectState AddConfigurationGroup(BaseObjectState configuration, string pumpName, string groupName, uint typeId)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var group = new BaseObjectState(configuration) {
            NodeId = new NodeId($"{pumpName}_{groupName}", appNamespaceIndex),
            BrowseName = new QualifiedName(groupName, _pumpsNamespaceIndex),
            DisplayName = new LocalizedText("en", groupName),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = new NodeId(typeId, _pumpsNamespaceIndex),
        };

        configuration.AddChild(group);

        return group;
    }

    /// <summary>
    /// Adds the SystemRequirements members (BrowseNames in the Pumps namespace) as defined by
    /// SystemRequirementsType in the Pumps companion specification. The member list (BrowseName and
    /// DataType) is imported directly from the Pumps NodeSet2 rather than hardcoded here.
    /// </summary>
    private Dictionary<string, BaseDataVariableState> AddSystemRequirements(BaseObjectState systemRequirements, string pumpName)
    {
        var members = new Dictionary<string, BaseDataVariableState>();

        foreach (PumpTypeMember member in PumpNodeManager.GetSystemRequirementsMembers())
        {
            NodeId dataType = ExpandedNodeId.ToNodeId(member.DataType, _plcNodeManager.Server.NamespaceUris);
            members[member.BrowseName] = AddSystemRequirement(systemRequirements, pumpName, member.BrowseName, dataType, member.ValueRank);
        }

        return members;
    }

    /// <summary>
    /// Adds a single SystemRequirements member variable. The NodeId stays in the application
    /// namespace so it is unique per pump instance, while the BrowseName is in the Pumps namespace
    /// to match the SystemRequirementsType member of the companion specification.
    /// </summary>
    private BaseDataVariableState AddSystemRequirement(BaseObjectState parent, string pumpName, string browseName, NodeId dataType, int valueRank)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var node = new BaseDataVariableState(parent) {
            NodeId = new NodeId($"{pumpName}_SystemRequirements_{browseName}", appNamespaceIndex),
            BrowseName = new QualifiedName(browseName, _pumpsNamespaceIndex),
            DisplayName = new LocalizedText("en", browseName),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            DataType = dataType,
            ValueRank = valueRank,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = GetDefaultValue(dataType),
            StatusCode = StatusCodes.Good,
            Timestamp = _timeService.UtcNow(),
        };

        parent.AddChild(node);

        return node;
    }

    /// <summary>
    /// Returns the SystemRequirements member with the given BrowseName if it already exists (imported
    /// from the Pumps NodeSet), otherwise creates a new simulated Double variable under
    /// SystemRequirements. This avoids duplicating members that the companion specification defines.
    /// </summary>
    private BaseDataVariableState GetOrAddSystemRequirement(BaseObjectState systemRequirements, Dictionary<string, BaseDataVariableState> members, string pumpName, string browseName, ushort namespaceIndex, double defaultValue)
    {
        if (members.TryGetValue(browseName, out BaseDataVariableState existing))
        {
            return existing;
        }

        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var node = new BaseDataVariableState(systemRequirements) {
            NodeId = new NodeId($"{pumpName}_{browseName}", appNamespaceIndex),
            BrowseName = new QualifiedName(browseName, namespaceIndex),
            DisplayName = new LocalizedText("en", browseName),
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

        systemRequirements.AddChild(node);
        members[browseName] = node;

        return node;
    }

    /// <summary>
    /// Returns a sensible default value for a SystemRequirements member based on its data type.
    /// Pumps-namespace data types (enums and option sets) and non-trivial types are left unset (null).
    /// </summary>
    private object GetDefaultValue(NodeId dataType)
    {
        if (dataType.NamespaceIndex != 0)
        {
            // Enum/option-set/structure data types defined in the Pumps namespace: leave unset.
            return null;
        }

        if (dataType == DataTypeIds.Double)
        {
            return 0.0;
        }

        if (dataType == DataTypeIds.Boolean)
        {
            return false;
        }

        return null;
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

    /// <summary>
    /// Adds an Events folder under the pump that acts as the event notifier and declares (via a
    /// GeneratesEvent reference) that it generates PumpEventType, mirroring the PumpType.Events
    /// functional group in the Pumps companion specification.
    /// </summary>
    private BaseObjectState AddEventsFolder(BaseObjectState pumpObject, string pumpName)
    {
        ushort appNamespaceIndex = _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];

        var eventsFolder = new BaseObjectState(pumpObject) {
            NodeId = new NodeId($"{pumpName}_Events", appNamespaceIndex),
            BrowseName = new QualifiedName("Events", _pumpsNamespaceIndex),
            DisplayName = new LocalizedText("en", "Events"),
            Description = new LocalizedText("en", "States, alarms, and conditions of a pump."),
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            TypeDefinitionId = ObjectTypeIds.BaseObjectType,
            EventNotifier = EventNotifiers.SubscribeToEvents,
        };

        // Declare that this folder generates PumpEventType (mirrors the GeneratesEvent reference
        // on PumpType.Events in the NodeSet).
        eventsFolder.AddReference(ReferenceTypeIds.GeneratesEvent, isInverse: false, new NodeId(PumpEventTypeId, _pumpsNamespaceIndex));

        pumpObject.AddChild(eventsFolder);

        // Link the pump root to the Events folder as an event source so event discovery can
        // propagate from the device root (HasEventSource) to the folder that owns the events.
        pumpObject.AddReference(ReferenceTypeIds.HasEventSource, isInverse: false, eventsFolder.NodeId);

        return eventsFolder;
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

            // Update the type-conformant PumpType-member variables.
            SetValue(_volumeFlowRateNodes[i], flowRate);
            SetValue(_ratedDifferentialPressureNodes[i], pressure);
            SetValue(_maximumOutletPressureNodes[i], pressure + 0.5);
            SetValue(_maximumInletPressureNodes[i], pressure - 0.5);

            // Derive DeviceHealth from the motor temperature (mirrors the Boiler2 approach).
            SetDeviceHealth(i, motorTemperature);

            RaisePumpEvent(i, flowRate, pressure);
        }
    }

    /// <summary>
    /// Maps the current motor temperature to a DeviceHealthEnumeration value and publishes it on the
    /// pump's DeviceHealth variable, mirroring the temperature-based logic used by Boiler2.
    /// </summary>
    private void SetDeviceHealth(int index, double motorTemperature)
    {
        const double baseTemperature = 38.0;
        const double targetTemperature = 48.0;
        const double overheatedTemperature = 52.0;

        Opc.Ua.DI.DeviceHealthEnumeration deviceHealth = motorTemperature switch {
            _ when motorTemperature >= baseTemperature && motorTemperature <= targetTemperature => Opc.Ua.DI.DeviceHealthEnumeration.NORMAL,
            _ when motorTemperature > targetTemperature && motorTemperature < overheatedTemperature => Opc.Ua.DI.DeviceHealthEnumeration.CHECK_FUNCTION,
            _ when motorTemperature >= overheatedTemperature => Opc.Ua.DI.DeviceHealthEnumeration.FAILURE,
            _ => Opc.Ua.DI.DeviceHealthEnumeration.OFF_SPEC,
        };

        SetDeviceHealthValue(_deviceHealthNodes[index], deviceHealth);
    }

    private void SetValue(BaseDataVariableState node, double value)
    {
        node.Value = value;
        node.Timestamp = _timeService.Now();
        node.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private void SetDeviceHealthValue(BaseDataVariableState node, Opc.Ua.DI.DeviceHealthEnumeration value)
    {
        node.Value = value;
        node.Timestamp = _timeService.Now();
        node.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private void RaisePumpEvent(int index, double flowRate, double pressure)
    {
        BaseObjectState pumpObject = _pumpObjects[index];
        BaseObjectState eventsFolder = _pumpEventsFolders[index];
        if (pumpObject is null || eventsFolder is null)
        {
            return;
        }

        string pumpId = $"Pump{index + 1}";

        var pumpEventTypeId = new NodeId(PumpEventTypeId, _pumpsNamespaceIndex);

        var pumpEvent = new BaseEventState(eventsFolder);
        pumpEvent.Initialize(
            _plcNodeManager.SystemContext,
            source: eventsFolder,
            EventSeverity.Medium,
            new LocalizedText("en", $"{pumpId} telemetry: flow={flowRate:F1}, pressure={pressure:F2}"));

        // Set the concrete event type (after Initialize) so that OfType event filters match.
        pumpEvent.TypeDefinitionId = pumpEventTypeId;

        pumpEvent.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.EventType, pumpEventTypeId, copy: false);

        // SourceNode and SourceName must refer to the same node: the pump that raised the event.
        // The Events folder is only the notifier that propagates the event, not its source.
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
