namespace OpcPlc.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using static System.TimeSpan;

[TestFixture]
public class DeterministicAlarmsTests2 : SubscriptionTestsBase
{
    private const string Alarms = OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance;
    private static readonly LocalizedText Active = English("Active");
    private static readonly LocalizedText Inactive = English("Inactive");
    private static readonly LocalizedText Disabled = English("Disabled");
    private static readonly LocalizedText Enabled = English("Enabled");

    public DeterministicAlarmsTests2() : base(new[]
    {
            "--dalm=DeterministicAlarmsTests/dalm002.json",
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
    public void VerifyThatTimeForEventsChanges()
    {
        var machine1 = AlarmNodeId("VendingMachine1");

        var doorOpen1 = FindNode(machine1, Alarms, "VendingMachine1_DoorOpen");

        NodeShouldHaveStates(doorOpen1, Inactive, Disabled);

        var waitUntilStartInSeconds = FromSeconds(5); // value in dalm002.json file
        var opcEvent1 = FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, 1).First();
        var timeForFirstEvent = DateTime.Parse(opcEvent1["/Time"].ToString());

        NodeShouldHaveStates(doorOpen1, Active, Enabled);

        AdvanceToNextStep();

        var opcEvent2 = FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, 1).First();
        var timeForNextEvent = DateTime.Parse(opcEvent2["/Time"].ToString());

        NodeShouldHaveStates(doorOpen1, Inactive, Disabled);

        Assert.AreNotEqual(timeForFirstEvent, timeForNextEvent);
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
