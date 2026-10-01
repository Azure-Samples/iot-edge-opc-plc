namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client.Subscriptions;
using Opc.Ua.Client.Subscriptions.MonitoredItems;
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
public abstract class SubscriptionTestsBase : SimulatorTestsBase, ISubscriptionNotificationHandler
{
    /// <summary>
    /// The monitored item.
    /// </summary>
    protected MonitoredItemOptions MonitoredItem;

    private ISubscription _subscription;

    private readonly ConcurrentQueue<IEncodeable> _receivedEvents = new();

    protected SubscriptionTestsBase(string[] args = default) : base(args)
    {
    }

    /// <summary>
    /// Creates the subscription.
    /// </summary>
    [SetUp]
    public void CreateSubscription()
    {
        Session.TryGetSubscriptionManager(out var manager).Should().BeTrue("managed sessions use the V2 engine");
        manager.PoolNotifications.Should().BeFalse("these tests retain notification payloads after callbacks return");
        _receivedEvents.Clear();
        _subscription = manager.Add(this, new OptionsMonitor<SubscriptionOptions>(new()
        {
            PublishingEnabled = true,
            PublishingInterval = TimeSpan.FromSeconds(1),
            KeepAliveCount = 10,
            LifetimeCount = 1000,
            Priority = 255,
            MinLifetimeInterval = MinimumSubscriptionLifetime
        }));
    }

    /// <summary>
    /// Deletes the subscription.
    /// </summary>
    [TearDown]
    public async Task DeleteSubscriptionAsync()
    {
        if (_subscription is not null)
        {
            await _subscription.DisposeAsync().ConfigureAwait(false);
            _subscription = null;
        }
    }

    /// <summary>
    /// Create <see cref="MonitoredItemOptions"/> configured to receive
    /// events that can be retrieved by the test class using <see cref="ReceiveEvents"/>.
    /// The object is not sent to the server at this point.
    /// Call <see cref="AddMonitoredItemAsync"/> to add the object to the subscription.
    /// </summary>
    /// <param name="startNodeId">The start node for the browse path that identifies the node to monitor..</param>
    /// <param name="nodeClass">The node class of the node being monitored (affects the type of filter available).</param>
    /// <param name="attributeId">The attribute to monitor.</param>
    protected void SetUpMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
    {
        MonitoredItem = new MonitoredItemOptions
        {
            StartNodeId = startNodeId,
            SamplingInterval = TimeSpan.Zero,
            AttributeId = attributeId,
            QueueSize = 1000,
            Filter = nodeClass == NodeClass.Object ? new EventFilter
            {
                SelectClauses = new[]
                {
                    BrowseNames.EventId, BrowseNames.EventType, BrowseNames.SourceNode, BrowseNames.SourceName,
                    BrowseNames.Time, BrowseNames.ReceiveTime, BrowseNames.LocalTime, BrowseNames.Message, BrowseNames.Severity
                }.Select(name => new SimpleAttributeOperand
                {
                    TypeDefinitionId = ObjectTypeIds.BaseEventType,
                    BrowsePath = [new QualifiedName(name)],
                    AttributeId = Attributes.Value
                }).ToArrayOf()
            } : null
        };
    }

    /// <summary>
    /// Add the configured monitored item to the subscription.
    /// Derived tests should call this method after having configured the
    /// <see cref="MonitoredItem"/> definition, e.g. with filters.
    /// </summary>
    protected async Task AddMonitoredItemAsync()
    {
        _subscription.MonitoredItems.TryAdd("TestItem",
            new OptionsMonitor<MonitoredItemOptions>(MonitoredItem),
            out var item).Should().BeTrue();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!item.Created)
        {
            ServiceResult.IsBad(item.Error).Should().BeFalse($"monitored item creation failed: {item.Error}");
            await Task.Delay(25, deadline.Token).ConfigureAwait(false);
        }
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
    protected IEnumerable<IEncodeable> ReceiveEvents(int expectedCount)
    {
        var events = new List<IEncodeable>();

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
            .Cast<EventFieldList>()
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
    protected List<IEncodeable> ReceiveAtMostEvents(int expectedCount)
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
            ((EventFilter)MonitoredItem.Filter).SelectClauses.ToArray() // all retrieved fields for event
            .Zip(arg.EventFields.ToArray()) // values of retrieved fields
            .ToDictionary(
                p => SimpleAttributeOperand.Format(p.First.BrowsePath), // e.g. "/EventId"
                p => ConvertValue(SimpleAttributeOperand.Format(p.First.BrowsePath), p.Second));
    }

    private static object ConvertValue(string browsePath, Variant value)
    {
        if (value.IsNull)
        {
            return null;
        }
        return (value.TypeInfo.BuiltInType, value.TypeInfo.IsScalar) switch
        {
            (BuiltInType.ByteString, true) => Encoding.UTF8.GetString(value.GetByteString().Span),
            (BuiltInType.DateTime, true) => value.CastTo<DateTime>(),
            (BuiltInType.UInt16, true) when browsePath == "/Severity" => (EventSeverity)value.GetUInt16(),
            _ => value.AsBoxedObject()
        };
    }

    ValueTask ISubscriptionNotificationHandler.OnDataChangeNotificationAsync(ISubscription subscription,
        uint sequenceNumber, DateTime publishTime, ReadOnlyMemory<DataValueChange> notification,
        PublishState publishStateMask, IReadOnlyList<string> stringTable)
    {
        foreach (var change in notification.Span)
        {
            _receivedEvents.Enqueue(new MonitoredItemNotification { Value = change.Value });
        }
        return ValueTask.CompletedTask;
    }

    ValueTask ISubscriptionNotificationHandler.OnEventDataNotificationAsync(ISubscription subscription,
        uint sequenceNumber, DateTime publishTime, ReadOnlyMemory<EventNotification> notification,
        PublishState publishStateMask, IReadOnlyList<string> stringTable)
    {
        foreach (var change in notification.Span)
        {
            _receivedEvents.Enqueue(new EventFieldList { EventFields = change.Fields });
        }
        return ValueTask.CompletedTask;
    }

    ValueTask ISubscriptionNotificationHandler.OnKeepAliveNotificationAsync(ISubscription subscription,
        uint sequenceNumber, DateTime publishTime, PublishState publishStateMask) => ValueTask.CompletedTask;

    ValueTask ISubscriptionNotificationHandler.OnSubscriptionStateChangedAsync(ISubscription subscription,
        SubscriptionState state, PublishState publishStateMask,
        CancellationToken ct) => ValueTask.CompletedTask;
}
