namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using OpcPlc.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstract base class for tests using OPC-UA Subscriptions.
/// </summary>
[TestFixture]
public abstract class SubscriptionTestsBase : SimulatorTestsBase
{
    /// <summary>
    /// The monitored item.
    /// </summary>
    protected MonitoredItem MonitoredItem;

    private Subscription _subscription;

    private readonly ConcurrentQueue<MonitoredItemNotificationEventArgs> _receivedEvents = new();

    protected SubscriptionTestsBase(string[] args = default) : base(args)
    {
    }

    /// <summary>
    /// Creates the subscription.
    /// </summary>
    [SetUp]
    public async Task CreateSubscription()
    {
        Utils.SetLogger(new TestLogger<SubscriptionTestsBase>(TestContext.Out, new SyslogFormatter(new SyslogFormatterOptions())));
        _subscription = Session.DefaultSubscription;
        Session.AddSubscription(_subscription);
        await _subscription.CreateAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the subscription.
    /// </summary>
    [TearDown]
    public async Task DeleteSubscription()
    {
        if (_subscription != null)
        {
            await _subscription.DeleteAsync(true).ConfigureAwait(false);
            await Session.RemoveSubscriptionAsync(_subscription).ConfigureAwait(false);
            _subscription = null;
        }
    }

    /// <summary>
    /// Create a <see cref="MonitoredItem"/> object configured to receive
    /// events that can be retrieved by the test class using <see cref="ReceiveEvents"/>.
    /// The object is not sent to the server at this point.
    /// Call <see cref="AddMonitoredItemAsync"/> to add the object to the subscription.
    /// </summary>
    /// <param name="startNodeId">The start node for the browse path that identifies the node to monitor..</param>
    /// <param name="nodeClass">The node class of the node being monitored (affects the type of filter available).</param>
    /// <param name="attributeId">The attribute to monitor.</param>
    protected void SetUpMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
    {
        MonitoredItem = new MonitoredItem(_subscription.DefaultItem)
        {
            DisplayName = startNodeId.Identifier.ToString(),
            StartNodeId = startNodeId,
            NodeClass = nodeClass,
            SamplingInterval = 0,
            AttributeId = attributeId,
            QueueSize = 1000,
        };

        MonitoredItem.Notification += MonitoredItem_Notification;
    }

    /// <summary>
    /// Add the <see cref="MonitoredItem"/> to the subscription.
    /// Derived tests should call this method after having configured the
    /// <see cref="MonitoredItem"/> definition, e.g. with filters.
    /// </summary>
    protected async Task AddMonitoredItemAsync()
    {
        _subscription.AddItem(MonitoredItem);
        await _subscription.ApplyChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Clear the buffer of received events.
    /// </summary>
    protected void ClearEvents()
    {
        _receivedEvents.Clear();
    }

    /// <summary>
    /// Wait until a given number of events have been received, and return them.
    /// </summary>
    /// <param name="expectedCount">Number of events to receive.</param>
    protected IEnumerable<MonitoredItemNotificationEventArgs> ReceiveEvents(int expectedCount)
    {
        var events = new List<MonitoredItemNotificationEventArgs>();

        var sw = Stopwatch.StartNew();
        do
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            while (_receivedEvents.TryDequeue(out var item))
            {
                events.Add(item);
            }
        } while (_receivedEvents.Count < expectedCount && sw.Elapsed < TimeSpan.FromSeconds(10));

        events.Should().HaveCount(expectedCount);

        return events;
    }

    protected IEnumerable<Dictionary<string, object>> ReceiveEventsAsDictionary(int expectedCount)
    {
        var events = ReceiveEvents(expectedCount);
        var values = events
            .Select(a => (EventFieldList)a.NotificationValue)
            .Select(EventFieldListToDictionary);

        return values;
    }

    protected IEnumerable<Dictionary<string, object>> FireTimersWithPeriodAndReceiveEvents(TimeSpan period, int expectedCount)
    {
        FireTimersWithPeriod(period, numberOfTimes: 1);
        return ReceiveEventsAsDictionary(expectedCount);
    }

    /// <summary>
    /// Wait until a given number of events have been received, and return them.
    /// </summary>
    /// <param name="expectedCount">Number of events to at most receive.</param>
    protected List<MonitoredItemNotificationEventArgs> ReceiveAtMostEvents(int expectedCount)
    {
        var sw = Stopwatch.StartNew();
        do
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
        } while (_receivedEvents.Count < expectedCount && sw.Elapsed < TimeSpan.FromSeconds(10));

        var events = _receivedEvents.Take(expectedCount).ToList();
        events.Should().HaveCount(expectedCount);

        return events;
    }

    /// <summary>
    /// Utility method to combine the retrieved field names (from the monitored item filter select clause)
    /// and the retrieved field values (from a received event) into a name/value dictionary.
    /// </summary>
    /// <param name="arg">A field list from a received event.</param>
    /// <returns>A dictionary of field name to field value.</returns>
    protected Dictionary<string, object> EventFieldListToDictionary(EventFieldList arg)
    {
        return
            ((EventFilter)MonitoredItem.Filter).SelectClauses // all retrieved fields for event
            .Zip(arg.EventFields) // values of retrieved fields
            .ToDictionary(
                p => SimpleAttributeOperand.Format(p.First.BrowsePath), // e.g. "/EventId"
                p => ConvertValue(SimpleAttributeOperand.Format(p.First.BrowsePath), p.Second.Value));
    }

    private static object ConvertValue(string browsePath, object value)
    {
        return value switch
        {
            byte[] byteArray => Encoding.UTF8.GetString(byteArray),
            ushort severity when browsePath == "/Severity" => Enum.Parse(typeof(EventSeverity), severity.ToString()),
            _ => value
        };
    }

    private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
    {
        _receivedEvents.Enqueue(e);
    }
}
