namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the Pump plugin, which uses a trimmed subset of the OPC UA Pumps companion spec
/// and supports DI discovery, type-based discovery and events.
/// </summary>
[TestFixture]
public class PumpTests : SimulatorTestsBase
{
    private const uint PumpTypeId = 1052;
    private const uint PumpIdentificationTypeId = 1005;

    public PumpTests() : base(["--pumps"])
    {
    }

    private ushort PumpsNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.Pumps);

    private ushort DiNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI);

    [TestCase("Pump1")]
    [TestCase("Pump2")]
    public async Task Pump_HasCorrectTypeDefinition(string pumpName)
    {
        var expectedTypeDefNodeId = new NodeId(PumpTypeId, PumpsNamespaceIndex);
        var actualTypeDefNodeId = await BrowseTypeDefinitionAsync(GetOpcPlcNodeId(pumpName)).ConfigureAwait(false);

        actualTypeDefNodeId.Should().Be(expectedTypeDefNodeId, "the pump should be typed as PumpType for type-based discovery");
    }

    [TestCase("Pump1")]
    [TestCase("Pump2")]
    public async Task Pump_HasDiIdentificationObject(string pumpName)
    {
        var identificationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId(pumpName),
            new QualifiedName(Opc.Ua.DI.BrowseNames.Identification, DiNamespaceIndex)).ConfigureAwait(false);

        identificationNodeId.Should().NotBeNull("the pump must expose a DI Identification object for DI discovery");

        var typeDefNodeId = await BrowseTypeDefinitionAsync(identificationNodeId).ConfigureAwait(false);
        typeDefNodeId.Should().Be(new NodeId(PumpIdentificationTypeId, PumpsNamespaceIndex), "the Identification object should be typed as PumpIdentificationType");
    }

    [TestCase("Pump1")]
    [TestCase("Pump2")]
    public async Task Pump_Identification_HasNameplateProperties(string pumpName)
    {
        var identificationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId(pumpName),
            new QualifiedName(Opc.Ua.DI.BrowseNames.Identification, DiNamespaceIndex)).ConfigureAwait(false);

        var manufacturerNodeId = await BrowseChildByBrowseNameAsync(
            identificationNodeId,
            new QualifiedName(Opc.Ua.DI.BrowseNames.Manufacturer, DiNamespaceIndex)).ConfigureAwait(false);

        manufacturerNodeId.Should().NotBeNull("the Identification object should expose the DI Manufacturer property");

        var manufacturer = (await ReadDataValueAsync(manufacturerNodeId).ConfigureAwait(false)).Value;
        manufacturer.Should().BeOfType<LocalizedText>().Which.Text.Should().Be("Contoso Pumps");
    }

    [Test]
    public async Task Pump_HasFlowRateTelemetry()
    {
        var flowRateNodeId = GetOpcPlcNodeId("Pump1_FlowRate");
        var node = await Session.ReadNodeAsync(flowRateNodeId).ConfigureAwait(false);

        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        ExpandedNodeId.ToNodeId(variableNode.DataType, Session.NamespaceUris).Should().Be(DataTypeIds.Double);
    }

    [TestCase("Pump1")]
    [TestCase("Pump2")]
    public async Task Pump_HasConfigurationWithSystemRequirements(string pumpName)
    {
        var configurationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId(pumpName),
            new QualifiedName("Configuration", DiNamespaceIndex)).ConfigureAwait(false);

        configurationNodeId.Should().NotBeNull("the pump should expose a Configuration functional group");

        foreach (var groupName in new[] { "Design", "Implementation", "SystemRequirements" })
        {
            var groupNodeId = await BrowseChildByBrowseNameAsync(
                configurationNodeId,
                new QualifiedName(groupName, PumpsNamespaceIndex)).ConfigureAwait(false);

            groupNodeId.Should().NotBeNull($"the Configuration group should contain a {groupName} sub-group");
        }
    }

    [TestCase("CompressionRatio")]
    [TestCase("Fluid")]
    [TestCase("MaximumOutletPressure")]
    [TestCase("WorkingTemperature")]
    public async Task Pump_SystemRequirements_HasMember(string memberName)
    {
        var configurationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId("Pump1"),
            new QualifiedName("Configuration", DiNamespaceIndex)).ConfigureAwait(false);

        var systemRequirementsNodeId = await BrowseChildByBrowseNameAsync(
            configurationNodeId,
            new QualifiedName("SystemRequirements", PumpsNamespaceIndex)).ConfigureAwait(false);

        var memberNodeId = await BrowseChildByBrowseNameAsync(
            systemRequirementsNodeId,
            new QualifiedName(memberName, PumpsNamespaceIndex)).ConfigureAwait(false);

        memberNodeId.Should().NotBeNull($"SystemRequirements should expose the {memberName} member");
    }

    [Test]
    public async Task Pump_SystemRequirements_FluidHasStringDataType()
    {
        var configurationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId("Pump1"),
            new QualifiedName("Configuration", DiNamespaceIndex)).ConfigureAwait(false);

        var systemRequirementsNodeId = await BrowseChildByBrowseNameAsync(
            configurationNodeId,
            new QualifiedName("SystemRequirements", PumpsNamespaceIndex)).ConfigureAwait(false);

        var fluidNodeId = await BrowseChildByBrowseNameAsync(
            systemRequirementsNodeId,
            new QualifiedName("Fluid", PumpsNamespaceIndex)).ConfigureAwait(false);

        var node = await Session.ReadNodeAsync(fluidNodeId).ConfigureAwait(false);
        var variableNode = node.Should().BeOfType<VariableNode>().Subject;
        ExpandedNodeId.ToNodeId(variableNode.DataType, Session.NamespaceUris).Should().Be(DataTypeIds.String);
    }

    [Test]
    public async Task Pump_SystemRequirements_ContainsSimulatedVariablesWithoutDuplication()
    {
        var systemRequirementsNodeId = await BrowseSystemRequirementsAsync("Pump1").ConfigureAwait(false);
        var childNames = (await BrowseChildrenAsync(systemRequirementsNodeId).ConfigureAwait(false))
            .Select(r => r.BrowseName.Name)
            .ToList();

        // The simulated variables moved from the pump root into SystemRequirements.
        childNames.Should().Contain(["FlowRate", "Pressure", "RotationalSpeed", "MotorTemperature", "VolumeFlowRate", "RatedDifferentialPressure"]);

        // Members already defined by the Pumps NodeSet are reused, not duplicated.
        childNames.Count(n => n == "MaximumInletPressure").Should().Be(1, "the NodeSet member should be reused, not duplicated");
        childNames.Count(n => n == "MaximumOutletPressure").Should().Be(1, "the NodeSet member should be reused, not duplicated");
    }

    [TestCase("Pump1")]
    [TestCase("Pump2")]
    public async Task Pump_Root_ExposesOnlyConfigurationDeviceHealthEventsAndIdentification(string pumpName)
    {
        // The Events folder is referenced both as a component and as an event source, so use the
        // distinct set of browse names to describe the pump root's children.
        var childNames = (await BrowseChildrenAsync(GetOpcPlcNodeId(pumpName)).ConfigureAwait(false))
            .Select(r => r.BrowseName.Name)
            .Distinct()
            .ToList();

        childNames.Should().BeEquivalentTo(["Configuration", "DeviceHealth", "Events", "Identification"]);
    }

    [Test]
    public async Task Pump_DeviceHealth_IsPopulatedFromSimulation()
    {
        var deviceHealthNodeId = GetOpcPlcNodeId("Pump1_DeviceHealth");

        // The initial DeviceHealth value is NORMAL.
        var initial = (Opc.Ua.DI.DeviceHealthEnumeration)(await ReadDataValueAsync(deviceHealthNodeId).ConfigureAwait(false)).Value;
        initial.Should().Be(Opc.Ua.DI.DeviceHealthEnumeration.NORMAL);

        // After the simulation timer fires, DeviceHealth is set to a valid enum value derived from
        // the motor temperature (mirrors the Boiler2 behavior).
        FireTimersWithPeriod(FromMilliseconds(1000), numberOfTimes: 1);

        var deviceHealth = (Opc.Ua.DI.DeviceHealthEnumeration)(await ReadDataValueAsync(deviceHealthNodeId).ConfigureAwait(false)).Value;
        deviceHealth.Should().BeOneOf(
            Opc.Ua.DI.DeviceHealthEnumeration.NORMAL,
            Opc.Ua.DI.DeviceHealthEnumeration.CHECK_FUNCTION,
            Opc.Ua.DI.DeviceHealthEnumeration.FAILURE,
            Opc.Ua.DI.DeviceHealthEnumeration.OFF_SPEC);
    }

    private async Task<NodeId> BrowseSystemRequirementsAsync(string pumpName)
    {
        var configurationNodeId = await BrowseChildByBrowseNameAsync(
            GetOpcPlcNodeId(pumpName),
            new QualifiedName("Configuration", DiNamespaceIndex)).ConfigureAwait(false);

        return await BrowseChildByBrowseNameAsync(
            configurationNodeId,
            new QualifiedName("SystemRequirements", PumpsNamespaceIndex)).ConfigureAwait(false);
    }

    private async Task<ReferenceDescriptionCollection> BrowseChildrenAsync(NodeId nodeId)
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = nodeId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        return results.Results[0].References;
    }

    private async Task<NodeId> BrowseTypeDefinitionAsync(NodeId nodeId)
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = nodeId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition,
            IncludeSubtypes = false,
            NodeClassMask = (uint)NodeClass.ObjectType | (uint)NodeClass.VariableType,
            ResultMask = (uint)BrowseResultMask.TargetInfo,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        var references = results.Results[0].References;
        references.Should().ContainSingle("node should have exactly one HasTypeDefinition reference");

        return ExpandedNodeId.ToNodeId(references[0].NodeId, Session.NamespaceUris);
    }

    private async Task<NodeId> BrowseChildByBrowseNameAsync(NodeId nodeId, QualifiedName browseName)
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = nodeId,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        var reference = results.Results[0].References.FirstOrDefault(r => r.BrowseName == browseName);

        return reference is null
            ? null
            : ExpandedNodeId.ToNodeId(reference.NodeId, Session.NamespaceUris);
    }
}

