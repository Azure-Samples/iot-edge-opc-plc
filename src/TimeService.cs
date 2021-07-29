namespace OpcPlc
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Service returning <see cref="DateTime"/> values and <see cref="Timer"/> instances. Mocked in tests.
    /// </summary>
    public class TimeService
    {
        /// <summary>
        /// Create a new <see cref="Timer"/> instance with <see cref="Timer.Enabled"/> set to true
        /// and <see cref="Timer.AutoReset"/> set to true. The <see cref="Timer"/> will call the
        /// provided callback at regular intervals. This method is overridden in tests to return
        /// a mock object.
        /// </summary>
        /// <param name="callback">Event handler to call at regular intervals.</param>
        /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
        /// <returns>A <see cref="Timer"/>.</returns>
        public virtual ITimer NewTimer(
            ElapsedEventHandler callback,
            uint intervalInMilliseconds)
        {
            var timer = new TimerAdapter
            {
                Interval = intervalInMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += callback;
            return timer;
        }

        /// <summary>
        /// Create a new <see cref="FastTimer"/> instance with <see cref="FastTimer.Enabled"/> set to true
        /// and <see cref="FastTimer.AutoReset"/> set to true. The <see cref="FastTimer"/> will call the
        /// provided callback at regular intervals. This method is overridden in tests to return
        /// a mock object.
        /// </summary>
        /// <param name="callback">Event handler to call at regular intervals.</param>
        /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
        /// <returns>A <see cref="Timer"/>.</returns>
        public virtual ITimer NewFastTimer(
            FastTimerElapsedEventHandler callback,
            uint intervalInMilliseconds)
        {
            var timer = new FastTimer
            {
                Interval = intervalInMilliseconds,
                AutoReset = true
            };
            timer.Elapsed += callback;
            timer.Enabled = true;
            return timer;
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
        /// An adapter allowing the construction of <see cref="Timer"/> objects
        /// that explicitly implement the <see cref="ITimer"/> interface.
        /// The adapter itself must remain empty, add any required properties
        /// or methods from the <see cref="Timer"/> class into the
        /// <see cref="ITimer"/> interface.
        /// </summary>
        private class TimerAdapter : Timer, ITimer
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

    public class FastTimerElapsedEventArgs : EventArgs
    {
    }

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
        public double Interval { get; set; } = 0.0;

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
                var thread = new Thread(Runner)
                {
                    Priority = ThreadPriority.Highest
                };
                thread.Start();
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

            Stopwatch sw = new Stopwatch();
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
                var elapsed = sw.ElapsedTicks * TickFrequency;
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
        }

        private static readonly float TickFrequency = 1000f / Stopwatch.Frequency;

        private bool _isEnabled = false;
        private int _isRunning = 0;
    }
}