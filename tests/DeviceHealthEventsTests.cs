namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections.Generic;
using System.Linq;
using static System.TimeSpan;

/// <summary>
/// Tests for Boiler2 DeviceHealth events.
/// </summary>
[TestFixture]
public class DeviceHealthEventsTests : SubscriptionTestsBase
{
    private NodeId _eventType;

    public DeviceHealthEventsTests() : base(new[] {
        "--b2ts=5",
        "--b2bt=50",
        "--b2tt=60",
        "--b2mi=5",
        "--b2oi=1",
    })
    {
    }

    [SetUp]
    public void CreateMonitoredItem()
    {
        _eventType = new NodeId(15143, 2);

        SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

        // add condition fields to retrieve selected event.
        var filter = (EventFilter)MonitoredItem.Filter;
        var whereClause = filter.WhereClause;
        whereClause.Push(FilterOperator.OfType, _eventType);

        AddMonitoredItem();
    }

    [Test]
    public void EventSubscribed_FiresNotificationOverheated()
    {
        // Arrange
        ClearEvents();

        // Act
        // Event is fired every second
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 4);

        var events = ReceiveAtMostEvents(1);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object>
            {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Overheat",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Contain("OffSpecAlarm.");
        }
    }

    [Test]
    public void EventSubscribed_FiresNotificationMaintenance()
    {
        // Arrange
        ClearEvents();

        // Act
        // Event is fired every 5 second
        FireTimersWithPeriod(FromSeconds(5), numberOfTimes: 2);

        var events = ReceiveAtMostEvents(1);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object>
            {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Maintenance",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Contain("MaintenanceRequiredAlarm.");
        }
    }
}