/// <summary>
/// Tests for events raised by the Pump plugin.
/// </summary>
[TestFixture]
public class PumpEventTests : SubscriptionTestsBase
{
    private const uint PumpEventTypeId = 1100;

    private NodeId _eventType;

    public PumpEventTests() : base(["--pumps"])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        var pumpsNamespaceIndex = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.Pumps);
        _eventType = new NodeId(PumpEventTypeId, pumpsNamespaceIndex);

        SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

        var filter = (EventFilter)MonitoredItem.Filter;
        filter.WhereClause.Push(FilterOperator.OfType, _eventType);

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void PumpEvents_AreRaisedForBothPumps()
    {
        ClearEvents();

        // Fire the 1000 ms pump timer once; each of the two pumps raises one event.
        var values = FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1000), expectedCount: 2).ToList();

        foreach (var value in values)
        {
            value.Should().Contain(new KeyValuePair<string, object>("/EventType", _eventType));
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().MatchRegex("^Pump\\d telemetry: flow=.+, pressure=.+$");

            // Each event's source node is the pump that raised it.
            var sourceName = value["/SourceName"].Should().BeOfType<string>().Subject;
            value.Should().Contain(new KeyValuePair<string, object>("/SourceNode", GetOpcPlcNodeId(sourceName)));
        }

        values.Select(v => v["/SourceName"]).Should().BeEquivalentTo(["Pump1", "Pump2"]);
    }
}
