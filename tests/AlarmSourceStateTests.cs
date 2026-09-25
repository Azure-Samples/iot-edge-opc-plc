namespace OpcPlc.Tests;

using FluentAssertions;
using global::AlarmCondition;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class AlarmSourceStateTests
{
    [Test]
    public void RoundRobinBytes_FillOnlyRequestedSliceWithoutAdvancingIntegerSequence()
    {
        var random = new OpcPlc.AlarmCondition.RoundRobinSource(42);
        byte[] bytes = Enumerable.Repeat((byte)0xAA, 24).ToArray();

        random.NextBytes(bytes, 4, 16);

        bytes[..4].Should().Equal(Enumerable.Repeat((byte)0xAA, 4));
        bytes[20..].Should().Equal(Enumerable.Repeat((byte)0xAA, 4));
        bytes[4..20].Should().NotEqual(Enumerable.Repeat((byte)0xAA, 16));
        random.NextInt32(100).Should().Be(42);
        random.NextInt32(100).Should().Be(43);
    }

    [Test]
    public void RoundRobinBytes_ReproduceSeededSequenceAndChangeBetweenCalls()
    {
        var first = new OpcPlc.AlarmCondition.RoundRobinSource(42);
        var second = new OpcPlc.AlarmCondition.RoundRobinSource(42);
        byte[] firstBytes = new byte[16];
        byte[] secondBytes = new byte[16];
        first.NextBytes(firstBytes, 0, firstBytes.Length);
        second.NextBytes(secondBytes, 0, secondBytes.Length);
        firstBytes.Should().Equal(secondBytes).And.NotEqual(new byte[16]);
        byte[] previous = (byte[])firstBytes.Clone();

        first.NextBytes(firstBytes, 0, firstBytes.Length);
        second.NextBytes(secondBytes, 0, secondBytes.Length);

        firstBytes.Should().Equal(secondBytes).And.NotEqual(previous);
    }

    [Test]
    public void RoundRobinBytes_RejectInvalidBuffersAndRanges()
    {
        var random = new OpcPlc.AlarmCondition.RoundRobinSource(42);
        byte[] bytes = new byte[16];
        Action nullBuffer = () => random.NextBytes(null, 0, 0);
        Action negativeOffset = () => random.NextBytes(bytes, -1, 1);
        Action tooManyBytes = () => random.NextBytes(bytes, 8, 9);

        nullBuffer.Should().Throw<ArgumentNullException>();
        negativeOffset.Should().Throw<ArgumentOutOfRangeException>();
        tooManyBytes.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase("Red", typeof(ExclusiveDeviationAlarmState))]
    [TestCase("Yellow", typeof(NonExclusiveLevelAlarmState))]
    [TestCase("Green", typeof(TripAlarmState))]
    public void Refresh_CreatesTypedAlarmsWithStableIdentifiers(string name, Type expectedType)
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm(name);

        alarm.Should().BeOfType(expectedType);
        alarm.NodeId.Should().Be(new NodeId("Source?" + name, 2));
        alarm.ReferenceTypeId.Should().Be(ReferenceTypeIds.HasComponent);
        alarm.SourceNode.Value.Should().Be(fixture.Source.NodeId);
        alarm.SourceName.Value.Should().Be("Motor");
        alarm.BranchId.Value.IsNull.Should().BeTrue();
        alarm.ActiveState.TransitionTime.Should().BeAssignableTo<PropertyState<DateTimeUtc>>();
        alarm.EnabledState.TransitionTime.Should().BeAssignableTo<PropertyState<DateTimeUtc>>();
        alarm.EventId.Value.Span.Length.Should().Be(16);
        alarm.Retain.Value.Should().BeTrue();

        NodeId id = alarm.NodeId;
        fixture.Backend.Refresh();
        fixture.Alarm(name).NodeId.Should().Be(id);
        fixture.Alarms.Should().HaveCount(3);
    }

    [Test]
    public void Refresh_PreservesLimitValuesAndStates()
    {
        using var fixture = new SourceFixture();
        var high = (ExclusiveLimitAlarmState)fixture.Alarm("Red");
        var highLow = (NonExclusiveLimitAlarmState)fixture.Alarm("Yellow");

        high.HighLimit.Value.Should().Be(80.0);
        highLow.HighHighLimit.Value.Should().Be(90.0);
        highLow.HighLimit.Value.Should().Be(70.0);
        highLow.LowLimit.Value.Should().Be(30.0);
        highLow.LowLowLimit.Value.Should().Be(10.0);
        highLow.HighState.Id.Value.Should().BeTrue();
        highLow.HighHighState.Id.Value.Should().BeFalse();
        highLow.LowState.Id.Value.Should().BeFalse();
        highLow.LowLowState.Id.Value.Should().BeFalse();
    }

    [TestCase("comment")]
    [TestCase("acknowledge")]
    [TestCase("confirm")]
    public void ConditionCallbacks_RejectNullUnknownAndStaleEventIds(string operation)
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        ByteString staleEventId = alarm.EventId.Value;
        fixture.Backend.Refresh();
        ByteString currentEventId = alarm.EventId.Value;
        currentEventId.Should().NotBe(staleEventId);

        ByteString[] rejectedIds = [default, ByteString.Empty, (ByteString)new byte[] { 1, 2 }, staleEventId];
        foreach (ByteString eventId in rejectedIds)
        {
            ServiceResult result = InvokeOperation(operation, fixture.Context, alarm, eventId);
            result.StatusCode.Should().Be((StatusCode)StatusCodes.BadEventIdUnknown);
            alarm.EventId.Value.Should().Be(currentEventId);
        }
    }

    [Test]
    public void AcknowledgeAndConfirm_UpdateBackendAndClearActiveState()
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        ByteString originalEventId = alarm.EventId.Value;

        ServiceResult acknowledged = InvokeOperation("acknowledge", fixture.Context, alarm, originalEventId);

        ServiceResult.IsGood(acknowledged).Should().BeTrue();
        alarm.AckedState.Id.Value.Should().BeTrue();
        alarm.ConfirmedState.Id.Value.Should().BeFalse();
        alarm.Comment.Value.Text.Should().Be("test comment");
        alarm.EventId.Value.Should().NotBe(originalEventId);
        ServiceResult confirmed = InvokeOperation("confirm", fixture.Context, alarm, alarm.EventId.Value);

        ServiceResult.IsGood(confirmed).Should().BeTrue();
        alarm.ConfirmedState.Id.Value.Should().BeTrue();
        alarm.ActiveState.Id.Value.Should().BeFalse();
        alarm.Retain.Value.Should().BeFalse();
        fixture.Backend.Refresh();
        alarm.ActiveState.Id.Value.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Comment_UsesTokenUsernameOrDisplayNameFallback(bool usernameToken)
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        var identity = new Mock<IUserIdentity>();
        IUserIdentityTokenHandler handler = usernameToken
            ? new UserNameIdentityTokenHandler("operator", new byte[] { 1 })
            : new AnonymousIdentityTokenHandler(new AnonymousIdentityToken());
        identity.SetupGet(value => value.TokenHandler).Returns(handler);
        identity.SetupGet(value => value.DisplayName).Returns("display-name");
        var sessionContext = new Mock<ISessionSystemContext>();
        sessionContext.SetupGet(value => value.UserIdentity).Returns(identity.Object);

        ServiceResult result = InvokeOperation("comment", sessionContext.Object, alarm, alarm.EventId.Value);

        ServiceResult.IsGood(result).Should().BeTrue();
        alarm.Comment.Value.Text.Should().Be("test comment");
        alarm.ClientUserId.Value.Should().Be(usernameToken ? "operator" : "display-name");
    }

    [Test]
    public void Comment_EmptyLocalizedTextPreservesExistingComment()
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        InvokeOperation("comment", fixture.Context, alarm, alarm.EventId.Value);

        ServiceResult result = alarm.OnAddComment(fixture.Context, alarm, alarm.EventId.Value, default);

        ServiceResult.IsGood(result).Should().BeTrue();
        alarm.Comment.Value.Text.Should().Be("test comment");
    }

    [Test]
    public void Dialog_PreservesChoicesAndOfflineTransitions()
    {
        using var fixture = new SourceFixture();
        DialogConditionState dialog = fixture.Dialog;
        dialog.ResponseOptionSet.Value.ToArray().Select(value => value.Text)
            .Should().Equal("Online", "Offline", "No Change");
        dialog.DefaultResponse.Value.Should().Be(2);
        dialog.CancelResponse.Value.Should().Be(2);
        dialog.OkResponse.Value.Should().Be(0);
        dialog.ReferenceTypeId.Should().Be(ReferenceTypeIds.HasComponent);
        dialog.DialogState.Id.Value.Should().BeTrue();

        ServiceResult.IsGood(dialog.OnRespond(fixture.Context, dialog, 1)).Should().BeTrue();
        fixture.Backend.IsOffline.Should().BeTrue();
        fixture.Alarms.Should().OnlyContain(alarm => alarm.SuppressedState.Id.Value);
        dialog.Retain.Value.Should().BeFalse();
        dialog.DialogState.Id.Value.Should().BeFalse();

        dialog.Activate(fixture.Context);
        dialog.OnRespond(fixture.Context, dialog, 2);
        fixture.Backend.IsOffline.Should().BeTrue();
        dialog.Activate(fixture.Context);
        dialog.OnRespond(fixture.Context, dialog, 0);
        fixture.Backend.IsOffline.Should().BeFalse();
        fixture.Alarms.Should().OnlyContain(alarm => !alarm.SuppressedState.Id.Value);
    }

    [Test]
    public void EnableDisable_ControlsRetentionAndRefreshDeduplicatesSource()
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        ServiceResult.IsGood(alarm.OnEnableDisable(fixture.Context, alarm, false)).Should().BeTrue();
        alarm.EnabledState.Id.Value.Should().BeFalse();
        alarm.Retain.Value.Should().BeFalse();

        var events = new List<IFilterTarget>();
        fixture.Source.ConditionRefresh(fixture.Context, events, true);
        int expectedCount = fixture.Alarms.Count(node => node.Retain.Value) + (fixture.Dialog.Retain.Value ? 1 : 0);
        events.Should().HaveCount(expectedCount).And.NotBeEmpty();
        fixture.Source.ConditionRefresh(fixture.Context, events, true);
        events.Should().HaveCount(expectedCount);

        alarm.OnEnableDisable(fixture.Context, alarm, true);
        alarm.EnabledState.Id.Value.Should().BeTrue();
        alarm.Retain.Value.Should().BeTrue();
    }

    [Test]
    public void ShelvingAndUnshelving_ReplaceEventIdsAndPreserveMessages()
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        ByteString originalEventId = alarm.EventId.Value;
        try
        {
            ServiceResult.IsGood(alarm.OnShelve(fixture.Context, alarm, true, false, 60_000)).Should().BeTrue();
            alarm.Message.Value.Text.Should().Be("The alarm shelved.");
            alarm.EventId.Value.Should().NotBe(originalEventId);
        }
        finally
        {
            alarm.OnTimedUnshelve(fixture.Context, alarm);
        }

        alarm.Message.Value.Text.Should().Be("The timed shelving period expired.");
        alarm.ShelvingState.CurrentState.Id.Value.Should().Be(ObjectIds.ShelvedStateMachineType_Unshelved);
    }

    [Test]
    public void Source_ReportsChangesThroughInjectedEventSink()
    {
        using var fixture = new SourceFixture();
        fixture.Events.Clear();
        fixture.Source.SetAreEventsMonitored(fixture.Context, true, true);

        fixture.Backend.SetOfflineState(true);

        fixture.Events.Should().NotBeEmpty();
    }

    [TestCase("refresh")]
    [TestCase("comment")]
    [TestCase("acknowledge")]
    [TestCase("confirm")]
    [TestCase("enable")]
    [TestCase("shelve")]
    [TestCase("unshelve")]
    [TestCase("respond")]
    public async Task SourceCallbacks_UseSharedDomainLockAsync(string operation)
    {
        using var fixture = new SourceFixture();
        AlarmConditionState alarm = fixture.Alarm("Green");
        Action invoke = operation switch
        {
            "refresh" => () => fixture.Source.ConditionRefresh(fixture.Context, [], true),
            "enable" => () => alarm.OnEnableDisable(fixture.Context, alarm, false),
            "shelve" => () => alarm.OnShelve(fixture.Context, alarm, true, false, 60_000),
            "unshelve" => () => alarm.OnTimedUnshelve(fixture.Context, alarm),
            "respond" => () => fixture.Dialog.OnRespond(fixture.Context, fixture.Dialog, 2),
            _ => () => InvokeOperation(operation, fixture.Context, alarm, alarm.EventId.Value)
        };
        using var entered = new ManualResetEventSlim();
        Task callback = null;
        try
        {
            lock (fixture.SyncRoot)
            {
                callback = Task.Run(() =>
                {
                    entered.Set();
                    invoke();
                });
                entered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
                callback.Wait(TimeSpan.FromMilliseconds(100)).Should().BeFalse(
                    "the callback must wait for the domain lock");
            }
        }
        finally
        {
            if (callback is not null)
            {
                await callback.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            if (operation == "shelve")
            {
                alarm.OnTimedUnshelve(fixture.Context, alarm);
            }
        }
    }

    [Test]
    public async Task UnderlyingSystem_StopAsync_DrainsAlarmCallbackAsync()
    {
        using var system = new UnderlyingSystem(NullLogger.Instance);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        system.CreateSource("Colours/Motor", _ =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Alarm callback was not released.");
            }
        });
        system.StartSimulation();
        Task stopping = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            stopping = system.StopSimulationAsync().AsTask();
            stopping.IsCompleted.Should().BeFalse("the active backend callback must drain");
        }
        finally
        {
            release.Set();
            await (stopping ?? system.StopSimulationAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        await system.StopSimulationAsync().ConfigureAwait(false);
    }

    private static ServiceResult InvokeOperation(
        string operation, ISystemContext context, AlarmConditionState alarm, ByteString eventId)
    {
        var comment = new LocalizedText("test comment");
        return operation switch
        {
            "comment" => alarm.OnAddComment(context, alarm, eventId, comment),
            "acknowledge" => alarm.OnAcknowledge(context, alarm, eventId, comment),
            "confirm" => alarm.OnConfirm(context, alarm, eventId, comment),
            _ => throw new ArgumentException("Unknown test operation", nameof(operation))
        };
    }

    private sealed class SourceFixture : IDisposable
    {
        private readonly UnderlyingSystem _system = new(NullLogger.Instance);
        public SystemContext Context { get; }
        public SourceState Source { get; }
        public UnderlyingSystemSource Backend { get; }
        public List<IFilterTarget> Events { get; } = [];
        public object SyncRoot { get; } = new();

        public SourceFixture()
        {
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:application");
            namespaces.GetIndexOrAppend("urn:opcplc:test:alarms");
            var nodeIdFactory = new Mock<INodeIdFactory>();
            nodeIdFactory.Setup(factory => factory.New(It.IsAny<ISystemContext>(), It.IsAny<NodeState>()))
                .Returns((ISystemContext _, NodeState node) => ModelUtils.ConstructIdForComponent(node, 2));
            Context = new SystemContext(null)
            {
                NamespaceUris = namespaces,
                TypeTable = new TypeTable(namespaces),
                NodeIdFactory = nodeIdFactory.Object,
                SystemHandle = _system
            };
            var generator = new DataGenerator(new OpcPlc.AlarmCondition.RoundRobinSource(1234), null);
            Source = new SourceState(Context, SyncRoot, (_, value) => Events.Add(value), Context.TypeTable,
                new NodeId("Source", 2), "Colours/Motor", generator);
            _system.TryGetSource("Motor", out UnderlyingSystemSource backend).Should().BeTrue();
            Backend = backend;
            Backend.Refresh();
        }

        public IReadOnlyList<AlarmConditionState> Alarms => Children.OfType<AlarmConditionState>().ToArray();
        public DialogConditionState Dialog => Children.OfType<DialogConditionState>().Single();
        public AlarmConditionState Alarm(string name) => Alarms.Single(node => node.SymbolicName == name);

        private List<BaseInstanceState> Children
        {
            get
            {
                var children = new List<BaseInstanceState>();
                Source.GetChildren(Context, children);
                return children;
            }
        }

        public void Dispose()
        {
            _system.Dispose();
        }
    }
}