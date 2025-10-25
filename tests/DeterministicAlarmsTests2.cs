namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

[TestFixture]
public class DeterministicAlarmsTests2 : SubscriptionTestsBase
{
    private const string Alarms = OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance;
    private static readonly LocalizedText Active = English("Active");
    private static readonly LocalizedText Inactive = English("Inactive");
    private static readonly LocalizedText Disabled = English("Disabled");
    private static readonly LocalizedText Enabled = English("Enabled");

    public DeterministicAlarmsTests2() : base(
        [
            "--dalm=DeterministicAlarmsTests/dalm002.json",
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
    public async Task VerifyThatTimeForEventsChanges()
    {
        var machine1 = AlarmNodeId("VendingMachine1");

        var doorOpen1 = await FindNodeAsync(machine1, Alarms, "VendingMachine1_DoorOpen").ConfigureAwait(false);

        await NodeShouldHaveStatesAsync(doorOpen1, Inactive, Disabled).ConfigureAwait(false);

        var waitUntilStartInSeconds = FromSeconds(5); // value in dalm002.json file
        var opcEvent1 = FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, expectedCount: 1).First();
        var timeForFirstEvent = DateTime.Parse(opcEvent1["/Time"].ToString());

        await NodeShouldHaveStatesAsync(doorOpen1, Active, Enabled).ConfigureAwait(false);

        AdvanceToNextStep();

        var opcEvent2 = FireTimersWithPeriodAndReceiveEvents(waitUntilStartInSeconds, expectedCount: 1).First();
        var timeForNextEvent = DateTime.Parse(opcEvent2["/Time"].ToString());

        await NodeShouldHaveStatesAsync(doorOpen1, Inactive, Disabled).ConfigureAwait(false);

        timeForFirstEvent.Should().NotBe(timeForNextEvent);
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
