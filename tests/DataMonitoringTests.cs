namespace OpcPlc.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using static System.TimeSpan;

/// <summary>
/// Tests for OPC-UA Monitoring for Data changes.
/// </summary>
[TestFixture]
public class DataMonitoringTests : SubscriptionTestsBase
{
    // Set any cmd params needed for the plc server explicitly.
    public DataMonitoringTests() : base(Array.Empty<string>())
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        var nodeId = GetOpcPlcNodeId("FastUInt1");
        nodeId.Should().NotBeNull();

        SetUpMonitoredItem(nodeId, NodeClass.Variable, Attributes.Value);

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void Monitoring_NotifiesValueUpdates()
    {
        // Arrange
        ClearEvents();

        // Act: collect events during 5 seconds
        // Value is updated every second
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 5);

        // Assert
        var events = ReceiveEvents(6);
        var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
        var differences = values.Zip(values.Skip(1), (x, y) => y - x);
        differences.Should().AllBeEquivalentTo(1, $"elements of sequence {string.Join(",", values)} should be increasing by interval 1");
    }
}
