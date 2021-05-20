namespace OpcPlc.Tests
{
    using System.Linq;

    using FluentAssertions;

    using NUnit.Framework;

    using Opc.Ua;

    /// <summary>
    /// Tests for OPC-UA Monitoring for Data changes.
    /// </summary>
    [TestFixture]
    public class DataRandomizationTests : SubscriptionTestsBase
    {
        public DataRandomizationTests() : base(new[] { "--str=true", "--sr=2" })
        { }

        [SetUp]
        public void CreateMonitoredItem()
        {
            var slowNodeId = GetOpcPlcNodeId("SlowUInt1");
            slowNodeId.Should().NotBeNull();

            SetUpMonitoredItem(slowNodeId, NodeClass.Variable, Attributes.Value);
            AddMonitoredItem();
        }

        [Test]
        public void Node_GeneratesRandomValues()
        {
            // Arrange
            ClearEvents();

            // Act: collect events during 10 seconds
            // Value is updated every 2 seconds
            FireTimersWithPeriod(2000, 5);

            // Assert
            var events = ReceiveEvents(6);
            var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
            var uniqueCount = values.Distinct().Count();

            // We are expecting random numbers to be unique mostly, not always.
            uniqueCount.Should().BeInRange(5, 6);
        }
    }
}