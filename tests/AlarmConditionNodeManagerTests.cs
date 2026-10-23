namespace OpcPlc.Tests;

using FluentAssertions;
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

    [Test]
    public async Task GetManagerHandle_NodeOfOtherNamespace_DoesNotWaitForAlarmLock()
    {
        var alarmNodeManager = _simulator.Server.AlarmNodeManager;
        using var lockHeld = new ManualResetEventSlim();
        using var releaseLock = new ManualResetEventSlim();
        Task holder = Task.Run(() =>
        {
            lock (alarmNodeManager.SyncRoot)
            {
                lockHeld.Set();
                releaseLock.Wait(TimeSpan.FromSeconds(30));
            }
        });

        try
        {
            lockHeld.Wait(LookupTimeout).Should().BeTrue();

            Task<object> lookup = Task.Run(() => alarmNodeManager.GetManagerHandleAsync(ObjectIds.Server).AsTask());
            Task completed = await Task.WhenAny(lookup, Task.Delay(LookupTimeout)).ConfigureAwait(false);

            completed.Should().BeSameAs(lookup, "a lookup of a foreign node must not wait for the alarm lock");
            (await lookup.ConfigureAwait(false)).Should().BeNull();
        }
        finally
        {
            releaseLock.Set();
            await holder.ConfigureAwait(false);
        }
    }
}
