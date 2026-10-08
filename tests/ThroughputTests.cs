namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client.Subscriptions;
using Opc.Ua.Client.Subscriptions.MonitoredItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Performance/throughput tests for OPC UA server publishing.
/// Verifies the server delivers data change notifications
/// for many fast-changing nodes without significant loss,
/// both in burst and sustained scenarios.
/// </summary>
[TestFixture]
public class ThroughputTests : SimulatorTestsBase
{
    private const int NodeCount = 250;
    private const uint NodeRateMs = 100;

    public ThroughputTests() : base([$"--fn={NodeCount}", $"--vfr={NodeRateMs}", "--ft=uint"])
    {
    }

    /// <summary>
    /// Burst throughput: fire all timer events as fast as possible,
    /// then verify the OPC UA stack delivers all notifications.
    /// Tests OPC UA server buffering under high burst load.
    /// </summary>
    [TestCase(250, 100u, 1000)]
    public async Task FastNodes_BurstThroughput(int nodeCount, uint rateMs, int timerFires)
    {
        var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);
        await using var cleanup = context.ConfigureAwait(false);

        int expectedTotal = nodeCount * timerFires;

        var sw = Stopwatch.StartNew();
        FireTimersWithPeriod(TimeSpan.FromMilliseconds(rateMs), timerFires);
        var fireElapsed = sw.Elapsed;

        int actualCount = await WaitForNotificationsAsync(context.Notifications, expectedTotal, maxWaitSeconds: 60).ConfigureAwait(false);
        var totalElapsed = sw.Elapsed;
        double ratio = (double)actualCount / expectedTotal;
        double notificationsPerSecond = actualCount / totalElapsed.TotalSeconds;

        TestContext.Progress.WriteLine(
            $"Burst: {actualCount}/{expectedTotal} ({ratio:P1}), " +
            $"fire={fireElapsed.TotalSeconds:F2}s, total={totalElapsed.TotalSeconds:F2}s, " +
            $"rate={notificationsPerSecond:N0} notif/s");

