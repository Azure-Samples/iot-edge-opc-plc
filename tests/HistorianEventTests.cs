namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class HistorianEventTests() : SubscriptionTestsBase(["--historian"])
{
    private static readonly QualifiedName[] Fields =
    [
        BrowseNames.EventId, BrowseNames.EventType, BrowseNames.SourceNode, BrowseNames.SourceName,
        BrowseNames.Time, BrowseNames.ReceiveTime, BrowseNames.Message, BrowseNames.Severity,
    ];

    [Test]
    public async Task NotifierAdvertisesEventHistory()
    {
        DataValue notifier = await ReadAttributeAsync(Attributes.EventNotifier).ConfigureAwait(false);

        notifier.Value.Should().Be((byte)(EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead));
    }

    [Test]
    public async Task HistoryCapabilitiesAdvertiseEventHistory()
    {
        NodeId capabilities = await FindNodeAsync(
            ObjectIds.Server_ServerCapabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.HistoryServerCapabilities)
            .ConfigureAwait(false);
        NodeId access = await FindNodeAsync(
            capabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.AccessHistoryEventsCapability).ConfigureAwait(false);
        NodeId pageLimit = await FindNodeAsync(
            capabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.MaxReturnEventValues).ConfigureAwait(false);

        (await ReadValueAsync<bool>(access).ConfigureAwait(false)).Should().BeTrue();
        (await ReadValueAsync<uint>(pageLimit).ConfigureAwait(false)).Should().Be(100);
    }

    [Test]
    public async Task ForwardReadReturnsSeededWindowAcrossPages()
    {
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.AddSeconds(-10000),
            EndTime = latest.AddSeconds(1),
            Filter = CreateFilter(),
        };

        List<Dictionary<string, object>> events = await ReadAllAsync(details).ConfigureAwait(false);

        events.Should().HaveCount(1001);
        events.Select(e => (DateTime)e["Time"]).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        ((DateTime)events[0]["Time"]).Should().Be(details.StartTime);
        ((DateTime)events[^1]["Time"]).Should().Be(latest);
        events.Should().OnlyContain(e =>
            (NodeId)e["EventType"] == ObjectTypeIds.BaseEventType
            && (NodeId)e["SourceNode"] == GetOpcPlcNodeId("HistorianEvents")
            && (string)e["SourceName"] == "HistorianEvents"
            && (ushort)e["Severity"] == (ushort)EventSeverity.Medium
            && (DateTime)e["ReceiveTime"] == (DateTime)e["Time"]);
        events.Select(e => (byte[])e["EventId"]).Select(Convert.ToBase64String).Should().OnlyHaveUniqueItems();
        AssertConsecutiveMessages(events);
    }

    [Test]
    public async Task CountReadReturnsNewestEventsFirst()
    {
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            EndTime = latest.AddSeconds(1),
            NumValuesPerNode = 150,
            Filter = CreateFilter(),
        };

        HistoryReadResult first = await ReadEventHistoryAsync(details).ConfigureAwait(false);
        Events(first).Should().HaveCount(100);
        first.ContinuationPoint.Should().NotBeNullOrEmpty();
        HistoryReadResult last = await ReadEventHistoryAsync(details, first.ContinuationPoint).ConfigureAwait(false);
        last.ContinuationPoint.Should().BeNullOrEmpty();

        List<Dictionary<string, object>> events = [.. Events(first), .. Events(last)];
        events.Should().HaveCount(150);
        events.Select(e => (DateTime)e["Time"]).Should().BeInDescendingOrder();
        ((DateTime)events[0]["Time"]).Should().Be(latest);
        events.Reverse();
        AssertConsecutiveMessages(events);
    }

    [TestCase(FilterOperator.GreaterThan, 0)]
    [TestCase(FilterOperator.Equals, 10)]
    public async Task WhereClauseFiltersEvents(FilterOperator filterOperator, int expected)
    {
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        EventFilter filter = CreateFilter();
        filter.WhereClause.Push(
            filterOperator,
            new SimpleAttributeOperand(ObjectTypeIds.BaseEventType, BrowseNames.Severity),
            new LiteralOperand((ushort)EventSeverity.Medium));
        var details = new ReadEventDetails { EndTime = latest.AddSeconds(1), NumValuesPerNode = 10, Filter = filter };

        HistoryReadResult result = await ReadEventHistoryAsync(details).ConfigureAwait(false);

        Events(result).Should().HaveCount(expected);
        result.StatusCode.Should().Be(expected == 0 ? (StatusCode)StatusCodes.GoodNoData : (StatusCode)StatusCodes.Good);
    }

    [Test]
    public async Task EmptyRangeReturnsGoodNoData()
    {
        var details = new ReadEventDetails
        {
            StartTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Filter = CreateFilter(),
        };

        HistoryReadResult result = await ReadEventHistoryAsync(details).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.GoodNoData);
        Events(result).Should().BeEmpty();
        result.ContinuationPoint.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task InvalidSelectClauseIsRejected()
    {
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var filter = new EventFilter();
        filter.SelectClauses.Add(new SimpleAttributeOperand
        {
            TypeDefinitionId = ObjectTypeIds.BaseEventType,
            BrowsePath = [BrowseNames.Message],
            AttributeId = 999,
        });
        var details = new ReadEventDetails
        {
            StartTime = latest.AddMinutes(-5),
            EndTime = latest,
            Filter = filter,
        };

        Func<Task> read = () => ReadEventHistoryAsync(details);

        (await read.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false))
            .Which.StatusCode.Should().Be(StatusCodes.BadEventFilterInvalid);
    }

    [Test]
    public async Task ReleasedContinuationCannotBeReused()
    {
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.AddMinutes(-5),
            EndTime = latest,
            NumValuesPerNode = 2,
            Filter = CreateFilter(),
        };
        HistoryReadResult first = await ReadEventHistoryAsync(details).ConfigureAwait(false);
        first.ContinuationPoint.Should().NotBeNullOrEmpty();

        HistoryReadResult released = await ReadEventHistoryAsync(details, first.ContinuationPoint, release: true)
            .ConfigureAwait(false);
        released.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        HistoryReadResult reused = await ReadEventHistoryAsync(details, first.ContinuationPoint).ConfigureAwait(false);
        reused.StatusCode.Should().Be((StatusCode)StatusCodes.BadContinuationPointInvalid);
    }

    [Test]
    public async Task SimulationReportsAndStoresEachNewEvent()
    {
        SetUpMonitoredItem(GetOpcPlcNodeId("HistorianEvents"), NodeClass.Object, Attributes.EventNotifier);
        await AddMonitoredItemAsync().ConfigureAwait(false);
        ClearEvents();

        Dictionary<string, object> live = FireTimersWithPeriodAndReceiveEvents(TimeSpan.FromSeconds(10), 1).Single();
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        HistoryReadResult result = await ReadEventHistoryAsync(new ReadEventDetails
        {
            EndTime = latest.AddSeconds(1),
            NumValuesPerNode = 1,
            Filter = CreateFilter(),
        }).ConfigureAwait(false);

        Dictionary<string, object> stored = Events(result).Single();
        live["/EventId"].Should().Be(System.Text.Encoding.UTF8.GetString((byte[])stored["EventId"]));
        ((LocalizedText)live["/Message"]).Text.Should().Be(((LocalizedText)stored["Message"]).Text);
        live["/SourceNode"].Should().Be(GetOpcPlcNodeId("HistorianEvents"));
    }

    [Test]
    public async Task SimulationEvictsOldestEvents()
    {
        FireTimersWithPeriod(TimeSpan.FromSeconds(10), 2001);
        DateTime latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.AddDays(-1),
            EndTime = latest.AddSeconds(1),
            Filter = CreateFilter(),
        };

        List<Dictionary<string, object>> events = await ReadAllAsync(details).ConfigureAwait(false);

        events.Should().HaveCount(2000);
        ((DateTime)events[0]["Time"]).Should().Be(latest.AddSeconds(-19990));
        ((DateTime)events[^1]["Time"]).Should().Be(latest);
        AssertConsecutiveMessages(events);
    }

    private static EventFilter CreateFilter()
    {
        var filter = new EventFilter();
        foreach (QualifiedName field in Fields)
        {
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, field);
        }

        return filter;
    }

    private static List<Dictionary<string, object>> Events(HistoryReadResult result)
    {
        return ((HistoryEvent)result.HistoryData.Body).Events
            .Select(fields => Fields.Zip(fields.EventFields).ToDictionary(p => p.First.Name, p => p.Second.Value))
            .ToList();
    }

    private static void AssertConsecutiveMessages(List<Dictionary<string, object>> events)
    {
        List<int> sequence = events
            .Select(e => int.Parse(((LocalizedText)e["Message"]).Text["Historian event ".Length..], System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        sequence.Zip(sequence.Skip(1)).Should().OnlyContain(pair => pair.Second == pair.First + 1);
    }

    private async Task<DateTime> LatestEventTimeAsync()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        return latest.SourceTimestamp;
    }

    private async Task<List<Dictionary<string, object>>> ReadAllAsync(ReadEventDetails details)
    {
        var events = new List<Dictionary<string, object>>();
        byte[] continuation = null;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadEventHistoryAsync(details, continuation).ConfigureAwait(false);
            StatusCode.IsGood(result.StatusCode).Should().BeTrue(result.StatusCode.ToString());
            List<Dictionary<string, object>> page = Events(result);
            page.Should().HaveCountLessThanOrEqualTo(100);
            events.AddRange(page);
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(30);
        }
        while (continuation is { Length: > 0 });

        return events;
    }

    private async Task<DataValue> ReadAttributeAsync(uint attributeId)
    {
        ReadResponse response = await Session.ReadAsync(null, 0, TimestampsToReturn.Neither,
            new ReadValueIdCollection
            {
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianEvents"), AttributeId = attributeId },
            }, CancellationToken.None).ConfigureAwait(false);
        return response.Results.Single();
    }

    private async Task<HistoryReadResult> ReadEventHistoryAsync(
        ReadEventDetails details, byte[] continuation = null, bool release = false)
    {
        HistoryReadResponse response = await Session.HistoryReadAsync(
            null, new ExtensionObject(details), TimestampsToReturn.Source, release,
            new HistoryReadValueIdCollection
            {
                new HistoryReadValueId { NodeId = GetOpcPlcNodeId("HistorianEvents"), ContinuationPoint = continuation },
            }, CancellationToken.None).ConfigureAwait(false);
        return response.Results.Single();
    }
}
