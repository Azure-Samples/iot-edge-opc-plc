// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using global::AlarmCondition;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.CompanionSpecs.DI;
using OpcPlc.CompanionSpecs.IA;
using OpcPlc.CompanionSpecs.Machinery;
using OpcPlc.CompanionSpecs.Pumps;
using OpcPlc.Configuration;
using OpcPlc.PluginNodes;
using OpcPlc.PluginNodes.Models;
using SimpleEvents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

[TestFixture]
public class PlcNodeManagerTests
{
    [TestCase(ValueRanks.Scalar)]
    [TestCase(ValueRanks.Any)]
    [TestCase(ValueRanks.ScalarOrOneDimension)]
    public async Task UserDefinedValues_RegisterWithConfiguredTypesAsync(int rank)
    {
        string path = Path.GetTempFileName();
        try
        {
            string arrayNode = rank == ValueRanks.Scalar ? string.Empty :
                $$""",{"NodeId":"array","DataType":"UInt32","ValueRank":{{rank}},"Value":[1,2]}""";
            await File.WriteAllTextAsync(path, $$"""
                {"Folder":"Custom","NodeList":[
                    {"NodeId":"integer","DataType":"UInt64","ValueRank":{{rank}},"Value":18446744073709551615.0},
                    {"NodeId":"date","DataType":"DateTime","ValueRank":{{rank}},"Value":"2026-10-07T12:34:56Z"}
                    {{arrayNode}}
                ]}
                """).ConfigureAwait(false);
            var plugin = new UserDefinedPluginNodes(new TimeService(), NullLogger.Instance);
            var options = new Mono.Options.OptionSet();
            plugin.AddOptions(options);
            options.Parse([$"--nodesfile={path}"]);
            using var fixture = new ManagerFixture([plugin]);
            await fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
            ushort ns = fixture.Manager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];
            var integer = fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("integer", ns));
            integer.DataType.Should().Be(Opc.Ua.DataTypeIds.UInt64);
            integer.Value.TypeInfo.BuiltInType.Should().Be(BuiltInType.UInt64);
            integer.Value.GetUInt64().Should().Be(ulong.MaxValue);
            integer.ValueRank.Should().Be(rank);
            var date = fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("date", ns));
            date.Value.TypeInfo.BuiltInType.Should().Be(BuiltInType.DateTime);
            date.ValueRank.Should().Be(rank);
            if (rank != ValueRanks.Scalar)
            {
                var array = fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("array", ns));
                array.Value.TypeInfo.Should().Be(TypeInfo.Construct(typeof(uint[])));
                array.Value.AsBoxedObject(Variant.BoxingBehavior.Legacy).Should().BeEquivalentTo(new uint[] { 1, 2 });
                array.ValueRank.Should().Be(rank);
            }
            plugin.Nodes.Should().HaveCount(rank == ValueRanks.Scalar ? 2 : 3);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("Float", "1e40")]
    [TestCase("Double", "1e400")]
    [TestCase("Int64", "9007199254740993.00000001")]
    public async Task UserDefinedValues_LogRejectedNumbersWithoutRegisteringThemAsync(string dataType, string value)
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, $$"""
                {"Folder":"Custom","NodeList":[{"NodeId":"invalid","DataType":"{{dataType}}","Value":{{value}}}]}
                """).ConfigureAwait(false);
            var logger = new Mock<ILogger>();
            logger.Setup(instance => instance.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            var plugin = new UserDefinedPluginNodes(new TimeService(), logger.Object);
            var options = new Mono.Options.OptionSet();
            plugin.AddOptions(options);
            options.Parse([$"--nodesfile={path}"]);
            using var fixture = new ManagerFixture([plugin]);
            await fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
            ushort ns = fixture.Manager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];
            fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("invalid", ns)).Should().BeNull();
            plugin.Nodes.Should().BeEmpty();
            logger.Verify(instance => instance.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(exception => exception is OverflowException),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task FastByteString_PublishesImmutableSnapshotsAndNotifiesDefaultFilterAsync()
    {
        ElapsedEventHandler tick = null;
        var time = new Mock<TimeService>();
        time.Setup(service => service.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()))
            .Returns((ElapsedEventHandler callback, uint _) =>
            {
                tick = callback;
                return Mock.Of<OpcPlc.ITimer>();
            });
        var plugin = new VeryFastByteStringPluginNodes(time.Object, NullLogger.Instance);
        var options = new Mono.Options.OptionSet();
        plugin.AddOptions(options);
        options.Parse(["--vfbs=2", "--vfbss=4"]);
        using var fixture = new ManagerFixture([plugin]);
        await fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        ushort ns = fixture.Manager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];
        var first = fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("VeryFastByteString1", ns));
        var second = fixture.Manager.FindPredefinedNode<BaseDataVariableState>(new NodeId("VeryFastByteString2", ns));
        var previous = new DataValue(first.Value.Copy());
        byte initial = previous.WrappedValue.GetByteString().ToArray()[0];
        plugin.StartSimulation();
        try
        {
            tick(null, null);
            var current = new DataValue(first.Value);
            previous.WrappedValue.GetByteString().ToArray()[0].Should().Be(initial);
            current.WrappedValue.GetByteString().ToArray()[0].Should().Be(unchecked((byte)(initial + 1)));
            current.WrappedValue.GetByteString().Length.Should().Be(4);
            second.Value.Should().Be(first.Value);
            MonitoredItem.ValueChanged(current, null, previous, null, null, 0).Should().BeTrue();
            for (int i = 1; i < 256; i++)
            {
                tick(null, null);
            }
            first.Value.GetByteString().ToArray()[0].Should().Be(initial, "the counter wraps after 256 ticks");
            current.WrappedValue.GetByteString().ToArray()[0].Should().Be(unchecked((byte)(initial + 1)));
        }
        finally
        {
            plugin.StopSimulation();
        }
    }

    [Test]
    public async Task AddressSpace_AwaitsPluginsInRegistrationOrderAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new Mock<IPluginNodes>();
        var second = new Mock<IPluginNodes>();
        FolderState child = null;
        first.Setup(plugin => plugin.AddToAddressSpaceAsync(
                It.IsAny<FolderState>(), It.IsAny<FolderState>(), It.IsAny<PlcNodeManager>(),
                It.IsAny<CancellationToken>()))
            .Returns((FolderState telemetry, FolderState methods, PlcNodeManager manager, CancellationToken token) =>
                new ValueTask(RegisterAsync(telemetry, manager, token)));
        second.Setup(plugin => plugin.AddToAddressSpaceAsync(
                It.IsAny<FolderState>(), It.IsAny<FolderState>(), It.IsAny<PlcNodeManager>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                child.Should().NotBeNull();
                return ValueTask.CompletedTask;
            });
        using var fixture = new ManagerFixture([first.Object, second.Object]);
        Task creation = fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            creation.IsCompleted.Should().BeFalse();
            second.Invocations.Should().BeEmpty();
        }
        finally
        {
            release.TrySetResult();
            await creation.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        fixture.Manager.FindPredefinedNode<FolderState>(child.NodeId).Should().BeSameAs(child);
        second.Verify(plugin => plugin.AddToAddressSpaceAsync(
            It.IsAny<FolderState>(), It.IsAny<FolderState>(), fixture.Manager,
            CancellationToken.None), Times.Once);

        async Task RegisterAsync(FolderState telemetry, PlcNodeManager manager, CancellationToken token)
        {
            entered.SetResult();
            await release.Task.WaitAsync(token).ConfigureAwait(false);
            child = manager.CreateFolder(telemetry, "Awaited", "Awaited", NamespaceType.OpcPlcApplications);
        }
    }

    [Test]
    public async Task AddressSpace_PreCanceled_DoesNotInvokePluginsOrAddReferencesAsync()
    {
        var plugin = new Mock<IPluginNodes>(MockBehavior.Strict);
        using var fixture = new ManagerFixture([plugin.Object]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> create = () => fixture.Manager.CreateAddressSpaceAsync(
            fixture.ExternalReferences, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        fixture.ExternalReferences.Should().BeEmpty();
        plugin.Invocations.Should().BeEmpty();
    }

    [Test]
    public async Task AddressSpace_CancellationDuringPlugin_StopsRegistrationAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new Mock<IPluginNodes>();
        var second = new Mock<IPluginNodes>(MockBehavior.Strict);
        first.Setup(plugin => plugin.AddToAddressSpaceAsync(
                It.IsAny<FolderState>(), It.IsAny<FolderState>(), It.IsAny<PlcNodeManager>(),
                It.IsAny<CancellationToken>()))
            .Returns((FolderState telemetry, FolderState methods, PlcNodeManager manager, CancellationToken token) =>
            {
                entered.SetResult();
                return new ValueTask(pending.Task.WaitAsync(token));
            });
        using var fixture = new ManagerFixture([first.Object, second.Object]);
        using var cancellation = new CancellationTokenSource();
        Task creation = fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences, cancellation.Token).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        finally
        {
            cancellation.Cancel();
        }

        Func<Task> create = () => creation.WaitAsync(TimeSpan.FromSeconds(5));
        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        second.Invocations.Should().BeEmpty();
    }

    [Test]
    public async Task AddressSpace_PluginFailure_PropagatesAndStopsRegistrationAsync()
    {
        var expected = new InvalidOperationException("Registration failed");
        var first = new Mock<IPluginNodes>();
        var second = new Mock<IPluginNodes>(MockBehavior.Strict);
        first.Setup(plugin => plugin.AddToAddressSpaceAsync(
                It.IsAny<FolderState>(), It.IsAny<FolderState>(), It.IsAny<PlcNodeManager>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(expected));
        using var fixture = new ManagerFixture([first.Object, second.Object]);
        Func<Task> create = () => fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).AsTask();

        var failure = await create.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);

        failure.Which.Should().BeSameAs(expected);
        second.Invocations.Should().BeEmpty();
    }

    [Test]
    public async Task ModelLoading_PreCanceled_DoesNotInvokeLoaderAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool invoked = false;
        Func<Task> load = () => fixture.Manager.LoadPredefinedNodesAsync(_ =>
        {
            invoked = true;
            return [];
        }, cancellation.Token).AsTask();

        await load.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        invoked.Should().BeFalse();
    }

    [TestCase(typeof(DiNodeManager))]
    [TestCase(typeof(IaNodeManager))]
    [TestCase(typeof(MachineryNodeManager))]
    [TestCase(typeof(PumpNodeManager))]
    public async Task CompanionModel_PreCanceled_DoesNotRegisterNodesAsync(Type managerType)
    {
        using var fixture = new ManagerFixture([]);
        using var manager = (AsyncCustomNodeManager)Activator.CreateInstance(managerType,
            fixture.Manager.Server, new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var references = new Dictionary<NodeId, IList<IReference>>();
        Func<Task> create = () => manager.CreateAddressSpaceAsync(references, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        references.Should().BeEmpty();
    }

    [Test]
    public async Task BuiltInPlugins_PreCanceled_DoNotConstructNodesAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Type[] types = typeof(PlcNodeManager).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IPluginNodes).IsAssignableFrom(type)).ToArray();
        types.Should().NotBeEmpty();
        foreach (Type type in types)
        {
            var plugin = (IPluginNodes)Activator.CreateInstance(type, new TimeService(), NullLogger.Instance);
            Func<Task> register = () => plugin.AddToAddressSpaceAsync(null, null, null, cancellation.Token).AsTask();

            var failure = await register.Should().ThrowAsync<OperationCanceledException>(type.Name)
                .ConfigureAwait(false);
            failure.Which.CancellationToken.Should().Be(cancellation.Token);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FilePlugin_InvalidModel_ReleasesFileAndContinuesAsync(bool binary)
    {
        string path = Path.GetTempFileName();
        try
        {
            IPluginNodes plugin = binary
                ? new UaNodesPluginNodes(new TimeService(), NullLogger.Instance)
                : new NodeSet2PluginNodes(new TimeService(), NullLogger.Instance);
            var options = new Mono.Options.OptionSet();
            plugin.AddOptions(options);
            options.Parse([$"--{(binary ? "unf" : "ns2")}={path}"]);
            using var fixture = new ManagerFixture([plugin]);

            await fixture.Manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);

            using var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            file.Length.Should().Be(0);
            plugin.Nodes.Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task SimpleEvents_PreCanceledCreate_DoesNotRegisterModelAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateSimpleEventsManager();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> create = () => manager.CreateAddressSpaceAsync(
            fixture.ExternalReferences, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        fixture.ExternalReferences.Should().BeEmpty();
        var typeId = ExpandedNodeId.ToNodeId(
            SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, manager.Server.NamespaceUris);
        manager.FindPredefinedNode<BaseObjectTypeState>(typeId).Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SimpleEvents_EmitsPairedPayloadsAsync(bool cancelDeletionBeforeTick)
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateSimpleEventsManager();
        var events = new ConcurrentQueue<BaseEventState>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock.Get(manager.Server).Setup(server => server.ReportEvent(It.IsAny<IFilterTarget>()))
            .Callback<IFilterTarget>(value =>
            {
                events.Enqueue((BaseEventState)value);
                if (events.Count == 2)
                {
                    received.TrySetResult();
                }
            });

        await manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        try
        {
            await manager.Server.LoadComplexTypesAsync(manager.Server.Telemetry).ConfigureAwait(false);
            var typeId = ExpandedNodeId.ToNodeId(
                SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, manager.Server.NamespaceUris);
            var handle = await manager.GetManagerHandleAsync(typeId).ConfigureAwait(false);
            handle.Should().BeOfType<NodeHandle>().Which.Validated.Should().BeTrue();
            (await manager.GetManagerHandleAsync(Opc.Ua.ObjectIds.ObjectsFolder).ConfigureAwait(false))
                .Should().BeNull();
            if (cancelDeletionBeforeTick)
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                Func<Task> delete = () => manager.DeleteAddressSpaceAsync(cancellation.Token).AsTask();
                await delete.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
                manager.FindPredefinedNode<BaseObjectTypeState>(typeId).Should().NotBeNull();
            }

            await received.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        finally
        {
            await manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
        }

        BaseEventState[] values = events.ToArray();
        values.Should().HaveCount(2);
        for (int index = 0; index < values.Length; index++)
        {
            var value = values[index];
            var properties = new List<BaseInstanceState>();
            value.GetChildren(manager.SystemContext, properties);
            Variant ReadProperty(string name) => properties.OfType<BaseVariableState>()
                .Single(property => property.BrowseName == new QualifiedName(name, manager.NamespaceIndex)).Value;
            ReadProperty("CycleId").GetString().Should().Be((index + 1).ToString());
            value.EventType.Value.Should().Be(ExpandedNodeId.ToNodeId(
                SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, manager.Server.NamespaceUris));
            value.Severity.Value.Should().Be((ushort)(index + 1));
            value.SourceNode.Value.Should().Be(Opc.Ua.ObjectIds.Server);
            value.SourceName.Value.Should().Be("System");
            value.Message.Value.Text.Should().Be($"The system cycle '{index + 1}' has started.");
            value.EventId.Value.Length.Should().BePositive();
            AssertRuntimeStep(ReadProperty("CurrentStep"));
            var steps = ReadProperty("Steps").GetExtensionObjectArray();
            steps.Count.Should().Be(2);
            foreach (var step in steps)
            {
                AssertRuntimeStep(new Variant(step));
            }
        }
        values[0].EventId.Value.Should().NotBe(values[1].EventId.Value);
    }

    private static void AssertRuntimeStep(Variant value)
    {
        value.TryGetStructure(out IEncodeable body).Should().BeTrue();
        var step = body.Should().BeAssignableTo<IStructure>().Subject;
        step["Name"].GetString().Should().Be("Step 1");
        step["Duration"].GetDouble().Should().Be(1000);
    }

    [Test]
    public async Task SimpleEvents_DeleteAddressSpace_DrainsInFlightEventAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateSimpleEventsManager();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int reports = 0;
        Mock.Get(manager.Server).Setup(server => server.ReportEvent(It.IsAny<IFilterTarget>()))
            .Callback(() =>
            {
                Interlocked.Increment(ref reports);
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Event report was not released by the test.");
                }
            });

        await manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        var typeId = ExpandedNodeId.ToNodeId(
            SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, manager.Server.NamespaceUris);
        Task deletion = null;
        try
        {
            await manager.Server.LoadComplexTypesAsync(manager.Server.Telemetry).ConfigureAwait(false);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            deletion = manager.DeleteAddressSpaceAsync().AsTask();
            deletion.IsCompleted.Should().BeFalse("the active event callback must drain first");
            manager.FindPredefinedNode<BaseObjectTypeState>(typeId).Should().NotBeNull();
        }
        finally
        {
            release.Set();
            await (deletion ?? manager.DeleteAddressSpaceAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        Volatile.Read(ref reports).Should().Be(1, "deletion stops the second event in the pair");
        (await manager.GetManagerHandleAsync(typeId).ConfigureAwait(false)).Should().BeNull();
        await manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task SimpleEvents_Dispose_ClearsNativeModelAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateSimpleEventsManager();
        await manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        var typeId = ExpandedNodeId.ToNodeId(
            SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, manager.Server.NamespaceUris);
        manager.FindPredefinedNode<BaseObjectTypeState>(typeId).Should().NotBeNull();

        manager.Dispose();

        manager.FindPredefinedNode<BaseObjectTypeState>(typeId).Should().BeNull();
    }

    [Test]
    public async Task AlarmManager_PreCanceledCreate_DoesNotAddReferencesAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateAlarmManager();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> create = () => manager.CreateAddressSpaceAsync(
            fixture.ExternalReferences, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        fixture.ExternalReferences.Should().BeEmpty();
    }

    [Test]
    public async Task AlarmManager_ResolvesDynamicNodesAndDeletesAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateAlarmManager();
        await manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        var sourceId = ModelUtils.ConstructIdForSource("Metals/SouthMotor", manager.NamespaceIndex);
        var areaId = ModelUtils.ConstructIdForArea("East/Blue", manager.NamespaceIndex);
        SourceState registeredSource = null;
        UnderlyingSystemSource registeredBackend = null;
        try
        {
            var source = (SourceState)await manager.ResolveAsync(sourceId).ConfigureAwait(false);
            registeredSource = source;
            source.BrowseName.Name.Should().Be("SouthMotor");
            (await manager.ResolveAsync(areaId).ConfigureAwait(false)).Should().BeOfType<AreaState>();
            (await manager.ResolveAsync(Opc.Ua.ObjectIds.ObjectsFolder).ConfigureAwait(false)).Should().BeNull();
            var missingId = ModelUtils.ConstructIdForSource("Metals/Missing", manager.NamespaceIndex);
            (await manager.ResolveAsync(missingId).ConfigureAwait(false)).Should().BeNull();
            var system = (UnderlyingSystem)manager.SystemContext.SystemHandle;
            system.TryGetSource("SouthMotor", out var backend).Should().BeTrue();
            registeredBackend = backend;
            backend.Refresh();
            var children = new List<BaseInstanceState>();
            source.GetChildren(manager.SystemContext, children);
            var alarm = children.OfType<AlarmConditionState>().Single(node => node.SymbolicName == "Bronze");
            (await manager.ResolveAsync(alarm.NodeId).ConfigureAwait(false)).Should().BeSameAs(alarm);
            (await manager.ResolveAsync(alarm.AddComment.NodeId).ConfigureAwait(false))
                .Should().BeSameAs(alarm.AddComment);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Func<Task> delete = () => manager.DeleteAddressSpaceAsync(cancellation.Token).AsTask();
            await delete.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            (await manager.ResolveAsync(sourceId).ConfigureAwait(false)).Should().BeSameAs(source);
        }
        finally
        {
            await manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
        }

        (await manager.ResolveAsync(sourceId).ConfigureAwait(false)).Should().BeNull();
        (await manager.ResolveAsync(areaId).ConfigureAwait(false)).Should().BeNull();
        registeredBackend.OnAlarmChanged.Should().BeNull();
        var events = new List<IFilterTarget>();
        registeredSource.ConditionRefresh(manager.SystemContext, events, true);
        events.Should().BeEmpty();
        await manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task AlarmManager_Delete_DrainsSystemEventCallbackAsync()
    {
        using var fixture = new ManagerFixture([]);
        using var manager = fixture.CreateAlarmManager();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int reports = 0;
        Mock.Get(manager.Server).Setup(server => server.ReportEvent(It.IsAny<IFilterTarget>()))
            .Callback(() =>
            {
                Interlocked.Increment(ref reports);
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("System event callback was not released.");
                }
            });
        await manager.CreateAddressSpaceAsync(fixture.ExternalReferences).ConfigureAwait(false);
        Task deletion = null;
        var areaId = ModelUtils.ConstructIdForArea("/Green", manager.NamespaceIndex);
        try
        {
            await ((UnderlyingSystem)manager.SystemContext.SystemHandle).StopSimulationAsync().ConfigureAwait(false);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            deletion = manager.DeleteAddressSpaceAsync().AsTask();
            deletion.Wait(TimeSpan.FromMilliseconds(250)).Should().BeFalse(
                "deletion must remain blocked on the held system-event callback");
            (await manager.ResolveAsync(areaId).ConfigureAwait(false)).Should().NotBeNull();
        }
        finally
        {
            release.Set();
            await (deletion ?? manager.DeleteAddressSpaceAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        Volatile.Read(ref reports).Should().Be(1, "the paired audit event stops when deletion begins");
        (await manager.ResolveAsync(areaId).ConfigureAwait(false)).Should().BeNull();
    }

    [Test]
    public async Task DeterministicManager_PreCanceledCreate_DoesNotStartScriptAsync()
    {
        using var fixture = new ManagerFixture([]);
        var time = new Mock<TimeService>(MockBehavior.Strict);
        using var manager = new OpcPlc.DeterministicAlarms.DeterministicAlarmsNodeManager(
            fixture.Manager.Server, new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() },
            time.Object, Path.Combine(TestContext.CurrentContext.TestDirectory, "DeterministicAlarmsTests", "dalm001.json"),
            NullLogger.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> create = () => manager.CreateAddressSpaceAsync(
            fixture.ExternalReferences, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        fixture.ExternalReferences.Should().BeEmpty();
        time.Invocations.Should().BeEmpty();
        manager.FindPredefinedNode<NodeState>(new NodeId("VendingMachines", manager.NamespaceIndex)).Should().BeNull();
    }

    private sealed class TestAlarmManager(IServerInternal server)
        : AlarmConditionServerNodeManager(server,
            new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() },
            NullLogger.Instance, server.Telemetry)
    {
        public async ValueTask<NodeState> ResolveAsync(NodeId id)
        {
            var handle = (NodeHandle)await GetManagerHandleAsync(id).ConfigureAwait(false);
            return await ValidateNodeAsync(SystemContext, handle, new Dictionary<NodeId, NodeState>())
                .ConfigureAwait(false);
        }
    }

    private sealed class ManagerFixture : IDisposable
    {
        public PlcNodeManager Manager { get; }
        public Dictionary<NodeId, IList<IReference>> ExternalReferences { get; } = new();

        public TestAlarmManager CreateAlarmManager()
        {
            var manager = new TestAlarmManager(Manager.Server);
            Mock.Get(Manager.Server.NodeManager).SetupGet(master => master.AsyncNodeManagers)
                .Returns([Manager, manager]);
            return manager;
        }

        public SimpleEventsNodeManager CreateSimpleEventsManager()
        {
            var typeTable = (TypeTable)Manager.Server.TypeTree;
            typeTable.AddSubtype(Opc.Ua.DataTypeIds.BaseDataType, NodeId.Null);
            typeTable.AddSubtype(Opc.Ua.DataTypeIds.Structure, Opc.Ua.DataTypeIds.BaseDataType);
            typeTable.AddSubtype(Opc.Ua.ObjectTypeIds.BaseObjectType, NodeId.Null);
            typeTable.AddSubtype(Opc.Ua.ObjectTypeIds.BaseEventType, Opc.Ua.ObjectTypeIds.BaseObjectType);
            typeTable.AddSubtype(Opc.Ua.ObjectTypeIds.SystemEventType, Opc.Ua.ObjectTypeIds.BaseEventType);
            var manager = new SimpleEventsNodeManager(Manager.Server,
                new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() }, NullLogger.Instance);
            Mock.Get(Manager.Server.NodeManager).SetupGet(master => master.AsyncNodeManagers)
                .Returns([Manager, manager]);
            Mock.Get(Manager.Server.NodeManager).Setup(master => master.FindNodeInAddressSpaceAsync(
                It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .Returns<NodeId, CancellationToken>((nodeId, _) =>
                    ValueTask.FromResult(manager.FindPredefinedNode<NodeState>(nodeId)));
            return manager;
        }

        public ManagerFixture(ImmutableList<IPluginNodes> plugins)
        {
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:server");
            var server = new Mock<IServerInternal>();
            server.SetupGet(instance => instance.Telemetry).Returns(DefaultTelemetry.Create(_ => { }));
            server.SetupGet(instance => instance.NamespaceUris).Returns(namespaces);
            server.SetupGet(instance => instance.ServerUris).Returns(new StringTable());
            server.SetupGet(instance => instance.TypeTree).Returns(new TypeTable(namespaces));
            server.SetupGet(instance => instance.Factory).Returns(EncodeableFactory.Create());
            server.SetupGet(instance => instance.DefaultSystemContext).Returns(new ServerSystemContext(server.Object));
            server.SetupGet(instance => instance.NodeManager).Returns(Mock.Of<IMasterNodeManager>());
            Manager = new PlcNodeManager(server.Object, new OpcPlcConfiguration(),
                new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() },
                new TimeService(), new PlcSimulation(plugins), plugins, NullLogger.Instance);
        }

        public void Dispose() => Manager.Dispose();
    }
}
