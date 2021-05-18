namespace OpcPlc
{
    using System;
    using System.Timers;

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
}