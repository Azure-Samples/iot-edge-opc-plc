namespace OpcPlc.Tests;

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using static System.TimeSpan;

[TestFixture]
public class DeterministicAlarmsTests : SubscriptionTestsBase
{
    private const string Alarms = OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance;
    private static readonly LocalizedText Active = English("Active");
    private static readonly LocalizedText Inactive = English("Inactive");
    private static readonly LocalizedText Disabled = English("Disabled");
    private static readonly LocalizedText Enabled = English("Enabled");

    public DeterministicAlarmsTests() : base(new[]
        {
                "--dalm=DeterministicAlarmsTests/dalm001.json",
            })
    {
    }

    [SetUp]
    public void CreateMonitoredItem()
    {
        SetUpMonitoredItem(Objects.Server, NodeClass.Object, Attributes.EventNotifier);

        AddMonitoredItem();
    }

    [Test]
    public void FiresEventSequence()
    {
        var machine1 = AlarmNodeId("VendingMachine1");
        var machine2 = AlarmNodeId("VendingMachine2");

        var doorOpen1 = FindNode(machine1, Alarms, "VendingMachine1_DoorOpen");
        var tempHigh1 = FindNode(machine1, Alarms, "VendingMachine1_TemperatureHigh");
        var doorOpen2 = FindNode(machine2, Alarms, "VendingMachine2_DoorOpen");
        var lightOff2 = FindNode(machine2, Alarms, "VendingMachine2_LightOff");

        NodeShouldHaveStates(doorOpen1, Inactive, Disabled);
        NodeShouldHaveStates(tempHigh1, Inactive, Disabled);
        NodeShouldHaveStates(doorOpen2, Inactive, Disabled);
        NodeShouldHaveStates(lightOff2, Inactive, Disabled);

        var waitUntilStartInSeconds = FromSeconds(9); // value in dalm001.json file
        FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, 1)
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

        NodeShouldHaveStates(doorOpen1, Active, Enabled);
        NodeShouldHaveStates(tempHigh1, Inactive, Disabled);
        NodeShouldHaveStates(doorOpen2, Inactive, Disabled);
        NodeShouldHaveStates(lightOff2, Inactive, Disabled);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), 1)
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

        NodeShouldHaveStates(lightOff2, Active, Enabled);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(7), 1)
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

        NodeShouldHaveStates(doorOpen1, Inactive, Enabled);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(4), 1)
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

        NodeShouldHaveStates(tempHigh1, Active, Enabled);

        FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V1_DoorOpen-1 (2)",
                ["/Message"] = new LocalizedText("Door Open"),
            });

        NodeShouldHaveStates(doorOpen1, Active, Enabled);

        AdvanceToNextStep();

        FireTimersWithPeriodAndReceiveEvents(FromSeconds(5), 1)
            .First()
            .Should().Contain(new Dictionary<string, object>
            {
                ["/EventId"] = "V2_LightOff-1 (2)",
                ["/Message"] = new LocalizedText("Light Off in machine"),
            });

        NodeShouldHaveStates(lightOff2, Active, Enabled);

        AdvanceToNextStep();

        // At this point, the *runningForSeconds* limit in the JSON file causes execution to stop
        FireTimersWithPeriodAndReceiveEvents(FromSeconds(1), 0);

        NodeShouldHaveStates(doorOpen1, Active, Enabled);
        NodeShouldHaveStates(tempHigh1, Active, Enabled);
        NodeShouldHaveStates(doorOpen2, Inactive, Disabled);
        NodeShouldHaveStates(lightOff2, Active, Enabled);
    }

    private void AdvanceToNextStep()
    {
        FireTimersWithPeriodAndReceiveEvents(FromMilliseconds(1), 0);
    }

    private void NodeShouldHaveStates(NodeId node, LocalizedText activeState, LocalizedText enabledState)
    {
        NodeShouldHaveState(node, "ActiveState", activeState);
        NodeShouldHaveState(node, "EnabledState", enabledState);
    }

    private void NodeShouldHaveState(NodeId node, string state, LocalizedText expectedValue)
    {
        var nodeId = FindNode(node, Namespaces.OpcUa, state);
        var value = ReadValue<LocalizedText>(nodeId);
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
