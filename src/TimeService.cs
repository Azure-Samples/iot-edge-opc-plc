namespace OpcPlc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

/// <summary>
/// Service returning <see cref="DateTime"/> values and owned timer handles. Mocked in tests.
/// </summary>
public class TimeService
{
    private readonly object _timersLock = new();
    private readonly List<TimerRegistration> _timers = [];
    private Task _stopping;

    /// <summary>
    /// Create a new <see cref="Timer"/> instance with <see cref="Timer.Enabled"/> set to true
    /// and <see cref="Timer.AutoReset"/> set to true. The <see cref="Timer"/> will call the
    /// provided callback at regular intervals. This method is overridden in tests to return
    /// a mock object.
    /// </summary>
    /// <param name="callback">Event handler to call at regular intervals.</param>
    /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
    /// <returns>An owned timer handle, also supplied as the callback's sender.</returns>
    public virtual ITimer NewTimer(
        ElapsedEventHandler callback,
        uint intervalInMilliseconds)
    {
        lock (_timersLock)
        {
            ThrowIfStopping();
            var timer = new TimerAdapter
            {
                Interval = intervalInMilliseconds,
                AutoReset = true
            };

            TimerRegistration registration = Register(timer);
            timer.Elapsed += (_, args) => registration.Invoke(() => callback(registration, args));
            timer.Enabled = true;
            return registration;
        }
    }

    /// <summary>
    /// Create a new <see cref="FastTimer"/> instance with <see cref="FastTimer.Enabled"/> set to true
    /// and <see cref="FastTimer.AutoReset"/> set to true. The <see cref="FastTimer"/> will call the
    /// provided callback at regular intervals. This method is overridden in tests to return
    /// a mock object.
    /// </summary>
    /// <param name="callback">Event handler to call at regular intervals.</param>
    /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
    /// <returns>An owned timer handle, also supplied as the callback's sender.</returns>
    public virtual ITimer NewFastTimer(
        FastTimerElapsedEventHandler callback,
        uint intervalInMilliseconds)
    {
        lock (_timersLock)
        {
            ThrowIfStopping();
            var timer = new FastTimer
            {
                Interval = intervalInMilliseconds,
                AutoReset = true
            };

            TimerRegistration registration = Register(timer);
            timer.Elapsed += (_, args) => registration.Invoke(() => callback(registration, args));
            timer.Enabled = true;
            return registration;
        }
    }

    /// <summary>
    /// Returns the current time. Overridden in tests.
    /// </summary>
    /// <returns>The current time.</returns>
    public virtual DateTime Now() => DateTime.Now;

    /// <summary>
    /// Returns the current UTC time. Overridden in tests.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    public virtual DateTime UtcNow() => DateTime.UtcNow;

    /// <summary>
    /// Prevent new callbacks, await callbacks already in progress, and release owned timers.
    /// Call after stopping simulation and before disposing its address space.
    /// </summary>
    public Task StopTimersAsync()
    {
        TimerRegistration[] timers;
        TaskCompletionSource completion;
        lock (_timersLock)
        {
            if (_stopping is not null)
            {
                return _stopping;
            }
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopping = completion.Task;
            timers = _timers.ToArray();
        }
        _ = StopTimersAsync(timers, completion);
        return completion.Task;
    }

