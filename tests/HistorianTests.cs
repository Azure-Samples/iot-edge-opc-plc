namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class HistorianTests() : SimulatorTestsBase(["--historian"])
{
    [Test]
    public async Task HistorianNodeAdvertisesHistoryRead()
    {
        ReadResponse response = await Session.ReadAsync(null, 0, TimestampsToReturn.Neither,
            new ReadValueIdCollection
            {
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.AccessLevel },
                new ReadValueId
                {
                    NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.UserAccessLevel
                },
                new ReadValueId { NodeId = GetOpcPlcNodeId("HistorianInt32"), AttributeId = Attributes.Historizing }
            }, CancellationToken.None).ConfigureAwait(false);

        response.Results[0].Value.Should().Be((byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));
        response.Results[1].Value.Should().Be((byte)(AccessLevels.CurrentRead | AccessLevels.HistoryRead));
        response.Results[2].Value.Should().Be(true);
    }

    [Test]
    public async Task RawHistoryReturnsSeededSamples()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.AddSeconds(-10000),
            EndTime = latest.SourceTimestamp.AddSeconds(1),
            NumValuesPerNode = 73
        };
        var allValues = new DataValueCollection();
        byte[] continuation = null;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details, continuation)
                .ConfigureAwait(false);
            StatusCode.IsGood(result.StatusCode).Should().BeTrue(result.StatusCode.ToString());
            var values = ((HistoryData)result.HistoryData.Body).DataValues;
            values.Should().HaveCountLessThanOrEqualTo(73);
            allValues.AddRange(values);
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(30);
        }
        while (continuation is { Length: > 0 });

        allValues.Should().HaveCount(1001);
        allValues.Select(value => value.SourceTimestamp).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        allValues.Should().OnlyContain(value => value.Value is int
            && value.StatusCode == StatusCodes.Good && value.ServerTimestamp == DateTime.MinValue);
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
            StartTime = reverse ? latest.SourceTimestamp : latest.SourceTimestamp.AddSeconds(-30),
            EndTime = reverse ? latest.SourceTimestamp.AddSeconds(-30) : latest.SourceTimestamp
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        var values = ((HistoryData)result.HistoryData.Body).DataValues;

        values.Should().HaveCount(count);
        values[0].SourceTimestamp.Should().Be(details.StartTime);
        values[^1].SourceTimestamp.Should().Be(bounds
            ? details.EndTime : details.EndTime.AddSeconds(reverse ? 10 : -10));
        result.ContinuationPoint.Should().BeNullOrEmpty();
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
            StartTime = latest.SourceTimestamp.AddSeconds(-10),
            EndTime = latest.SourceTimestamp.AddSeconds(-10)
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        var values = ((HistoryData)result.HistoryData.Body).DataValues;

        values.Should().HaveCount(count);
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
        ((HistoryData)result.HistoryData.Body).DataValues.Should().BeEmpty();
        result.ContinuationPoint.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task ReleasedContinuationCannotBeReused()
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.AddMinutes(-5),
            EndTime = latest.SourceTimestamp,
            NumValuesPerNode = 2
        };
        HistoryReadResult first = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        first.ContinuationPoint.Should().NotBeNullOrEmpty();
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
            new BrowseDescriptionCollection
            {
                new BrowseDescription
                {
                    NodeId = GetOpcPlcNodeId("Historian"),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All
                }
            }, CancellationToken.None).ConfigureAwait(false);

        response.Results.Single().References.Select(reference => reference.BrowseName.Name)
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

        ((HistoryData)result.HistoryData.Body).DataValues.Should().HaveCount(100);
        result.ContinuationPoint.Should().NotBeNullOrEmpty();
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
        DateTime firstTimestamp = await ReadValueAsync<DateTime>(archiveStart).ConfigureAwait(false);
        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = firstTimestamp,
            EndTime = firstTimestamp
        };
        HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details).ConfigureAwait(false);
        ((HistoryData)result.HistoryData.Body).DataValues.Should().ContainSingle()
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
        var firstValues = ((HistoryData)first.HistoryData.Body).DataValues;
        firstValues.Should().HaveCount(100);
        first.ContinuationPoint.Should().NotBeNullOrEmpty();
        HistoryReadResult last = await ReadHistoryAsync(
            "HistorianInt32", details, first.ContinuationPoint).ConfigureAwait(false);
        var lastValues = ((HistoryData)last.HistoryData.Body).DataValues;
        lastValues.Should().ContainSingle();
        lastValues[0].SourceTimestamp.Should().Be(firstValues[0].SourceTimestamp.AddSeconds(reverse ? -1000 : 1000));
        last.ContinuationPoint.Should().BeNullOrEmpty();
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
        var factory = new DefaultSessionFactory(null);
        using ISession otherSession = await factory.RecreateAsync(Session, CancellationToken.None)
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
            await otherSession.CloseAsync().ConfigureAwait(false);
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

    [Test]
    public async Task SimulationUpdatesInt32NodeAndEvictsOldestRecords()
    {
        DataValue previous = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        FireTimersWithPeriod(TimeSpan.FromSeconds(10), 2001);
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        latest.Value.Should().Be((int)previous.Value + 2001);

        var details = new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = previous.SourceTimestamp.AddDays(-1),
            EndTime = latest.SourceTimestamp.AddSeconds(1)
        };
        var values = new DataValueCollection();
        byte[] continuation = null;
        int pages = 0;
        do
        {
            HistoryReadResult result = await ReadHistoryAsync("HistorianInt32", details, continuation)
                .ConfigureAwait(false);
            values.AddRange(((HistoryData)result.HistoryData.Body).DataValues);
            continuation = result.ContinuationPoint;
            (++pages).Should().BeLessThan(25);
        }
        while (continuation is { Length: > 0 });

        values.Should().HaveCount(2000);
        values[0].SourceTimestamp.Should().Be(latest.SourceTimestamp.AddSeconds(-19990));
        values[^1].Value.Should().Be(latest.Value);
    }

    private async Task<ReadRawModifiedDetails> RecentRangeAsync(uint count)
    {
        DataValue latest = await ReadDataValueAsync(GetOpcPlcNodeId("HistorianInt32")).ConfigureAwait(false);
        return new ReadRawModifiedDetails
        {
            IsReadModified = false,
            ReturnBounds = false,
            StartTime = latest.SourceTimestamp.AddSeconds(-10000),
            EndTime = latest.SourceTimestamp.AddSeconds(1),
            NumValuesPerNode = count
        };
    }

    private async Task<HistoryReadResult> ReadHistoryAsync(
        string nodeName, ReadRawModifiedDetails details, byte[] continuation = null, bool release = false,
        ISession session = null)
    {
        HistoryReadResponse response = await (session ?? Session).HistoryReadAsync(
            null, new ExtensionObject(details), TimestampsToReturn.Source, release,
            new HistoryReadValueIdCollection
            {
                new HistoryReadValueId { NodeId = GetOpcPlcNodeId(nodeName), ContinuationPoint = continuation }
            }, CancellationToken.None).ConfigureAwait(false);
        return response.Results.Single();
    }
}