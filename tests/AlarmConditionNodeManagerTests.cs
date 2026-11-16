namespace OpcPlc.Tests;

using FluentAssertions;
using global::AlarmCondition;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Regression test for a deadlock between the alarm node manager lock and the diagnostics node manager lock.
/// The first namespace metadata lookup holds the diagnostics lock and asks every node manager for its handle.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AlarmConditionNodeManagerTests
{
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(5);

    private readonly PlcSimulatorFixture _simulator = new(["--alm"]);

    [OneTimeSetUp]
    public Task Setup() => _simulator.StartAsync();

    [OneTimeTearDown]
    public Task TearDown() => _simulator.StopAsync();

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetManagerHandle_DoesNotWaitForUnrelatedLock(bool lookupAlarmSource)
    {
        var alarmNodeManager = _simulator.Server.AlarmNodeManager;
        object syncRoot = lookupAlarmSource ? alarmNodeManager.SourceStateSyncRoot : alarmNodeManager.SyncRoot;
        NodeId nodeId = lookupAlarmSource
            ? ModelUtils.ConstructIdForSource("Metals/SouthMotor", alarmNodeManager.NamespaceIndex)
            : ObjectIds.Server;
        using var lockHeld = new ManualResetEventSlim();
        using var releaseLock = new ManualResetEventSlim();
        Task holder = Task.Run(() =>
        {
            lock (syncRoot)
            {
                lockHeld.Set();
                releaseLock.Wait(TimeSpan.FromSeconds(30));
            }
        });

        try
        {
            lockHeld.Wait(LookupTimeout).Should().BeTrue();

            Task<object> lookup = Task.Run(() => alarmNodeManager.GetManagerHandleAsync(nodeId).AsTask());
            Task completed = await Task.WhenAny(lookup, Task.Delay(LookupTimeout)).ConfigureAwait(false);

            completed.Should().BeSameAs(lookup, "permission lookups must not wait for an unrelated lock");
            object handle = await lookup.ConfigureAwait(false);
            if (lookupAlarmSource)
            {
                handle.Should().NotBeNull();
            }
            else
            {
                handle.Should().BeNull();
            }
        }
        finally
        {
            releaseLock.Set();
            await holder.ConfigureAwait(false);
        }
    }
}