    private static async Task StopTimersAsync(TimerRegistration[] timers, TaskCompletionSource completion)
    {
        try
        {
            await Task.WhenAll(timers.Select(timer => timer.StopAsync())).ConfigureAwait(false);
            completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
    }

    /// <summary>
    /// Allow timers for a new server generation after the previous generation has drained.
    /// </summary>
    public void StartTimers()
    {
        lock (_timersLock)
        {
            if (_stopping is not null && !_stopping.IsCompletedSuccessfully)
            {
                throw new InvalidOperationException("Previous simulation timers have not drained successfully.");
            }
            _stopping = null;
        }
    }

    private void ThrowIfStopping()
    {
        if (_stopping is not null)
        {
            throw new InvalidOperationException("Cannot start a timer while simulation is stopped.");
        }
    }

    private TimerRegistration Register(ITimer timer)
    {
        var registration = new TimerRegistration(this, timer);
        _timers.Add(registration);
        return registration;
    }

    private void Unregister(TimerRegistration registration)
    {
        lock (_timersLock)
        {
            _timers.Remove(registration);
        }
    }

    private sealed class TimerRegistration(TimeService owner, ITimer timer) : ITimer
    {
        private readonly object _callbackLock = new();
        private readonly TaskCompletionSource _callbacksDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeCallbacks;
        private bool _stopping;
        private bool _disposeStarted;
        private bool _disposed;
        private bool _retired;

        public bool Enabled
        {
            get
            {
                lock (_callbackLock)
                {
                    return !_stopping && timer.Enabled;
                }
            }
            set
            {
                lock (_callbackLock)
                {
                    if (_stopping && !value)
                    {
                        return;
                    }
                    ObjectDisposedException.ThrowIf(_stopping, this);
                    timer.Enabled = value;
                }
            }
        }

        public bool AutoReset
        {
            get => timer.AutoReset;
            set
            {
                lock (_callbackLock)
                {
                    ObjectDisposedException.ThrowIf(_disposeStarted, this);
                    timer.AutoReset = value;
                }
            }
        }

        public double Interval
        {
            get => timer.Interval;
            set
            {
                lock (_callbackLock)
                {
                    ObjectDisposedException.ThrowIf(_disposeStarted, this);
                    timer.Interval = value;
                }
            }
        }

        public void Close() => Dispose();

        public void Invoke(Action callback)
        {
            lock (_callbackLock)
            {
                if (_stopping)
                {
                    return;
                }
                _activeCallbacks++;
            }
            try
            {
                callback();
            }
            finally
            {
                lock (_callbackLock)
                {
                    _activeCallbacks--;
                    if (_stopping && _activeCallbacks == 0)
                    {
                        _callbacksDrained.TrySetResult();
                    }
                }
                RetireIfDrained();
            }
        }

        public void Dispose()
        {
            lock (_callbackLock)
            {
                if (_disposeStarted)
                {
                    return;
                }
                _stopping = true;
                _disposeStarted = true;
                if (_activeCallbacks == 0)
                {
                    _callbacksDrained.TrySetResult();
                }
            }
            try
            {
                timer.Enabled = false;
                timer.Dispose();
                lock (_callbackLock)
                {
                    _disposed = true;
                }
                RetireIfDrained();
            }
            catch (Exception exception)
            {
                _drained.TrySetException(exception);
                throw;
            }
        }

        public async Task StopAsync()
        {
            lock (_callbackLock)
            {
                _stopping = true;
                timer.Enabled = false;
                if (_activeCallbacks == 0)
                {
                    _callbacksDrained.TrySetResult();
                }
            }
            await _callbacksDrained.Task.ConfigureAwait(false);
            Dispose();
            await _drained.Task.ConfigureAwait(false);
        }

        private void RetireIfDrained()
        {
            lock (_callbackLock)
            {
                if (!_disposed || _activeCallbacks != 0 || _retired)
                {
                    return;
                }
                _retired = true;
            }
            owner.Unregister(this);
            _drained.TrySetResult();
        }
    }

    /// <summary>
    /// An adapter allowing the construction of <see cref="Timer"/> objects
    /// that explicitly implement the <see cref="ITimer"/> interface.
    /// The adapter itself must remain empty, add any required properties
    /// or methods from the <see cref="Timer"/> class into the
    /// <see cref="ITimer"/> interface.
    /// </summary>
    private sealed class TimerAdapter : Timer, ITimer
    {
    }
}

/// <summary>
/// An interface expressing the methods from the <see cref="Timer"/> class
/// used in this project. Used for mocking.
/// Add methods and properties from <see cref="Timer"/> to this interface as needed.
/// </summary>
public interface ITimer : IDisposable
{
    bool Enabled { get; set; }

