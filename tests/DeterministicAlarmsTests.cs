namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using OpcPlc.DeterministicAlarms.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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

[TestFixture]
public class DeterministicAlarmsSubscriptionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task NativeSubscriptions_RefreshUnsubscribeAndStopReplayAsync(bool serverWide)
    {
        var fixture = new PlcSimulatorFixture(["--dalm=DeterministicAlarmsTests/dalm001.json", "--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var session = await fixture.CreateSessionAsync("NativeDeterministicEvents").ConfigureAwait(false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var manager = fixture.Server.DeterministicAlarmsNodeManager;
            manager.Should().BeAssignableTo<Opc.Ua.Server.AsyncCustomNodeManager>();
            fixture.Server.CurrentInstance.NodeManager.AsyncNodeManagers.Should().Contain(manager);
            var folderId = new NodeId("VendingMachines", manager.NamespaceIndex);
            var source = manager.FindPredefinedNode<SimSourceNodeState>(new NodeId("VendingMachine1", manager.NamespaceIndex));
            var alarm = (AlarmConditionState)source.FindChildBySymbolicName(
                manager.SystemContext, "VendingMachine1_DoorOpen");
            var owner = await fixture.Server.CurrentInstance.NodeManager.GetManagerHandleAsync(
                alarm.NodeId, deadline.Token).ConfigureAwait(false);
            owner.nodeManager.Should().BeSameAs(manager);

            using var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 100 };
            session.AddSubscription(subscription);
            bool created = false;
            try
            {
                await subscription.CreateAsync(deadline.Token).ConfigureAwait(false);
                created = true;
                var firstEvents = Channel.CreateUnbounded<EventFieldList>();
                var secondEvents = Channel.CreateUnbounded<EventFieldList>();
                var first = CreateEventItem(firstEvents);
                var second = CreateEventItem(secondEvents);
                var enabled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var data = new MonitoredItem(subscription.DefaultItem)
                {
                    StartNodeId = alarm.EnabledState.Id.NodeId, AttributeId = Attributes.Value,
                    SamplingInterval = 0, QueueSize = 10
                };
                data.Notification += (item, notification) =>
                {
                    foreach (DataValue value in item.DequeueValues())
                    {
                        if (StatusCode.IsGood(value.StatusCode) && value.WrappedValue.GetBoolean())
                        {
                            enabled.TrySetResult();
                        }
                    }
                };
                subscription.AddItems([first, second, data]);
                await subscription.ApplyChangesAsync(deadline.Token).ConfigureAwait(false);
                source.AreEventsMonitored.Should().BeTrue();
                Action tick = fixture.GetTimerHandlersForPeriod(9000).Should().ContainSingle().Subject;
                tick();
                await WaitForEventAsync(firstEvents.Reader, "V1_DoorOpen-1 (1)", deadline.Token).ConfigureAwait(false);
                await WaitForEventAsync(secondEvents.Reader, "V1_DoorOpen-1 (1)", deadline.Token).ConfigureAwait(false);
                await enabled.Task.WaitAsync(deadline.Token).ConfigureAwait(false);

                using var canceled = new CancellationTokenSource();
                canceled.Cancel();
                Func<Task> canceledDelete = () => manager.DeleteAddressSpaceAsync(canceled.Token).AsTask();
                await canceledDelete.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

                subscription.RemoveItem(first);
                await subscription.ApplyChangesAsync(deadline.Token).ConfigureAwait(false);
                source.AreEventsMonitored.Should().BeTrue();
                while (secondEvents.Reader.TryRead(out _))
                {
                }
                await subscription.ConditionRefreshAsync(deadline.Token).ConfigureAwait(false);
                await WaitForEventAsync(secondEvents.Reader, "V1_DoorOpen-1 (1)", deadline.Token).ConfigureAwait(false);
                tick();
                tick();
                await WaitForEventAsync(secondEvents.Reader, "V2_LightOff-1 (1)", deadline.Token).ConfigureAwait(false);

                await subscription.DeleteAsync(true, deadline.Token).ConfigureAwait(false);
                created = false;
                source.AreEventsMonitored.Should().BeFalse();
                ByteString lastEventId = alarm.EventId.Value;
                await manager.DeleteAddressSpaceAsync(deadline.Token).ConfigureAwait(false);
                tick();
                tick();
                alarm.EventId.Value.Should().Be(lastEventId);
                manager.FindPredefinedNode<NodeState>(folderId).Should().BeNull();
                var retained = new List<IFilterTarget>();
                source.ConditionRefresh(manager.SystemContext, retained, true);
                retained.Should().BeEmpty();

                MonitoredItem CreateEventItem(Channel<EventFieldList> events)
                {
                    var item = new MonitoredItem(subscription.DefaultItem)
                    {
                        StartNodeId = serverWide ? ObjectIds.Server : folderId,
                        NodeClass = NodeClass.Object, AttributeId = Attributes.EventNotifier,
                        SamplingInterval = 0, QueueSize = 100,
                        Filter = new EventFilter
                        {
                            SelectClauses =
                            [
                                new SimpleAttributeOperand
                                {
                                    TypeDefinitionId = ObjectTypeIds.BaseEventType, AttributeId = Attributes.Value,
                                    BrowsePath = [new QualifiedName(BrowseNames.EventId)]
                                }
                            ]
                        }
                    };
                    item.Notification += (monitoredItem, notification) =>
                    {
                        if (notification.NotificationValue is EventFieldList fields)
                        {
                            events.Writer.TryWrite(fields);
                        }
                    };
                    return item;
                }
            }
            finally
            {
                if (created)
                {
                    await subscription.DeleteAsync(true, CancellationToken.None).ConfigureAwait(false);
                }
                await session.RemoveSubscriptionAsync(subscription, CancellationToken.None).ConfigureAwait(false);
            }
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }

    private static async Task WaitForEventAsync(
        ChannelReader<EventFieldList> events, string expectedId, CancellationToken cancellationToken)
    {
        while (true)
        {
            EventFieldList fields = await events.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (Encoding.UTF8.GetString(fields.EventFields[0].GetByteString().Span) == expectedId)
            {
                return;
            }
        }
    }
}
