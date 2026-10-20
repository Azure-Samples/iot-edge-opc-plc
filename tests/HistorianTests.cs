namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class HistorianTests() : SimulatorTestsBase(["--historian"])
{
    [Test]
    public async Task HistorianNodeAdvertisesHistoryRead()
    {
        ReadResponse response = await Session.ReadAsync(null, 0, TimestampsToReturn.Neither,
            [
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.AccessLevel },
                new ReadValueId
                {
                    NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.UserAccessLevel
                },
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.Historizing }
            ], CancellationToken.None).ConfigureAwait(false);

        response.Results[0].WrappedValue.CastTo<byte>()
            .Should().Be((byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));
        response.Results[1].WrappedValue.CastTo<byte>()
            .Should().Be((byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));
        response.Results[2].WrappedValue.CastTo<bool>().Should().BeTrue();
    }

    [Test]
    public async Task RawHistoryReturnsSeededSamples()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-10000)),
            EndTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(1)),
            NumValuesPerNode = 73
        };
        var allValues = new List<DataValue>();
        ByteString continuation = default;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details, continuation)
                .ConfigureAwait(false);
            StatusCode.IsGood(result.StatusCode).Should().BeTrue(result.StatusCode.ToString());
            ArrayOf<DataValue> values = GetHistoryData(result);
            values.ToList().Should().HaveCountLessThanOrEqualTo(73);
            allValues.AddRange(values.ToList());
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(30);
        }
        while (!continuation.IsEmpty);

        allValues.Should().HaveCount(1001);
        allValues.Select(value => value.SourceTimestamp).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        allValues.Should().OnlyContain(value => value.WrappedValue.TypeInfo.BuiltInType == BuiltInType.Int32
            && value.StatusCode == StatusCodes.Good && value.ServerTimestamp == DateTimeUtc.MinValue);
        allValues[0].SourceTimestamp.Should().Be(details.StartTime);
        allValues[^1].SourceTimestamp.Should().Be(latest.SourceTimestamp);
    }

    [TestCase(false, false, 3)]
    [TestCase(false, true, 4)]
    [TestCase(true, false, 3)]
    [TestCase(true, true, 4)]
    public async Task RawHistoryHonorsRangeDirectionAndBounds(bool reverse, bool bounds, int count)
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = bounds,
            StartTime = reverse ? latest.SourceTimestamp : latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-30)),
            EndTime = reverse ? latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-30)) : latest.SourceTimestamp
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ArrayOf<DataValue> values = GetHistoryData(result);

        values.ToList().Should().HaveCount(count);
        values[0].SourceTimestamp.Should().Be(details.StartTime);
        values[^1].SourceTimestamp.Should().Be(bounds
            ? details.EndTime : details.EndTime.Add(TimeSpan.FromSeconds(reverse ? 10 : -10)));
        result.ContinuationPoint.IsEmpty.Should().BeTrue();
    }

    [TestCase(false, 1)]
    [TestCase(true, 2)]
    public async Task EqualTimestampsReturnExactValueAndOptionalNextBound(bool bounds, int count)
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = bounds,
            StartTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-10)),
            EndTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-10))
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ArrayOf<DataValue> values = GetHistoryData(result);

        values.ToList().Should().HaveCount(count);
        values[0].SourceTimestamp.Should().Be(details.StartTime);
        if (bounds)
        {
            values[1].SourceTimestamp.Should().Be(latest.SourceTimestamp);
        }
    }

    [Test]
    public async Task EmptyRangeReturnsGoodNoData()
    {
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.GoodNoData);
        GetHistoryData(result).ToList().Should().BeEmpty();
        result.ContinuationPoint.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task ReleasedContinuationCannotBeReused()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.Add(TimeSpan.FromMinutes(-5)),
            EndTime = latest.SourceTimestamp,
            NumValuesPerNode = 2
        };
        HistoryReadResult first = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        first.ContinuationPoint.IsEmpty.Should().BeFalse();
        HistoryReadResult released = await ReadHistoryAsync(
            "HistorianInt32", details, first.ContinuationPoint, release: true).ConfigureAwait(false);
        released.StatusCode.Should().Be((StatusCode)StatusCodes.Good);

        HistoryReadResult reused = await ReadHistoryAsync(
            "HistorianInt32", details, first.ContinuationPoint).ConfigureAwait(false);
        reused.StatusCode.Should().Be((StatusCode)StatusCodes.BadContinuationPointInvalid);
    }

    [Test]
    public async Task HistorianFolderContainsOnlyInt32Variable()
    {
        BrowseResponse response = await Session.BrowseAsync(null, null, 0,
            [
                new BrowseDescription
                {
                    NodeId = GetOpcPlcNodeId("Historian"),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All
                }
            ], CancellationToken.None).ConfigureAwait(false);

        response.Results.ToList().Single().References.ToList().Select(reference => reference.BrowseName.Name)
            .Should().ContainSingle().Which.Should().Be("HistorianInt32");
    }

    [Test]
    public async Task HistoryCapabilitiesAdvertiseRawReadsOnly()
    {
        NodeId capabilities = await FindNodeAsync(
            ObjectIds.Server_ServerCapabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.HistoryServerCapabilities)
            .ConfigureAwait(false);
        NodeId access = await FindNodeAsync(
            capabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.AccessHistoryDataCapability).ConfigureAwait(false);
        NodeId pageLimit = await FindNodeAsync(
            capabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.MaxReturnDataValues).ConfigureAwait(false);
        NodeId insert = await FindNodeAsync(
            capabilities, Opc.Ua.Namespaces.OpcUa, BrowseNames.InsertDataCapability).ConfigureAwait(false);

        (await ReadValueAsync<bool>(access).ConfigureAwait(false)).Should().BeTrue();
        (await ReadValueAsync<uint>(pageLimit).ConfigureAwait(false)).Should().Be(100);
        (await ReadValueAsync<bool>(insert).ConfigureAwait(false)).Should().BeFalse();
    }

    [TestCase(0u)]
    [TestCase(1000u)]
    public async Task ServerCapsPagesAtOneHundredValues(uint requestedCount)
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(requestedCount).ConfigureAwait(false);
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);

        GetHistoryData(result).ToList().Should().HaveCount(100);
        result.ContinuationPoint.IsEmpty.Should().BeFalse();
        await ReadHistoryAsync("HistorianInt32", details, result.ContinuationPoint, release: true)
            .ConfigureAwait(false);
    }

    [Test]
    public async Task HistoricalConfigurationExposesSamplingIntervalAndArchiveStart()
    {
        NodeId configuration = await FindNodeAsync(GetOpcPlcNodeId("HistorianInt32"),
            Opc.Ua.Namespaces.OpcUa, BrowseNames.HAConfiguration).ConfigureAwait(false);
        NodeId interval = await FindNodeAsync(configuration,
            Opc.Ua.Namespaces.OpcUa, BrowseNames.MinTimeInterval).ConfigureAwait(false);
        NodeId archiveStart = await FindNodeAsync(configuration,
            Opc.Ua.Namespaces.OpcUa, BrowseNames.StartOfArchive).ConfigureAwait(false);

        (await ReadValueAsync<double>(interval).ConfigureAwait(false)).Should().Be(10000);
        DateTimeUtc firstTimestamp = await ReadValueAsync<DateTimeUtc>(archiveStart).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = firstTimestamp,
            EndTime = firstTimestamp
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        GetHistoryData(result).ToList().Should().ContainSingle()
            .Which.SourceTimestamp.Should().Be(firstTimestamp);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OneSidedRequestsStopAfterRequestedCount(bool reverse)
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(101).ConfigureAwait(false);
        if (reverse)
        {
            details.StartTime = DateTime.MinValue;
        }
        else
        {
            details.EndTime = DateTime.MinValue;
        }

        HistoryReadResult first = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ArrayOf<DataValue> firstValues = GetHistoryData(first);
        firstValues.ToList().Should().HaveCount(100);
        first.ContinuationPoint.IsEmpty.Should().BeFalse();
        HistoryReadResult last = await ReadHistoryAsync(
            "HistorianInt32", details, first.ContinuationPoint).ConfigureAwait(false);
        ArrayOf<DataValue> lastValues = GetHistoryData(last);
        lastValues.ToList().Should().ContainSingle();
        lastValues[0].SourceTimestamp.Should()
            .Be(firstValues[0].SourceTimestamp.Add(TimeSpan.FromSeconds(reverse ? -1000 : 1000)));
        last.ContinuationPoint.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task DoubleHistorianNodeIsAbsent()
    {
        DataValue result = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianDouble")).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadNodeIdUnknown);
    }

    [Test]
    public async Task ContinuationCannotBeUsedByAnotherSession()
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(2).ConfigureAwait(false);
        HistoryReadResult first = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ISession otherSession = await CreateSessionAsync(nameof(ContinuationCannotBeUsedByAnotherSession))
            .ConfigureAwait(false);
        try
        {
            HistoryReadResult wrongSession = await ReadHistoryAsync(
                "HistorianInt32", details, first.ContinuationPoint, session: otherSession).ConfigureAwait(false);
            wrongSession.StatusCode.Should().Be((StatusCode)StatusCodes.BadContinuationPointInvalid);
            HistoryReadResult owner = await ReadHistoryAsync(
                "HistorianInt32", details, first.ContinuationPoint, release: true).ConfigureAwait(false);
            owner.StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        }
        finally
        {
            try
            {
                await otherSession.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                await ((IAsyncDisposable)otherSession).DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [Test]
    public async Task ModifiedHistoryIsRejected()
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(2).ConfigureAwait(false);
        details.IsReadModified = true;
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadHistoryOperationUnsupported);
    }

    [TestCase(TimestampsToReturn.Source)]
    [TestCase(TimestampsToReturn.Server)]
    [TestCase(TimestampsToReturn.Both)]
    public async Task TimestampSelectionDoesNotMutateArchive(TimestampsToReturn timestamps)
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(0).ConfigureAwait(false);
        details.StartTime = details.EndTime.Add(TimeSpan.FromSeconds(-1));
        HistoryReadResult result = await ReadHistoryAsync(
            "HistorianInt32", details, timestamps: timestamps).ConfigureAwait(false);
        DataValue value = GetHistoryData(result).ToList().Should().ContainSingle().Subject;
        value.SourceTimestamp.Should().Be(timestamps is TimestampsToReturn.Source or TimestampsToReturn.Both
            ? details.EndTime.Add(TimeSpan.FromSeconds(-1)) : DateTimeUtc.MinValue);
        value.ServerTimestamp.Should().Be(timestamps is TimestampsToReturn.Server or TimestampsToReturn.Both
            ? details.EndTime.Add(TimeSpan.FromSeconds(-1)) : DateTimeUtc.MinValue);

        HistoryReadResult reread = await ReadHistoryAsync(
            "HistorianInt32", details, timestamps: TimestampsToReturn.Both).ConfigureAwait(false);
        DataValue archived = GetHistoryData(reread).ToList().Should().ContainSingle().Subject;
        archived.SourceTimestamp.Should().Be(details.EndTime.Add(TimeSpan.FromSeconds(-1)));
        archived.ServerTimestamp.Should().Be(archived.SourceTimestamp);
    }

    [Test]
    public async Task RawHistoryRejectsNeitherTimestampSelection()
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(2).ConfigureAwait(false);
        Func<Task> read = () => ReadHistoryAsync("HistorianInt32", details, timestamps: TimestampsToReturn.Neither);
        var failure = await read.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
        failure.Which.StatusCode.Should().Be((StatusCode)StatusCodes.BadTimestampsToReturnInvalid);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task MissingBoundsUseExplicitBadValues(bool reverse)
    {
        DateTimeUtc first = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTimeUtc last = first.Add(TimeSpan.FromSeconds(10));
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = true,
            StartTime = reverse ? last : first,
            EndTime = reverse ? first : last
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ArrayOf<DataValue> values = GetHistoryData(result);
        values.ToList().Should().HaveCount(2);
        if (reverse)
        {
            values[1].StatusCode.Should().Be((StatusCode)StatusCodes.BadBoundNotFound);
            values[1].SourceTimestamp.Should().Be(details.EndTime);
            values[1].IsNull.Should().BeFalse();
            values[1].WrappedValue.IsNull.Should().BeTrue();
        }
        else
        {
            values[0].StatusCode.Should().Be((StatusCode)StatusCodes.BadBoundNotFound);
            values[0].SourceTimestamp.Should().Be(details.StartTime);
            values[0].IsNull.Should().BeFalse();
            values[0].WrappedValue.IsNull.Should().BeTrue();
        }
    }

    [Test]
    public async Task ContinuationRejectsChangedTimestampSelection()
    {
        ReadRawModifiedDetails details = await RecentRangeAsync(2).ConfigureAwait(false);
        HistoryReadResult first = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        first.ContinuationPoint.IsEmpty.Should().BeFalse();
        HistoryReadResult changed = await ReadHistoryAsync(
            "HistorianInt32", details, first.ContinuationPoint, timestamps: TimestampsToReturn.Both)
            .ConfigureAwait(false);
        changed.StatusCode.Should().Be((StatusCode)StatusCodes.BadContinuationPointInvalid);
        HistoryReadResult reused = await ReadHistoryAsync("HistorianInt32", details, first.ContinuationPoint)
            .ConfigureAwait(false);
        reused.StatusCode.Should().Be((StatusCode)StatusCodes.BadContinuationPointInvalid);
    }

    [Test]
    public async Task SimulationUpdatesInt32NodeAndEvictsOldestRecords()
    {
        DataValue previous = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        FireTimersWithPeriod(TimeSpan.FromSeconds(10), 2001);
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        latest.WrappedValue.CastTo<int>().Should().Be(previous.WrappedValue.CastTo<int>() + 2001);

        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = previous.SourceTimestamp.Add(TimeSpan.FromDays(-1)),
            EndTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(1))
        };
        var values = new List<DataValue>();
        ByteString continuation = default;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details, continuation)
                .ConfigureAwait(false);
            values.AddRange(GetHistoryData(result).ToList());
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(25);
        }
        while (!continuation.IsEmpty);

        values.Should().HaveCount(2000);
        values[0].SourceTimestamp.Should().Be(latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-19990)));
        values[^1].WrappedValue.Should().Be(latest.WrappedValue);
    }

    private async Task<ReadRawModifiedDetails> RecentRangeAsync(uint count)
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        return new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(-10000)),
            EndTime = latest.SourceTimestamp.Add(TimeSpan.FromSeconds(1)),
            NumValuesPerNode = count
        };
    }

    private async Task<HistoryReadResult> ReadHistoryAsync(
        string nodeName, ReadRawModifiedDetails details, ByteString continuation = default, bool release = false,
        ISession session = null, TimestampsToReturn timestamps = TimestampsToReturn.Source)
    {
        HistoryReadResponse response = await (session ?? Session).HistoryReadAsync(
            null, new ExtensionObject((IEncodeable)details), timestamps, release,
            [
                new HistoryReadValueId { NodeId = GetOpcPlcNodeId(nodeName), ContinuationPoint = continuation }
            ], CancellationToken.None).ConfigureAwait(false);
        return response.Results.ToList().Single();
    }

    private static ArrayOf<DataValue> GetHistoryData(HistoryReadResult result)
    {
        result.HistoryData.TryGetValue(out HistoryData data).Should().BeTrue();
        return data.DataValues;
    }
}
