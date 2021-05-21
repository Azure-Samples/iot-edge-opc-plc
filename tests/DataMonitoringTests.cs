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
    public class DataMonitoringTests : SubscriptionTestsBase
    {
        // Set any cmd params needed for the plc server explicitly.        
        public DataMonitoringTests() : base(new[] { "--str=false" })
        {
        }

        [SetUp]
        public void CreateMonitoredItem()
        {
            var nodeId = GetOpcPlcNodeId("FastUInt1");
            nodeId.Should().NotBeNull();

            SetUpMonitoredItem(nodeId, NodeClass.Variable, Attributes.Value);

            AddMonitoredItem();
        }

        [Test]
        public void Monitoring_NotifiesValueUpdates()
        {
            // Arrange
            ClearEvents();

            // Act: collect events during 5 seconds
            // Value is updated every second
            FireTimersWithPeriod(1000, 5);

            // Assert
            var events = ReceiveEvents(6);
            var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
            var differences = values.Zip(values.Skip(1), (x, y) => y - x);
            differences.Should().AllBeEquivalentTo(1, $"elements of sequence {string.Join(",", values)} should be increasing by interval 1");
        }
    }
}