    bool AutoReset { get; set; }

    double Interval { get; set; }

    void Close();
}

public class FastTimerElapsedEventArgs : EventArgs;

public delegate void FastTimerElapsedEventHandler(object sender, FastTimerElapsedEventArgs e);

public class FastTimer : ITimer
{
    /// <summary>
    /// Initializes a new instance of the FastTimer class and sets all properties to their default values
    /// </summary>
    public FastTimer()
    {
    }

    /// <summary>
    /// Initializes a new instance of the FastTimer class and sets all properties to their default values except interval
    /// </summary>
    /// <param name="interval"></param>
    public FastTimer(double interval)
    {
        Interval = interval;
    }

    /// <summary>
    /// Property that sets if the timer should restart when an event has been fired
    /// </summary>
    public bool AutoReset { get; set; } = true;

    /// <summary>
    /// Is this timer currently running?
    /// </summary>
    public bool Enabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (_isEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    /// <summary>
    /// The current interval between triggering of this timer
    /// </summary>
    public double Interval { get; set; }

    /// <summary>
    /// The event handler we call when the timer is triggered
    /// </summary>
    public event FastTimerElapsedEventHandler Elapsed;

    public void Close()
    {
        Enabled = false;
    }

    /// <summary>
    /// Starts the timer
    /// </summary>
    private void Start()
    {
        var isRunning = Interlocked.Exchange(ref _isRunning, 1);
        if (isRunning == 0)
        {
            _thread = new Thread(Runner)
            {
                Priority = ThreadPriority.Highest
            };
            _thread.Start();
        }
    }

    /// <summary>
    /// Stops the timer
    /// </summary>
    private void Stop()
    {
        Interlocked.Exchange(ref _isRunning, 0);
    }

    private void Runner()
    {
        double nextTrigger = 0f;

        var sw = new Stopwatch();
        sw.Start();

        while (_isRunning == 1)
        {
            WaitInterval(sw, ref nextTrigger);
            if (_isRunning == 1)
            {
                Elapsed?.Invoke(this, new FastTimerElapsedEventArgs());

                if (!AutoReset)
                {
                    Interlocked.Exchange(ref _isRunning, 0);
                    Enabled = false;
                    break;
                }

                // restarting the timer in every hour to prevent precision problems
                if (sw.Elapsed.TotalHours >= 1d)
                {
                    sw.Restart();
                    nextTrigger = 0f;
                }
            }
        }

        sw.Stop();
    }

    private void WaitInterval(Stopwatch sw, ref double nextTrigger)
    {
        var intervalLocal = Interval;
        nextTrigger += intervalLocal;

        while (true)
        {
            var elapsed = sw.ElapsedTicks * _tickFrequency;
            var diff = nextTrigger - elapsed;
            if (diff <= 0f)
                break;

            if (diff < 1f)
                Thread.SpinWait(10);
            else if (diff < 10f)
                Thread.SpinWait(100);
            else
            {
                if (diff >= 16f)
                    Thread.Sleep(diff >= 100f ? 50 : 1);
                else
                {
                    Thread.SpinWait(1000);
                    Thread.Sleep(0);
                }

                // if we have a larger time to wait, we check if the interval has been changed in the meantime
                var newInterval = Interval;

                if (intervalLocal != newInterval)
                {
                    nextTrigger += newInterval - intervalLocal;
                    intervalLocal = newInterval;
                }
            }

            if (_isRunning == 0)
                return;
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _isRunning, 0);
        if (_thread is not null && _thread != Thread.CurrentThread)
        {
            _thread.Join();
        }
    }

    private static readonly float _tickFrequency = 1000f / Stopwatch.Frequency;

    private bool _isEnabled;
    private int _isRunning;
    private Thread _thread;
}