        ratio.Should().BeGreaterOrEqualTo(0.95,
            $"expected at least 95% of {expectedTotal} burst notifications, got {actualCount} ({ratio:P1})");
    }

    /// <summary>
    /// Sustained throughput: fire timer events in small batches with
    /// real-time delays between them, simulating the server running under steady load.
    /// Measures that the OPC UA publishing mechanism keeps up with continuous updates.
    /// </summary>
    [TestCase(250, 100u, 5, 200, 20)]
    public async Task FastNodes_SustainedThroughput(
        int nodeCount, uint rateMs, int batchSize, int batches, int delayMs)
    {
        var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);
        await using var cleanup = context.ConfigureAwait(false);

        int totalFires = batchSize * batches;
        int expectedTotal = nodeCount * totalFires;

        var sw = Stopwatch.StartNew();
        for (int b = 0; b < batches; b++)
        {
            FireTimersWithPeriod(TimeSpan.FromMilliseconds(rateMs), batchSize);
            await Task.Delay(delayMs).ConfigureAwait(false);
        }

        var fireElapsed = sw.Elapsed;
        int actualCount = await WaitForNotificationsAsync(context.Notifications, expectedTotal, maxWaitSeconds: 60).ConfigureAwait(false);
        var totalElapsed = sw.Elapsed;
        double ratio = (double)actualCount / expectedTotal;
        double notificationsPerSecond = actualCount / totalElapsed.TotalSeconds;

        TestContext.Progress.WriteLine(
            $"Sustained: {actualCount}/{expectedTotal} ({ratio:P1}), " +
            $"fire={fireElapsed.TotalSeconds:F2}s, total={totalElapsed.TotalSeconds:F2}s, " +
            $"rate={notificationsPerSecond:N0} notif/s");

        ratio.Should().BeGreaterOrEqualTo(0.95,
            $"expected at least 95% of {expectedTotal} sustained notifications, got {actualCount} ({ratio:P1})");
    }

    /// <summary>
    /// Measures and asserts a minimum processing rate (notifications per second).
    /// Fires a large burst and verifies notifications are received in bounded time.
    /// </summary>
    [TestCase(250, 100u, 400, 10_000)]
    public async Task FastNodes_MinimumProcessingRate(
        int nodeCount, uint rateMs, int timerFires, int minNotificationsPerSecond)
    {
        var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);
        await using var cleanup = context.ConfigureAwait(false);

        int expectedTotal = nodeCount * timerFires;

        var sw = Stopwatch.StartNew();
        FireTimersWithPeriod(TimeSpan.FromMilliseconds(rateMs), timerFires);
        int actualCount = await WaitForNotificationsAsync(context.Notifications, expectedTotal, maxWaitSeconds: 120).ConfigureAwait(false);
        var elapsed = sw.Elapsed;

        double rate = actualCount / elapsed.TotalSeconds;

        TestContext.Progress.WriteLine(
            $"Rate: {actualCount}/{expectedTotal} in {elapsed.TotalSeconds:F2}s, " +
            $"rate={rate:N0} notif/s (min={minNotificationsPerSecond:N0})");

        actualCount.Should().Be(expectedTotal,
            $"all {expectedTotal} notifications should be received");

        rate.Should().BeGreaterOrEqualTo(minNotificationsPerSecond,
            $"processing rate should be at least {minNotificationsPerSecond:N0} notif/s, was {rate:N0}");
    }

    private async Task<SubscriptionContext> SetupSubscriptionAsync(int nodeCount)
    {
        Session.TryGetSubscriptionManager(out var manager).Should().BeTrue("managed sessions use the V2 engine");
        manager.PoolNotifications.Should().BeFalse("these tests retain notification payloads after callbacks return");
        var context = new SubscriptionContext();
        context.Subscription = manager.Add(context,
            new OptionsMonitor<SubscriptionOptions>(new()
            {
                PublishingEnabled = true,
                PublishingInterval = TimeSpan.FromMilliseconds(100),
                LifetimeCount = 1000,
                KeepAliveCount = 100,
                MaxNotificationsPerPublish = 0,
                Priority = 255,
                MinLifetimeInterval = MinimumSubscriptionLifetime
            }));
        try
        {
            var monitoredItems = new List<IMonitoredItem>();
            for (int index = 1; index <= nodeCount; index++)
            {
                string name = $"FastUInt{index}";
                context.Subscription.MonitoredItems.TryAdd(name,
                    new OptionsMonitor<MonitoredItemOptions>(new()
                    {
                        StartNodeId = GetOpcPlcNodeId(name),
                        SamplingInterval = TimeSpan.Zero,
                        AttributeId = Attributes.Value,
                        QueueSize = 10000,
                        DiscardOldest = true
                    }), out var item).Should().BeTrue();
                monitoredItems.Add(item);
            }

            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (monitoredItems.Any(item => !item.Created))
            {
                foreach (var item in monitoredItems)
                {
                    ServiceResult.IsBad(item.Error).Should().BeFalse($"monitored item creation failed: {item.Error}");
                }
                await Task.Delay(25, deadline.Token).ConfigureAwait(false);
            }

            await Task.Delay(3000).ConfigureAwait(false);
            context.Notifications.Clear();
            return context;
        }
        catch
        {
            await context.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> WaitForNotificationsAsync(
        ConcurrentQueue<DataValue> notifications,
        int expectedTotal,
        int maxWaitSeconds)
    {
        var sw = Stopwatch.StartNew();
        int previousCount = 0;
        int stableIterations = 0;

        while (sw.Elapsed < TimeSpan.FromSeconds(maxWaitSeconds))
        {
            await Task.Delay(500).ConfigureAwait(false);

            int currentCount = notifications.Count;

            if (currentCount >= expectedTotal)
            {
                break;
            }

            // If no new notifications arrived for several consecutive polls, stop waiting.
            if (currentCount == previousCount)
            {
                stableIterations++;
                if (stableIterations >= 6) // 3 seconds of no new data
                {
                    break;
                }
            }
            else
            {
                stableIterations = 0;
            }

            previousCount = currentCount;
        }

        return notifications.Count;
    }

    private sealed class SubscriptionContext : IAsyncDisposable, ISubscriptionNotificationHandler
    {
        public ISubscription Subscription { get; set; }

        public ConcurrentQueue<DataValue> Notifications { get; } = new();

        public ValueTask DisposeAsync() => Subscription.DisposeAsync();

        public ValueTask OnDataChangeNotificationAsync(ISubscription subscription, uint sequenceNumber,
            DateTime publishTime, ReadOnlyMemory<DataValueChange> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable)
        {
            foreach (var change in notification.Span)
            {
                Notifications.Enqueue(change.Value);
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask OnEventDataNotificationAsync(ISubscription subscription, uint sequenceNumber,
            DateTime publishTime, ReadOnlyMemory<EventNotification> notification, PublishState publishStateMask,
            IReadOnlyList<string> stringTable) => ValueTask.CompletedTask;

        public ValueTask OnKeepAliveNotificationAsync(ISubscription subscription, uint sequenceNumber,
            DateTime publishTime, PublishState publishStateMask) => ValueTask.CompletedTask;

        public ValueTask OnSubscriptionStateChangedAsync(ISubscription subscription,
            SubscriptionState state, PublishState publishStateMask,
            CancellationToken ct = default) => ValueTask.CompletedTask;
    }
}
