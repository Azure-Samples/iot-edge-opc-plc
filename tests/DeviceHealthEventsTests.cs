namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.DI;
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
        "--b2bt=1",
        "--b2tt=123",
        "--b2mi=567",
        "--b2oi=678",
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

    [TestCase, Order(1)]
    public void EventSubscribed_FiresNotification_Maintenance()
    {
        // 1. MAINTENANCE_REQUIRED: Triggered by the maintenance interval

        ClearEvents();

        // Fast forward to trigger maintenance required.
        FireTimersWithPeriod(FromSeconds(567), numberOfTimes: 1);

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

    [TestCase, Order(2)]
    public void EventSubscribed_FiresNotification_Failure()
    {
        // 2. FAILURE: Temperature > overheated temperature

        ClearEvents();

        // Fast forward to trigger overheat, then cool down for 2 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 2);

        var events = ReceiveAtMostEvents(2);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        int i = 0;
        foreach (var value in values)
        {
            if (i == 1)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "Overheated",
                });
                value.Should().ContainKey("/Message")
                    .WhoseValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().Contain("FailureAlarm.");
            }
        }
    }

    [TestCase, Order(3)]
    public void EventSubscribed_FiresNotification_CheckFunction()
    {
        // 3. CHECK_FUNCTION: Target temperature < Temperature < overheated temperature

        ClearEvents();

        // Fast forward to trigger overheat, then cool down for 3 s.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 3);

        var events = ReceiveAtMostEvents(3);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        int i = 0;
        foreach (var value in values)
        {
            if (i == 2)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "Check function",
                });
                value.Should().ContainKey("/Message")
                    .WhoseValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().Contain("CheckFunctionAlarm.");
            }
            i++;
        }
    }


    [TestCase, Order(4)]
    public void EventSubscribed_FiresNotification_OffSpec1()
    {
        // 4. OFF_SPEC 1: Temperature < base temperature

        ClearEvents();

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var currentTemperatureDegrees = (float)Session.ReadValue(currentTemperatureNodeId).Value;

        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = WriteValue(baseTemperatureNodeId, currentTemperatureDegrees + 10f);

        // Fast forward 1 s to update the DeviceHealth.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var events = ReceiveAtMostEvents(1);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object>
            {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Off spec",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Contain("OffSpecAlarm.");
        }
    }

    [TestCase, Order(5)]
    public void EventSubscribed_FiresNotification_OffSpec2()
    {
        // 5. OFF_SPEC 2: Temperature > overheated temperature + 5

        ClearEvents();

        // Fast forward to trigger overheat.
        FireTimersWithPeriod(FromSeconds(678), numberOfTimes: 1);

        var events = ReceiveAtMostEvents(1);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);
        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object>
            {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Off spec",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Contain("OffSpecAlarm.");
        }
    }
}