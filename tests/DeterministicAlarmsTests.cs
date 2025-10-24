namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

[TestFixture]
public class DeterministicAlarmsTests : SubscriptionTestsBase
{
    private const string Alarms = OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance;
    private static readonly LocalizedText Active = English("Active");
    private static readonly LocalizedText Inactive = English("Inactive");
    private static readonly LocalizedText Disabled = English("Disabled");
    private static readonly LocalizedText Enabled = English("Enabled");

    public DeterministicAlarmsTests() : base(
        [
            "--dalm=DeterministicAlarmsTests/dalm001.json",
        ])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        SetUpMonitoredItem(AlarmNodeId("VendingMachines"), NodeClass.Object, Attributes.EventNotifier);

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task FiresEventSequence()
    {
        var machine1 = AlarmNodeId("VendingMachine1");
        var machine2 = AlarmNodeId("VendingMachine2");

        var doorOpen1 = await FindNodeAsync(machine1, Alarms, "VendingMachine1_DoorOpen").ConfigureAwait(false);
        var tempHigh1 = await FindNodeAsync(machine1, Alarms, "VendingMachine1_TemperatureHigh").ConfigureAwait(false);
        var doorOpen2 = await FindNodeAsync(machine2, Alarms, "VendingMachine2_DoorOpen").ConfigureAwait(false);
        var lightOff2 = await FindNodeAsync(machine2, Alarms, "VendingMachine2_LightOff").ConfigureAwait(false);

        await NodeShouldHaveStatesAsync(doorOpen1, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(tempHigh1, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(doorOpen2, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(lightOff2, Inactive, Disabled).ConfigureAwait(false);

        var waitUntilStartInSeconds = FromSeconds(9); // value in dalm001.json file
        FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V1_DoorOpen-1 (1)",
                ["/EventType"] = ToNodeId(ObjectTypes.TripAlarmType),
                ["/SourceNode"] = machine1,
                ["/SourceName"] = "VendingMachine1",
                ["/Message"] = new LocalizedText("Door Open"),
                ["/Severity"] = EventSeverity.High,
            });

        await NodeShouldHaveStatesAsync(doorOpen1, Active, Enabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(tempHigh1, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(doorOpen2, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(lightOff2, Inactive, Disabled).ConfigureAwait(false);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V2_LightOff-1 (1)",
                ["/EventType"] = ToNodeId(ObjectTypes.OffNormalAlarmType),
                ["/SourceNode"] = machine2,
                ["/SourceName"] = "VendingMachine2",
                ["/Message"] = new LocalizedText("Light Off in machine"),
                ["/Severity"] = EventSeverity.Medium,
            });

        await NodeShouldHaveStatesAsync(lightOff2, Active, Enabled).ConfigureAwait(false);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(7), expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V1_DoorOpen-2 (1)",
                ["/EventType"] = ToNodeId(ObjectTypes.TripAlarmType),
                ["/SourceNode"] = machine1,
                ["/SourceName"] = "VendingMachine1",
                ["/Message"] = new LocalizedText("Door Closed"),
                ["/Severity"] = EventSeverity.Medium,
            });

        await NodeShouldHaveStatesAsync(doorOpen1, Inactive, Enabled).ConfigureAwait(false);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(4), expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V1_TemperatureHigh-1 (1)",
                ["/EventType"] = ToNodeId(ObjectTypes.LimitAlarmType),
                ["/SourceNode"] = machine1,
                ["/SourceName"] = "VendingMachine1",
                ["/Message"] = new LocalizedText("Temperature is HIGH"),
                ["/Severity"] = EventSeverity.High,
            });

        await NodeShouldHaveStatesAsync(tempHigh1, Active, Enabled).ConfigureAwait(false);

        FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V1_DoorOpen-1 (2)",
                ["/Message"] = new LocalizedText("Door Open"),
            });

        await NodeShouldHaveStatesAsync(doorOpen1, Active, Enabled).ConfigureAwait(false);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), expectedCount: 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V2_LightOff-1 (2)",
                ["/Message"] = new LocalizedText("Light Off in machine"),
            });

        await NodeShouldHaveStatesAsync(lightOff2, Active, Enabled).ConfigureAwait(false);

        AdvanceToNextStep();

        // At this point, the *runningForSeconds* limit in the JSON file causes execution to stop
        FireTimersWithPeriodAndReceiveEvents(FromSeconds(1), expectedCount: 0);

        await NodeShouldHaveStatesAsync(doorOpen1, Active, Enabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(tempHigh1, Active, Enabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(doorOpen2, Inactive, Disabled).ConfigureAwait(false);
        await NodeShouldHaveStatesAsync(lightOff2, Active, Enabled).ConfigureAwait(false);
    }

    private void AdvanceToNextStep()
    {
        FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);
    }

    private async Task NodeShouldHaveStatesAsync(NodeId node, LocalizedText activeState, LocalizedText enabledState)
    {
        await NodeShouldHaveStateAsync(node, "ActiveState", activeState).ConfigureAwait(false);
        await NodeShouldHaveStateAsync(node, "EnabledState", enabledState).ConfigureAwait(false);
    }

    private async Task NodeShouldHaveStateAsync(NodeId node, string state, LocalizedText expectedValue)
    {
        var nodeId = await FindNodeAsync(node, Namespaces.OpcUa, state).ConfigureAwait(false);
        var value = await ReadValueAsync<LocalizedText>(nodeId).ConfigureAwait(false);
        value.Should().Be(expectedValue, "{0} should be {1}", state, expectedValue);
    }

    private NodeId AlarmNodeId(string identifier)
    {
        return NodeId.Create(identifier, Alarms, Session.NamespaceUris);
    }

    private static LocalizedText English(string text)
    {
        return new LocalizedText("en-US", text);
    }
}
