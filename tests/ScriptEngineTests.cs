// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using OpcPlc.DeterministicAlarms;
using OpcPlc.DeterministicAlarms.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

[TestFixture]
public class ScriptEngineTests
{
    [Test]
    public async Task Replay_PreservesIntervalsAndEndsOnceAsync()
    {
        var time = new ScriptClock();
        var steps = new List<Step> { new() { SleepInSeconds = 3 }, new() };
        var received = new List<(Step Step, long Loop)>();
        var engine = new ScriptEngine(new Script
        {
            WaitUntilStartInSeconds = 2, RunningForSeconds = 20, Steps = steps
        }, (step, loop) => received.Add((step, loop)), time.Service.Object);
        await using var cleanup = engine.ConfigureAwait(false);
        time.Timer.Object.Interval.Should().Be(2000);

        time.Fire();
        time.Timer.Object.Interval.Should().Be(3000);
        time.Fire();
        time.Timer.Object.Interval.Should().Be(1);
        time.Fire();
        time.Fire();

        received.Should().Equal((steps[0], 1L), (steps[1], 1L), (null, 1L));
        time.Timer.Verify(timer => timer.Close(), Times.Once);
    }

    [Test]
    public async Task Replay_PreservesLoopAndStopTimeBoundaryAsync()
    {
        var time = new ScriptClock();
        var steps = new List<Step> { new() { SleepInSeconds = 1 }, new() { SleepInSeconds = 2 } };
        var received = new List<(Step Step, long Loop)>();
        var engine = new ScriptEngine(new Script
        {
            WaitUntilStartInSeconds = 2, RunningForSeconds = 5, IsScriptInRepeatingLoop = true, Steps = steps
        }, (step, loop) => received.Add((step, loop)), time.Service.Object);
        await using var cleanup = engine.ConfigureAwait(false);

        time.Fire();
        time.Fire();
        time.Fire();
        time.Now = time.Now.AddSeconds(7);
        time.Fire();
        time.Now = time.Now.AddSeconds(1);
        time.Fire();
        time.Fire();

        received.Should().Equal((steps[0], 1L), (steps[1], 1L), (steps[0], 2L), (steps[1], 2L), (null, 2L));
        time.Timer.Verify(timer => timer.Close(), Times.Once);
    }

    [Test]
    public async Task DisposeAsync_DrainsCallbackAndIgnoresQueuedTicksAsync()
    {
        var time = new ScriptClock();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int delivered = 0;
        var engine = new ScriptEngine(new Script
        {
            WaitUntilStartInSeconds = 1, RunningForSeconds = 60,
            Steps = [new Step { SleepInSeconds = 1 }, new Step { SleepInSeconds = 2 }]
        }, (step, loop) =>
        {
            Interlocked.Increment(ref delivered);
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Script callback was not released.");
            }
        }, time.Service.Object);
        await using var cleanup = engine.ConfigureAwait(false);
        Task tick = Task.Run(time.Fire);
        Task disposing = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            disposing = engine.DisposeAsync().AsTask();
            disposing.IsCompleted.Should().BeFalse();
            await Task.Run(time.Fire).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Volatile.Read(ref delivered).Should().Be(1);
        }
        finally
        {
            release.Set();
            await tick.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (disposing is not null)
            {
                await disposing.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        time.Fire();
        engine.Dispose();
        Volatile.Read(ref delivered).Should().Be(1);
        time.Timer.Verify(timer => timer.Close(), Times.Once);
    }

    [Test]
    public async Task Replay_OverlappingTicksAreSerializedAsync()
    {
        var time = new ScriptClock();
        var steps = new List<Step> { new() { SleepInSeconds = 1 }, new() { SleepInSeconds = 2 } };
        var received = new List<Step>();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var engine = new ScriptEngine(new Script
        {
            WaitUntilStartInSeconds = 1, RunningForSeconds = 60, Steps = steps
        }, (step, loop) =>
        {
            received.Add(step);
            if (received.Count == 1)
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("First script callback was not released.");
                }
            }
        }, time.Service.Object);
        await using var cleanup = engine.ConfigureAwait(false);
        Task first = Task.Run(time.Fire);
        Task second = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            second = Task.Run(() =>
            {
                secondStarted.SetResult();
                time.Fire();
            });
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            second.Wait(TimeSpan.FromMilliseconds(100)).Should().BeFalse();
        }
        finally
        {
            release.Set();
            await first.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (second is not null)
            {
                await second.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
        }

        received.Should().Equal(steps);
        time.Timer.Object.Interval.Should().Be(2000);
    }

    private sealed class ScriptClock
    {
        private ElapsedEventHandler _callback;
        public Mock<TimeService> Service { get; } = new();
        public Mock<OpcPlc.ITimer> Timer { get; } = new();
        public DateTime Now { get; set; } = new(2026, 1, 1);

        public ScriptClock()
        {
            Timer.SetupAllProperties();
            Timer.Setup(timer => timer.Close()).Callback(() => Timer.Object.Enabled = false);
            Service.Setup(time => time.Now()).Returns(() => Now);
            Service.Setup(time => time.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()))
                .Returns((ElapsedEventHandler callback, uint interval) =>
                {
                    _callback = callback;
                    Timer.Object.Interval = interval;
                    Timer.Object.Enabled = true;
                    return Timer.Object;
                });
        }

        public void Fire() => _callback(Timer.Object, null);
    }
}