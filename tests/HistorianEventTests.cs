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
    private static readonly string[] Fields =
    [
        BrowseNames.EventId, BrowseNames.EventType, BrowseNames.SourceNode, BrowseNames.SourceName,
        BrowseNames.Time, BrowseNames.ReceiveTime, BrowseNames.Message, BrowseNames.Severity,
    ];

    [Test]
    public async Task NotifierAdvertisesEventHistory()
    {
        ReadResponse response = await Session.ReadAsync(null, 0, TimestampsToReturn.Neither,
            [
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianEvents"), AttributeId = Attributes.EventNotifier }
            ], CancellationToken.None).ConfigureAwait(false);

        response.Results[0].WrappedValue.CastTo<byte>()
            .Should().Be((byte)(EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead));
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
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.Add(TimeSpan.FromSeconds(-10000)),
            EndTime = latest.Add(TimeSpan.FromSeconds(1)),
            Filter = CreateFilter()
        };

        List<Dictionary<string, Variant>> events = await ReadAllAsync(details).ConfigureAwait(false);

        events.Should().HaveCount(1001);
        events.Select(e => e["Time"].GetDateTime()).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        events[0]["Time"].GetDateTime().Should().Be(details.StartTime);
        events[^1]["Time"].GetDateTime().Should().Be(latest);
        events.Should().OnlyContain(e =>
            e["EventType"].GetNodeId() == ObjectTypeIds.BaseEventType
            && e["SourceNode"].GetNodeId() == GetOpcPlcNodeId("HistorianEvents")
            && e["SourceName"].GetString() == "HistorianEvents"
            && e["Severity"].GetUInt16() == (ushort)EventSeverity.Medium
            && e["ReceiveTime"].GetDateTime() == e["Time"].GetDateTime());
        events.Select(e => Convert.ToBase64String(e["EventId"].GetByteString().ToArray()))
            .Should().OnlyHaveUniqueItems();
        AssertConsecutiveMessages(events);
    }

    [Test]
    public async Task CountReadReturnsNewestEventsFirst()
    {
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            EndTime = latest.Add(TimeSpan.FromSeconds(1)),
            NumValuesPerNode = 150,
            Filter = CreateFilter()
        };

        HistoryReadResult first = await ReadEventHistoryAsync(details).ConfigureAwait(false);
        Events(first).Should().HaveCount(100);
        first.ContinuationPoint.IsEmpty.Should().BeFalse();
        HistoryReadResult last = await ReadEventHistoryAsync(details, first.ContinuationPoint).ConfigureAwait(false);
        last.ContinuationPoint.IsEmpty.Should().BeTrue();

        List<Dictionary<string, Variant>> events = [.. Events(first), .. Events(last)];
        events.Should().HaveCount(150);
        events.Select(e => e["Time"].GetDateTime()).Should().BeInDescendingOrder();
        events[0]["Time"].GetDateTime().Should().Be(latest);
        events.Reverse();
        AssertConsecutiveMessages(events);
    }

    [TestCase(FilterOperator.GreaterThan, 0)]
    [TestCase(FilterOperator.Equals, 10)]
    public async Task WhereClauseFiltersEvents(FilterOperator filterOperator, int expected)
    {
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        EventFilter filter = CreateFilter();
        filter.WhereClause.Push(
            filterOperator,
            Variant.FromStructure(CreateOperand(BrowseNames.Severity)),
            Variant.From((ushort)EventSeverity.Medium));
        var details = new ReadEventDetails
        {
            EndTime = latest.Add(TimeSpan.FromSeconds(1)),
            NumValuesPerNode = 10,
            Filter = filter
        };

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
            Filter = CreateFilter()
        };

        HistoryReadResult result = await ReadEventHistoryAsync(details).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.GoodNoData);
        Events(result).Should().BeEmpty();
        result.ContinuationPoint.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task InvalidSelectClauseIsRejected()
    {
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        SimpleAttributeOperand invalid = CreateOperand(BrowseNames.Message);
        invalid.AttributeId = 999;
        var details = new ReadEventDetails
        {
            StartTime = latest.Add(TimeSpan.FromMinutes(-5)),
            EndTime = latest,
            Filter = new EventFilter { SelectClauses = new[] { invalid }.ToArrayOf() }
        };

        HistoryReadResult result = await ReadEventHistoryAsync(details).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadEventFilterInvalid);
        result.ContinuationPoint.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task ReleasedContinuationCannotBeReused()
    {
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.Add(TimeSpan.FromMinutes(-5)),
            EndTime = latest,
            NumValuesPerNode = 2,
            Filter = CreateFilter()
        };
        HistoryReadResult first = await ReadEventHistoryAsync(details).ConfigureAwait(false);
        first.ContinuationPoint.IsEmpty.Should().BeFalse();

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
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        HistoryReadResult result = await ReadEventHistoryAsync(new ReadEventDetails
        {
            EndTime = latest.Add(TimeSpan.FromSeconds(1)),
            NumValuesPerNode = 1,
            Filter = CreateFilter()
        }).ConfigureAwait(false);

        Dictionary<string, Variant> stored = Events(result).Single();
        live["/EventId"].Should().Be(System.Text.Encoding.UTF8.GetString(stored["EventId"].GetByteString().ToArray()));
        ((LocalizedText)live["/Message"]).Text.Should().Be(stored["Message"].GetLocalizedText().Text);
        live["/SourceNode"].Should().Be(GetOpcPlcNodeId("HistorianEvents"));
    }

    [Test]
    public async Task SimulationEvictsOldestEvents()
    {
        FireTimersWithPeriod(TimeSpan.FromSeconds(10), 2001);
        DateTimeUtc latest = await LatestEventTimeAsync().ConfigureAwait(false);
        var details = new ReadEventDetails
        {
            StartTime = latest.Add(TimeSpan.FromDays(-1)),
            EndTime = latest.Add(TimeSpan.FromSeconds(1)),
            Filter = CreateFilter()
        };

        List<Dictionary<string, Variant>> events = await ReadAllAsync(details).ConfigureAwait(false);

        events.Should().HaveCount(2000);
        events[0]["Time"].GetDateTime().Should().Be(latest.Add(TimeSpan.FromSeconds(-19990)));
        events[^1]["Time"].GetDateTime().Should().Be(latest);
        AssertConsecutiveMessages(events);
    }

    private static SimpleAttributeOperand CreateOperand(string field)
    {
        return new SimpleAttributeOperand
        {
            TypeDefinitionId = ObjectTypeIds.BaseEventType,
            BrowsePath = [new QualifiedName(field)],
            AttributeId = Attributes.Value
        };
    }

    private static EventFilter CreateFilter()
    {
        return new EventFilter { SelectClauses = Fields.Select(CreateOperand).ToArrayOf() };
    }

    private static List<Dictionary<string, Variant>> Events(HistoryReadResult result)
    {
        result.HistoryData.TryGetValue(out HistoryEvent history).Should().BeTrue();
        return history.Events.ToList()
            .Select(fields => Fields.Zip(fields.EventFields.ToArray()).ToDictionary(p => p.First, p => p.Second))
            .ToList();
    }

    private static void AssertConsecutiveMessages(List<Dictionary<string, Variant>> events)
    {
        List<int> sequence = events
            .Select(e => int.Parse(e["Message"].GetLocalizedText().Text["Historian event ".Length..],
                System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        sequence.Zip(sequence.Skip(1)).Should().OnlyContain(pair => pair.Second == pair.First + 1);
    }

    private async Task<DateTimeUtc> LatestEventTimeAsync()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        return latest.SourceTimestamp;
    }

    private async Task<List<Dictionary<string, Variant>>> ReadAllAsync(ReadEventDetails details)
    {
        var events = new List<Dictionary<string, Variant>>();
        ByteString continuation = default;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadEventHistoryAsync(details, continuation).ConfigureAwait(false);
            StatusCode.IsGood(result.StatusCode).Should().BeTrue(result.StatusCode.ToString());
            List<Dictionary<string, Variant>> page = Events(result);
            page.Should().HaveCountLessThanOrEqualTo(100);
            events.AddRange(page);
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(30);
        }
        while (!continuation.IsEmpty);

        return events;
    }

    private async Task<HistoryReadResult> ReadEventHistoryAsync(
        ReadEventDetails details, ByteString continuation = default, bool release = false)
    {
        HistoryReadResponse response = await Session.HistoryReadAsync(
            null, new ExtensionObject((IEncodeable)details), TimestampsToReturn.Source, release,
            [
                new HistoryReadValueId { NodeId = GetOpcPlcNodeId("HistorianEvents"), ContinuationPoint = continuation }
            ], CancellationToken.None).ConfigureAwait(false);
        return response.Results.ToList().Single();
    }
}
