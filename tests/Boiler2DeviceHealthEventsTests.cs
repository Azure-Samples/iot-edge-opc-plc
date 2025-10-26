namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for Boiler2 DeviceHealth events.
/// </summary>
[TestFixture]
public class Boiler2DeviceHealthEventsTests : SubscriptionTestsBase
{
    private NodeId _eventType;

    public Boiler2DeviceHealthEventsTests() : base([
        "--b2ts=5",    // Temperature change speed.
        "--b2bt=1",    // Base temperature.
        "--b2tt=123",  // Target temperature.
        "--b2mi=567",  // Maintenance interval.
        "--b2oi=678",  // Overheat interval.
    ])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        _eventType = new NodeId(15143, 2);

        SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

        // add condition fields to retrieve selected event.
        var filter = (EventFilter)MonitoredItem.Filter;
        var whereClause = filter.WhereClause;
        whereClause.Push(FilterOperator.OfType, _eventType);

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [TestCase, Order(1)]
    public void FiresEvent_Maintenance()
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
            value.Should().Contain(new Dictionary<string, object> {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Maintenance",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Be("Maintenance required!");
        }
    }

    [TestCase, Order(2)]
    public void FiresEvent_Failure()
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
                value.Should().Contain(new Dictionary<string, object> {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "Boiler #2",
                });
                value.Should().ContainKey("/Message")
                    .WhoseValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().Be("Temperature is above or equal to the overheat threshold!");
            }
            i++;
        }
    }

    [TestCase, Order(3)]
    public void FiresEvent_CheckFunction()
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
                value.Should().Contain(new Dictionary<string, object> {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "Boiler #2",
                });
                value.Should().ContainKey("/Message")
                    .WhoseValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().Be("Temperature is above target!");
            }
            i++;
        }
    }


    [TestCase, Order(4)]
    public async Task FiresEvent_OffSpec1()
    {
        // 4. OFF_SPEC 1: Temperature < base temperature

        ClearEvents();

        var currentTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var currentTemperatureDegrees = (float)(await Session.ReadValueAsync(currentTemperatureNodeId).ConfigureAwait(false)).Value;

        var baseTemperatureNodeId = NodeId.Create(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, OpcPlc.Namespaces.OpcPlcBoiler, Session.NamespaceUris);
        var statusCode = await WriteValueAsync(baseTemperatureNodeId, currentTemperatureDegrees + 10f).ConfigureAwait(false);
        statusCode.Should().Be(StatusCodes.Good);

        // Fast forward 1 s to update the DeviceHealth.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var events = ReceiveAtMostEvents(1);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);

        foreach (var value in values)
        {
            value.Should().Contain(new Dictionary<string, object> {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Boiler #2",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Be("Temperature is off spec!");
        }
    }

    [TestCase, Order(5)]
    public void FiresEvent_OffSpec2()
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
            value.Should().Contain(new Dictionary<string, object> {
                ["/EventType"] = _eventType,
                ["/SourceName"] = "Boiler #2",
            });
            value.Should().ContainKey("/Message")
                .WhoseValue.Should().BeOfType<LocalizedText>()
                .Which.Text.Should().Be("Temperature is off spec!");
        }
    }
}
