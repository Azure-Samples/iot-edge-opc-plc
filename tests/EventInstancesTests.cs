namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for OPC-UA Monitoring for Events.
/// </summary>
[TestFixture]
public class EventInstancesTests : SubscriptionTestsBase
{
    private NodeId _eventType;

    // Set any cmd params needed for the plc server explicitly.
    public EventInstancesTests() : base(["--ei=1", "--er=1000"])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        _eventType = ToNodeId(ObjectTypeIds.BaseEventType);

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

        // Act
        // Event is fired every second
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 5);

        // Assert
        var events = ReceiveAtMostEvents(5);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
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
                .Which.Text.Should().MatchRegex("^Event with index '0' and event cycle '\\d+'$");
        }
    }
}
