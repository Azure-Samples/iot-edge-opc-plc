namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StandardServer = Opc.Ua.Server.StandardServer;

/// <summary>
/// Regression tests for a server hang (thread-pool starvation) that started as soon as a session
/// subscribed to events on the Server object while the alarm, simple events and Boiler2 simulations
/// were reporting events.
/// The anonymous fixture is the control case: same server setup, but an Anonymous session.
/// </summary>
[TestFixture(UserTokenType.UserName)]
[TestFixture(UserTokenType.Anonymous)]
[NonParallelizable]
public class ServerEventSubscriptionTests
{
    private const string UserName = "user1";
    private const string Password = "password";

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BlockedProbeTimeout = TimeSpan.FromSeconds(10);
    private const int BlockedRequestCount = 5;
    private static readonly TimeSpan SimulationDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(30);

    private readonly UserTokenType _userTokenType;
    private readonly PlcSimulatorFixture _simulator;
    private ISession _session;

    public ServerEventSubscriptionTests(UserTokenType userTokenType)
    {
        _userTokenType = userTokenType;

        // Mirrors the command line of the Azure IoT Operations E2E instance that hung.
        string[] args =
        [
            "--ut",
            "--fn=10",
            "--gn=5",
            "--maxsessioncount=100",
            "--maxsubscriptioncount=100",
            "--maxqueuedrequestcount=2000",
            "--ses",
            "--alm",
            "--wotcon",
            "--drurs",
            "--au=sysadmin",
            "--ac=demo",
            $"--du={UserName}",
            $"--dc={Password}",
        ];

        if (userTokenType == UserTokenType.UserName)
        {
            args = [.. args, "--daa"];
        }

        _simulator = new PlcSimulatorFixture(args);
    }

