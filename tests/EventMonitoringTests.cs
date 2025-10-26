namespace OpcPlc.Tests;

using System.Collections.Generic;
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

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void EventSubscribed_FiresNotification()
    {
        // Arrange
        ClearEvents();

        // Assert
        var values = ReceiveEventsAsDictionary(6);
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
    }
}
