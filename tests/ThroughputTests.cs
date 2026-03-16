namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        using var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);

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
        using var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);

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
        using var context = await SetupSubscriptionAsync(nodeCount).ConfigureAwait(false);

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
        var subscription = new Subscription(Session.DefaultSubscription)
        {
            PublishingInterval = 100,
            LifetimeCount = 1000,
            KeepAliveCount = 100,
            MaxNotificationsPerPublish = 0, // unlimited
        };

        Session.AddSubscription(subscription);
        await subscription.CreateAsync().ConfigureAwait(false);

        var notifications = new ConcurrentQueue<MonitoredItemNotificationEventArgs>();
        var monitoredItems = new List<MonitoredItem>();

        for (int i = 1; i <= nodeCount; i++)
        {
            var nodeId = GetOpcPlcNodeId($"FastUInt{i}");
            var item = new MonitoredItem(subscription.DefaultItem)
            {
                DisplayName = $"FastUInt{i}",
                StartNodeId = nodeId,
                NodeClass = NodeClass.Variable,
                SamplingInterval = 0,
                AttributeId = Attributes.Value,
                QueueSize = 10000,
                DiscardOldest = true,
            };

            item.Notification += (_, e) => notifications.Enqueue(e);
            monitoredItems.Add(item);
        }

        subscription.AddItems(monitoredItems);
        await subscription.ApplyChangesAsync().ConfigureAwait(false);

        // Wait for initial value notifications and discard them.
        await Task.Delay(3000).ConfigureAwait(false);
        while (notifications.TryDequeue(out _))
        {
        }

        return new SubscriptionContext(Session, subscription, notifications);
    }

    private static async Task<int> WaitForNotificationsAsync(
        ConcurrentQueue<MonitoredItemNotificationEventArgs> notifications,
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

    private sealed class SubscriptionContext(Session session, Subscription subscription, ConcurrentQueue<MonitoredItemNotificationEventArgs> notifications) : IDisposable
    {
        public ConcurrentQueue<MonitoredItemNotificationEventArgs> Notifications => notifications;

        public void Dispose()
        {
            subscription.DeleteAsync(true).GetAwaiter().GetResult();
            session.RemoveSubscriptionAsync(subscription).GetAwaiter().GetResult();
        }
    }
}