    [OneTimeSetUp]
    public async Task Setup()
    {
        await _simulator.StartAsync().ConfigureAwait(false);

        IUserIdentity identity = _userTokenType == UserTokenType.UserName
            ? new UserIdentity(UserName, Encoding.UTF8.GetBytes(Password))
            : new UserIdentity(new AnonymousIdentityToken());

        _session = await _simulator.CreateSessionAsync(
            $"{nameof(ServerEventSubscriptionTests)}_{_userTokenType}",
            identity,
            useSecurity: true).ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        // Bounded, so a hung server fails the test instead of hanging the test run.
        try
        {
            if (_session is not null)
            {
                await _session.CloseAsync().WaitAsync(ShutdownTimeout).ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                if (_session is IAsyncDisposable asyncSession)
                {
                    await asyncSession.DisposeAsync().AsTask().WaitAsync(ShutdownTimeout).ConfigureAwait(false);
                }
                else
                {
                    _session?.Dispose();
                }
            }
            finally
            {
                await _simulator.StopAsync().WaitAsync(ShutdownTimeout).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reproduces the hang deterministically. In SDK 1.5.378, every service call completes through
    /// <c>StandardServer.OnRequestComplete</c>, which blocks synchronously on the same semaphore that
    /// GetEndpoints/FindServers/CreateSession acquire with <c>WaitAsync</c>. On a starved thread pool the
    /// continuation of an async waiter that was granted the semaphore can be stranded in the local queue of a
    /// thread that is itself blocked in <c>OnRequestComplete</c>, so the semaphore is never released again.
    /// Holding the semaphore here recreates that state without having to starve the thread pool.
    /// </summary>
    [Test]
    public async Task RequestCompletion_DoesNotBlockWhileServerSemaphoreIsHeld()
    {
        var counters = new NotificationCounters();
        Subscription subscription = await CreateEventSubscriptionAsync(counters).ConfigureAwait(false);

        SemaphoreSlim serverSemaphore = GetStandardServerSemaphore(_simulator.Server);
        await serverSemaphore.WaitAsync().ConfigureAwait(false);

        Task<ArrayOf<EndpointDescription>> getEndpoints;
        bool readsCompleted;
        int eventsWhileHeld;
        int dataChangesWhileHeld;
        int threadCountBefore = ThreadPool.ThreadCount;
        try
        {
            // Queued behind the held semaphore, like the GetEndpoints calls that timed out in production.
            getEndpoints = Task.Run(() => _simulator.GetEndpointsAsync(CancellationToken.None));

            int eventsBefore = Volatile.Read(ref counters.Events);
            int dataChangesBefore = Volatile.Read(ref counters.DataChanges);

            Task[] reads = Enumerable.Range(0, BlockedRequestCount)
                .Select(_ => Task.Run(async () => await _session.ReadValueAsync(
                    VariableIds.Server_ServerStatus_CurrentTime).ConfigureAwait(false)))
                .ToArray();
            readsCompleted = await WaitAsync(Task.WhenAll(reads), BlockedProbeTimeout).ConfigureAwait(false);

            // Publish responses complete through OnRequestComplete as well. Fire the Boiler2 overheat and
            // maintenance timers so that events are reported while the semaphore is held.
            for (int second = 0; second < 5; second++)
            {
                _simulator.QueueTimersWithPeriod(1000);
                _simulator.QueueTimersWithPeriod(second % 2 == 0 ? 120_000u : 300_000u);
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            eventsWhileHeld = Volatile.Read(ref counters.Events) - eventsBefore;
            dataChangesWhileHeld = Volatile.Read(ref counters.DataChanges) - dataChangesBefore;
        }
        finally
        {
            serverSemaphore.Release();
        }

        string diagnostics = $"thread pool threads before: {threadCountBefore}, while held: {ThreadPool.ThreadCount}, " +
            $"pending work items: {ThreadPool.PendingWorkItemCount}, events while held: {eventsWhileHeld}, " +
            $"data changes while held: {dataChangesWhileHeld}";

        readsCompleted.Should().BeTrue(
            $"session requests must not block thread-pool threads on the StandardServer semaphore ({diagnostics})");
        dataChangesWhileHeld.Should().BePositive($"Publish responses must still be delivered ({diagnostics})");
        eventsWhileHeld.Should().BePositive($"events must still be delivered ({diagnostics})");

        (await WaitAsync(getEndpoints, ProbeTimeout).ConfigureAwait(false)).Should().BeTrue(
            "GetEndpoints must complete once the semaphore is released");
        (await getEndpoints.ConfigureAwait(false)).ToArray().Should().NotBeEmpty();

        await subscription.DeleteAsync(silent: true).WaitAsync(ProbeTimeout).ConfigureAwait(false);
    }

    [Test]
    public async Task SubscribeToServerEvents_ServerStaysResponsive()
    {
        var counters = new NotificationCounters();
        Subscription subscription = await CreateEventSubscriptionAsync(counters).ConfigureAwait(false);

        int threadCountBefore = ThreadPool.ThreadCount;

        // Drive the mocked-time simulations like the real 1 s timers would, and fire the Boiler2
        // overheat (120 s) and maintenance (300 s) timers so that Boiler2 reports events too.
        // Alarm (--alm) and simple events (--ses) run on real timers.
        for (int second = 1; second <= SimulationDuration.TotalSeconds; second++)
        {
            _simulator.QueueTimersWithPeriod(1000);

            if (second == 2)
            {
                _simulator.QueueTimersWithPeriod(120_000);
            }

            if (second == 4)
            {
                _simulator.QueueTimersWithPeriod(300_000);
            }

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        // A sessionless GetEndpoints and a Read on the session must both still be served.
        var getEndpoints = Task.Run(() => _simulator.GetEndpointsAsync(CancellationToken.None));
        var read = Task.Run(async () => await _session.ReadValueAsync(
            VariableIds.Server_ServerStatus_CurrentTime).ConfigureAwait(false));

        bool getEndpointsCompleted = await WaitAsync(getEndpoints, ProbeTimeout).ConfigureAwait(false);
        bool readCompleted = await WaitAsync(read, ProbeTimeout).ConfigureAwait(false);

        int threadCountAfter = ThreadPool.ThreadCount;
        int eventCount = Volatile.Read(ref counters.Events);
        int dataChangeCount = Volatile.Read(ref counters.DataChanges);
        string diagnostics = $"thread pool threads before: {threadCountBefore}, after: {threadCountAfter}, " +
            $"pending work items: {ThreadPool.PendingWorkItemCount}, events received: {eventCount}, " +
            $"data changes received: {dataChangeCount}";

        getEndpointsCompleted.Should().BeTrue($"a sessionless GetEndpoints call must complete within {ProbeTimeout} ({diagnostics})");
        readCompleted.Should().BeTrue($"a Read on the session must complete within {ProbeTimeout} ({diagnostics})");
        (await getEndpoints.ConfigureAwait(false)).ToArray().Should().NotBeEmpty();
        StatusCode.IsGood((await read.ConfigureAwait(false)).StatusCode).Should().BeTrue();

        // Every blocked timer tick parks one more thread-pool thread, so a hung server keeps adding threads.
        (threadCountAfter - threadCountBefore).Should().BeLessThan(
            (int)SimulationDuration.TotalSeconds,
            $"the thread pool must not keep growing ({diagnostics})");

        eventCount.Should().BePositive($"events must be delivered to the session ({diagnostics})");
        dataChangeCount.Should().BePositive($"data changes must be delivered to the session ({diagnostics})");

        await subscription.DeleteAsync(silent: true).WaitAsync(ProbeTimeout).ConfigureAwait(false);
    }

    private async Task<Subscription> CreateEventSubscriptionAsync(NotificationCounters counters)
    {
        _session.Identity.TokenType.Should().Be(_userTokenType);
        _session.Endpoint.SecurityMode.Should().Be(MessageSecurityMode.SignAndEncrypt);

        // Same subscription parameters as the E2E client.
        var subscription = new Subscription(_session.DefaultSubscription) {
            PublishingInterval = 1000,
            KeepAliveCount = 5,
            LifetimeCount = 120,
            Priority = 255,
            MaxNotificationsPerPublish = 1000,
        };

        var eventItem = new MonitoredItem(subscription.DefaultItem) {
            StartNodeId = ObjectIds.Server,
            NodeClass = NodeClass.Object,
            AttributeId = Attributes.EventNotifier,
            SamplingInterval = 0,
            QueueSize = 1000,
            Filter = CreateEventFilter(),
        };
        eventItem.Notification += (_, _) => Interlocked.Increment(ref counters.Events);

        var dataItem = new MonitoredItem(subscription.DefaultItem) {
            StartNodeId = VariableIds.Server_ServerStatus_CurrentTime,
            AttributeId = Attributes.Value,
            SamplingInterval = 1000,
        };
        dataItem.Notification += (_, _) => Interlocked.Increment(ref counters.DataChanges);

        subscription.AddItem(dataItem);
        subscription.AddItem(eventItem);
        _session.AddSubscription(subscription);
        await subscription.CreateAsync().ConfigureAwait(false);

        return subscription;
    }

    private static SemaphoreSlim GetStandardServerSemaphore(StandardServer server)
    {
        FieldInfo field = typeof(StandardServer).GetField("m_semaphoreSlim", BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull("the test needs the StandardServer semaphore that GetEndpoints and CreateSession wait on");
        return (SemaphoreSlim)field.GetValue(server);
    }

    private static EventFilter CreateEventFilter()
    {
        var filter = new EventFilter();
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.EventId));
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.EventType));
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.SourceNode));
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.Time));
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.Message));
        filter.AddSelectClause(ObjectTypeIds.BaseEventType, new QualifiedName(BrowseNames.Severity));
        return filter;
    }

    private static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (completed != task)
        {
            return false;
        }

        await task.ConfigureAwait(false);
        return true;
    }

    private sealed class NotificationCounters
    {
        public int Events;
        public int DataChanges;
    }
}
