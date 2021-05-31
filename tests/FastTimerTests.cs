namespace OpcPlc.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class FastTimerTests
    {

        [Test]
        public void FastTimer_ShouldFire_20TimesPerSecond()
        {
            // Arrange
            var fastTimer = new FastTimer(50)
            {
                Enabled = false
            };
            fastTimer.Elapsed += Callback;
            _callbacks.Clear();

            // Act
            fastTimer.Enabled = true;
            Thread.Sleep(2000);
            fastTimer.Enabled = false;

            // Assert (let's have some wiggle room here for timing issues)
            _callbacks.Count.Should().BeInRange(35, 45);
        }

        [Test]
        public void FastTimer_ShouldFire_100TimesPerSecond()
        {
            // Arrange
            var fastTimer = new FastTimer(10)
            {
                Enabled = false
            };
            fastTimer.Elapsed += Callback;
            _callbacks.Clear();

            // Act
            fastTimer.Enabled = true;
            Thread.Sleep(2000);
            fastTimer.Enabled = false;

            // Assert (let's have some wiggle room here for timing issues)
            _callbacks.Count.Should().BeInRange(185, 215);
        }

        [Test]
        public void FastTimerNotEnabled_ShouldFire_0TimesPerSecond()
        {
            // Arrange
            var fastTimer = new FastTimer(30)
            {
                Enabled = false
            };
            fastTimer.Elapsed += Callback;
            _callbacks.Clear();

            // Act
            Thread.Sleep(2000);

            // Assert 
            _callbacks.Count.Should().Be(0);
        }

        [Test]
        public void FastTimerWithNoCallback_ShouldFire_0TimesPerSecond()
        {
            // Arrange
            var fastTimer = new FastTimer(30)
            {
                Enabled = false
            };
            _callbacks.Clear();

            // Act
            fastTimer.Enabled = true;
            Thread.Sleep(2000);
            fastTimer.Enabled = false;

            // Assert 
            _callbacks.Count.Should().Be(0);
        }

        [Test]
        public void FastTimerWithNoAutoReset_ShouldFire_Once()
        {
            // Arrange
            var fastTimer = new FastTimer(30)
            {
                Enabled = false,
                AutoReset = false
            };
            fastTimer.Elapsed += Callback;
            _callbacks.Clear();

            // Act
            fastTimer.Enabled = true;
            Thread.Sleep(2000);
            fastTimer.Enabled = false;

            // Assert 
            _callbacks.Count.Should().Be(1);
        }


        private readonly List<DateTime> _callbacks = new List<DateTime>();

        private void Callback(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            _callbacks.Add(DateTime.UtcNow);
        }
    }
}