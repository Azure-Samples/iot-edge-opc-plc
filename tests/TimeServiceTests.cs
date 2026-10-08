namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class TimeServiceTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task DisposedTimers_AreRetiredDuringReconfigurationAsync(bool fast, bool close)
    {
        var time = new TimeService();
        try
        {
            for (int i = 0; i < 100; i++)
            {
                OpcPlc.ITimer timer = fast
                    ? time.NewFastTimer((_, _) => { }, 60000)
                    : time.NewTimer((_, _) => { }, 60000);
                if (close)
                {
                    timer.Close();
                }
                else
                {
                    timer.Dispose();
                }
                timer.Dispose();
                timer.Enabled.Should().BeFalse();
                Action restart = () => timer.Enabled = true;
                restart.Should().Throw<ObjectDisposedException>();
                RegisteredTimerCount(time).Should().Be(0);
            }
        }
        finally
        {
            await time.StopTimersAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StopTimers_AllowsInFlightCallbacksToFinishIntervalUpdatesAsync(bool fast)
    {
        var time = new TimeService();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var updated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        OpcPlc.ITimer timer = null;
        timer = fast
            ? time.NewFastTimer((_, _) => Callback(), 10)
            : time.NewTimer((_, _) => Callback(), 10);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            time.StopTimersAsync().IsCompleted.Should().BeFalse();
        }
        finally
        {
            release.Set();
            await time.StopTimersAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        await updated.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        void Callback()
        {
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(10));
            try
            {
                timer.Interval = 100;
                timer.AutoReset = true;
                updated.TrySetResult();
            }
            catch (Exception exception)
            {
                updated.TrySetException(exception);
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DisposedTimer_StaysTrackedUntilItsCallbackReturnsAsync(bool fast)
    {
        var time = new TimeService();
        using var release = new ManualResetEventSlim();
        using var dispatch = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object observedSender = null;
        OpcPlc.ITimer timer = null;
        timer = fast
            ? time.NewFastTimer((sender, _) => Callback(sender), 10)
            : time.NewTimer((sender, _) => Callback(sender), 10);
        dispatch.Set();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            observedSender.Should().BeSameAs(timer);
            RegisteredTimerCount(time).Should().Be(1);
            time.StopTimersAsync().IsCompleted.Should().BeFalse();
        }
        finally
        {
            release.Set();
            await time.StopTimersAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        RegisteredTimerCount(time).Should().Be(0);

        void Callback(object sender)
        {
            dispatch.Wait(TimeSpan.FromSeconds(5));
            observedSender = sender;
            ((OpcPlc.ITimer)sender).Dispose();
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(10));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StopTimers_ConcurrentDisposalStillDrainsCallbacksAsync(bool fast)
    {
        var time = new TimeService();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        OpcPlc.ITimer timer = fast
            ? time.NewFastTimer((_, _) => Callback(), 10)
            : time.NewTimer((_, _) => Callback(), 10);
        Task disposing = Task.CompletedTask;
        Task stopping = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            disposing = Task.Run(timer.Dispose);
            stopping = time.StopTimersAsync();
            stopping.IsCompleted.Should().BeFalse();
            RegisteredTimerCount(time).Should().Be(1);
        }
        finally
        {
            release.Set();
            await Task.WhenAll(disposing, stopping ?? time.StopTimersAsync())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        RegisteredTimerCount(time).Should().Be(0);

        void Callback()
        {
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(10));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StopTimers_WaitsForCallbacksAndSupportsRestartAsync(bool fast)
    {
        var time = new TimeService();
        for (int restart = 0; restart < 2; restart++)
        {
            time.StartTimers();
            using var release = new ManualResetEventSlim();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            OpcPlc.ITimer timer = fast
                ? time.NewFastTimer((_, _) => Callback(), 10)
                : time.NewTimer((_, _) => Callback(), 10);
            Task stopping = null;
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                stopping = time.StopTimersAsync();
                stopping.IsCompleted.Should().BeFalse("an executing callback still owns the address space");
                timer.Enabled.Should().BeFalse();
                time.StopTimersAsync().Should().BeSameAs(stopping);
                Action start = () => time.StartTimers();
                start.Should().Throw<InvalidOperationException>();
                Action create = () => time.NewTimer((_, _) => { }, 10);
                create.Should().Throw<InvalidOperationException>();
            }
            finally
            {
                release.Set();
                await (stopping ?? time.StopTimersAsync()).WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }

            void Callback()
            {
                entered.TrySetResult();
                release.Wait(TimeSpan.FromSeconds(10));
            }
        }

        await time.StopTimersAsync().ConfigureAwait(false);
    }

    private static int RegisteredTimerCount(TimeService time) =>
        ((ICollection)typeof(TimeService).GetField("_timers", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(time)).Count;
}
