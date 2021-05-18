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
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += callback;
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
    }

    public class FastTimerElapsedEventArgs : EventArgs 
    {
        public FastTimerElapsedEventArgs()
        {
        }
    }

    public delegate void FastTimerElapsedEventHandler(object sender, FastTimerElapsedEventArgs e);

    public class FastTimer : ITimer, IDisposable
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
            : base()
        {
            Interval = interval;
        }

        /// <summary>
        /// Property that sets if the timer should restart when an event has been fired
        /// </summary>
        public bool AutoReset
        {
            get
            {
                return this.autoReset;
            }
            set
            {
                autoReset = value;
                if (this.isEnabled)
                {
                    Start();
                }
            }
        }

        /// <summary>
        /// Is this timer currently running?
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                this.isEnabled = value;
                if (this.isEnabled)
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
        public double Interval
        {
            get
            {
                return this.interval;
            }
            set
            {
                this.interval = value;
                if (this.isEnabled)
                {
                    Start();
                }
            }
        }

        /// <summary>
        /// The event handler we call when the timer is triggered
        /// </summary>
        public event FastTimerElapsedEventHandler Elapsed;

        /// <summary>
        /// Disposes resource held by this object
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the timer 
        /// </summary>
        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
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
        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;
            }
        }

        private void Runner()
        {
            double nextTrigger = 0f;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (isRunning)
            {
                WaitInterval(sw, ref nextTrigger);
                Elapsed?.Invoke(this, new FastTimerElapsedEventArgs());

                // restarting the timer in every hour to prevent precision problems
                if (sw.Elapsed.TotalHours >= 1d)
                {
                    sw.Restart();
                    nextTrigger = 0f;
                }
            }

            sw.Stop();
        }

        private void WaitInterval(Stopwatch sw, ref double nextTrigger)
        {
            var intervalLocal = interval;
            nextTrigger += intervalLocal;

            while (true)
            {
                var elapsed = sw.ElapsedTicks * tickFrequency;
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
                    var newInterval = interval;

                    if (intervalLocal != newInterval)
                    {
                        nextTrigger += newInterval - intervalLocal;
                        intervalLocal = newInterval;
                    }
                }

                if (!isRunning)
                    return;
            }
        }


#if false
private void ExecuteTimer()
        {
            int fallouts = 0;
            float nextTrigger = 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (isRunning)
            {
                float intervalLocal = interval;
                nextTrigger += intervalLocal;
                float elapsed;


                while (true)
                {
                    elapsed = ElapsedHiRes(stopwatch);
                    float diff = nextTrigger - elapsed;
                    if (diff <= 0f)
                        break;

                    if (diff < 1f)
                        Thread.SpinWait(10);
                    else if (diff < 10f)
                        Thread.SpinWait(100);
                    else
                    {
                        // By default Sleep(1) lasts about 15.5 ms (if not configured otherwise for the application by WinMM, for example)
                        // so not allowing sleeping under 16 ms. Not sleeping for more than 50 ms so interval changes/stopping can be detected.
                        if (diff >= 16f)
                            Thread.Sleep(diff >= 100f ? 50 : 1);
                        else
                        {
                            Thread.SpinWait(1000);
                            Thread.Sleep(0);
                        }

                        // if we have a larger time to wait, we check if the interval has been changed in the meantime
                        float newInterval = interval;

                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (intervalLocal != newInterval)
                        {
                            nextTrigger += newInterval - intervalLocal;
                            intervalLocal = newInterval;
                        }
                    }

                    if (!isRunning)
                        return;
                }


                float delay = elapsed - nextTrigger;
                if (delay >= ignoreElapsedThreshold)
                {
                    fallouts += 1;
                    continue;
                }

                Elapsed?.Invoke(this, new HiResTimerElapsedEventArgs(delay, fallouts));
                fallouts = 0;

                // restarting the timer in every hour to prevent precision problems
                if (stopwatch.Elapsed.TotalHours >= 1d)
                {
#if NET35
                    stopwatch.Reset();
                    stopwatch.Start();
#else
                    stopwatch.Restart();
#endif
                    nextTrigger = 0f;
                }
            }

            stopwatch.Stop();
        }
#endif

        private static readonly float tickFrequency = 1000f / Stopwatch.Frequency;

        private bool isEnabled = true;
        private bool autoReset = true;
        private double interval = 0.0f;
        public bool isRunning = false;
        public Thread thread;

    }
}