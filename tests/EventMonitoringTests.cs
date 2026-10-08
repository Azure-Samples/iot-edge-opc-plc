namespace OpcPlc.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;

/// <summary>
/// Tests for OPC-UA Monitoring for Events.
/// </summary>
[TestFixture]
public class EventMonitoringTests : SubscriptionTestsBase
{
    private NodeId _eventType;

    public EventMonitoringTests() : base(["--simpleevents"])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        _eventType = ToNodeId(SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType);

        SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

        // add condition fields to retrieve selected event.
        var filter = (EventFilter)MonitoredItem.Filter;
        var whereClause = filter.WhereClause;
        whereClause.Push(FilterOperator.OfType, _eventType);

        ushort namespaceIndex = (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcSimpleEvents);
        filter.SelectClauses = filter.SelectClauses.ToArray().Concat(new[] { "CycleId", "CurrentStep", "Steps" }
            .Select(name => new SimpleAttributeOperand
            {
                TypeDefinitionId = _eventType,
                BrowsePath = new[] { new QualifiedName(name, namespaceIndex) }.ToArrayOf(),
                AttributeId = Attributes.Value
            })).ToArrayOf();
        Session.MessageContext.Factory.Builder.AddEncodeableType(typeof(SimpleEvents.CycleStepDataType)).Commit();

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void EventSubscribed_FiresNotification()
    {
        // Arrange
        ClearEvents();

        // Assert
        var notifications = ReceiveEvents(6).Cast<EventFieldList>().ToArray();
        var values = notifications.Select(EventFieldListToDictionary);
        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object>
            {
                ["/EventType"] = _eventType,
                ["/SourceNode"] = Server,
                ["/SourceName"] = "System",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().MatchRegex("^The system cycle '\\d+' has started\\.$");
        }
        AssertRuntimeStructures(notifications);
    }

    private static void AssertRuntimeStructures(IEnumerable<EventFieldList> notifications)
    {
        foreach (var notification in notifications)
        {
            var fields = notification.EventFields;
            fields[fields.Count - 3].GetString().Should().MatchRegex("^[0-9]+$");
            fields[fields.Count - 2].TryGetStructure(out SimpleEvents.CycleStepDataType current).Should().BeTrue();
            current.Name.Should().Be("Step 1");
            current.Duration.Should().Be(1000);
            var steps = fields[fields.Count - 1].GetStructureArray<SimpleEvents.CycleStepDataType>();
            steps.Count.Should().Be(2);
            foreach (var step in steps)
            {
                step.Name.Should().Be("Step 1");
                step.Duration.Should().Be(1000);
            }
        }
    }
}
