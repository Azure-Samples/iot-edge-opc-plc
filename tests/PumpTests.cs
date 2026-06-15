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
