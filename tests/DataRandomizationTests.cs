namespace OpcPlc.Tests;

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
public class DataRandomizationTests : SubscriptionTestsBase
{
    // Set any cmd params needed for the plc server explicitly
    public DataRandomizationTests() : base(["--str=true"])
    {
    }

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        var slowNodeId = GetOpcPlcNodeId("SlowUInt1");
        slowNodeId.Should().NotBeNull();

        SetUpMonitoredItem(slowNodeId, NodeClass.Variable, Attributes.Value);
        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [Test]
    public void Node_GeneratesRandomValues()
    {
        // Arrange
        ClearEvents();

        // Act: collect events during 50 seconds
        // Value is updated every 10 seconds
        FireTimersWithPeriod(FromSeconds(10), numberOfTimes: 5);

        // Assert
        var events = ReceiveEvents(6);
        var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
        var differences = values.Zip(values.Skip(1), (x, y) => y - x);
        var differencesOfDifferences = differences.Zip(differences.Skip(1), (x, y) => y - x);

        var uniqueCount = differencesOfDifferences.Distinct().Count();

        // We are expecting random numbers to be unique mostly, not always so the differences between numbers should also be unique mostly.
        uniqueCount.Should().BeInRange(3, 4);
    }
}
