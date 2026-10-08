namespace OpcPlc;

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class PlcNodeManager
{
    private const int HistoryCapacity = 2000;
    private const uint HistoryPageSize = 100;
    private readonly object _historyLock = new();
    private readonly Dictionary<NodeId,
        (BaseDataVariableState Variable, HistoricalDataConfigurationState Configuration, Queue<DataValue> Values)>
        _history = new();

    internal async ValueTask EnableHistoryAsync(
        BaseDataVariableState variable, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HistoricalDataConfigurationState configuration = SystemContext.CreateInstanceOfHistoricalDataConfigurationType(
            variable, QualifiedName.From(BrowseNames.HAConfiguration));
        configuration.NodeId = new NodeId(
            $"{variable.NodeId.IdentifierAsString}_HAConfiguration", variable.NodeId.NamespaceIndex);
        configuration
            .AddMinTimeInterval(SystemContext, true, property => property.Value = 10000)
            .AddMaxTimeInterval(SystemContext, true, property => property.Value = 10000)
            .AddStartOfArchive(SystemContext, default)
            .AddStartOfOnlineArchive(SystemContext, default);
        lock (_historyLock)
        {
            DateTime now = _timeService.UtcNow();
            var values = new Queue<DataValue>(HistoryCapacity);
            for (int sample = 0; sample <= 1000; sample++)
            {
                values.Enqueue(new DataValue(new Variant(sample), StatusCodes.Good,
                    now.AddSeconds((sample - 1000) * 10), now.AddSeconds((sample - 1000) * 10)));
            }

            DataValue latest = values.Last();
            variable.Value = latest.WrappedValue;
            variable.Timestamp = latest.SourceTimestamp;
            variable.UserAccessLevel = variable.AccessLevel;

            configuration.ReferenceTypeId = ReferenceTypeIds.HasHistoricalConfiguration;
            configuration.Stepped.Value = true;
            configuration.StartOfArchive.Value = values.Peek().SourceTimestamp;
            configuration.StartOfOnlineArchive.Value = values.Peek().SourceTimestamp;
            variable.AddChild(configuration);
            _history.Add(variable.NodeId, (variable, configuration, values));
        }
        HistoryServerCapabilitiesState capabilities = await Server.DiagnosticsNodeManager
            .GetDefaultHistoryCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        capabilities.AccessHistoryDataCapability.Value = true;
        capabilities.MaxReturnDataValues.Value = HistoryPageSize;
        capabilities.ServerTimestampSupported.Value = true;
    }

    internal void UpdateHistory()
    {
        lock (_historyLock)
        {
            DateTime now = _timeService.UtcNow();
            foreach (var entry in _history.Values)
            {
                if (!entry.Variable.Historizing || now <= entry.Variable.Timestamp)
                {
                    continue;
                }

                int value = entry.Variable.Value.CastTo<int>() + 1;
                entry.Values.Enqueue(new DataValue(new Variant(value), StatusCodes.Good, now, now));
                if (entry.Values.Count > HistoryCapacity)
                {
                    entry.Values.Dequeue();
                    entry.Configuration.StartOfArchive.Value = entry.Values.Peek().SourceTimestamp;
                    entry.Configuration.StartOfOnlineArchive.Value = entry.Values.Peek().SourceTimestamp;
                    entry.Configuration.ClearChangeMasks(SystemContext, false);
                }

                entry.Variable.Value = Variant.From(value);
                entry.Variable.Timestamp = now;
                entry.Variable.ClearChangeMasks(SystemContext, false);
            }
        }
    }

    protected override bool HasHistorianProvider(NodeState node)
    {
        lock (_historyLock)
        {
            if (_history.ContainsKey(node.NodeId))
            {
                return true;
            }
        }
        return base.HasHistorianProvider(node);
    }

    protected override ValueTask HistoryReadRawModifiedAsync(
        ServerSystemContext context,
        ReadRawModifiedDetails details,
        TimestampsToReturn timestampsToReturn,
        ArrayOf<HistoryReadValueId> nodesToRead,
        IList<HistoryReadResult> results,
        IList<ServiceResult> errors,
        List<NodeHandle> nodesToProcess,
        IDictionary<NodeId, NodeState> cache,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_historyLock)
        {
            foreach (NodeHandle handle in nodesToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                errors[handle.Index] = ReadHistory(
                    context, details, timestampsToReturn, nodesToRead[handle.Index], results[handle.Index]);
            }
        }
        return ValueTask.CompletedTask;
    }

    protected override ValueTask HistoryReleaseContinuationPointsAsync(
        ServerSystemContext context,
        ArrayOf<HistoryReadValueId> nodesToRead,
        IList<ServiceResult> errors,
        List<NodeHandle> nodesToProcess,
        IDictionary<NodeId, NodeState> cache,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (NodeHandle handle in nodesToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HistoryReadValueId request = nodesToRead[handle.Index];
            using var cursor = context.OperationContext.Session.ContinuationPoints
                .RestoreHistory(request.ContinuationPoint) as HistoryCursor;
            errors[handle.Index] = cursor != null && cursor.NodeId == request.NodeId
                ? ServiceResult.Good
                : StatusCodes.BadContinuationPointInvalid;
        }
        return ValueTask.CompletedTask;
    }

    private ServiceResult ReadHistory(
        ServerSystemContext context, ReadRawModifiedDetails details, TimestampsToReturn timestampsToReturn,
        HistoryReadValueId request, HistoryReadResult result)
    {
        if (!_history.TryGetValue(request.NodeId, out var entry) || details.IsReadModified)
        {
            return StatusCodes.BadHistoryOperationUnsupported;
        }

        if ((entry.Variable.UserAccessLevel & AccessLevels.HistoryRead) == 0)
        {
            return StatusCodes.BadUserAccessDenied;
        }

        if (!string.IsNullOrEmpty(request.IndexRange))
        {
            return StatusCodes.BadIndexRangeNoData;
        }

        if (!request.DataEncoding.IsNull)
        {
            return StatusCodes.BadDataEncodingUnsupported;
        }

        HistoryCursor cursor;
        if (!request.ContinuationPoint.IsEmpty)
        {
            cursor = context.OperationContext.Session.ContinuationPoints
                .RestoreHistory(request.ContinuationPoint) as HistoryCursor;
            if (cursor == null)
            {
                return StatusCodes.BadContinuationPointInvalid;
            }

            if (cursor.NodeId != request.NodeId || !cursor.Matches(details, timestampsToReturn))
            {
                cursor.Dispose();
                return StatusCodes.BadContinuationPointInvalid;
            }
        }
        else
        {
            bool hasStart = details.StartTime != DateTimeUtc.MinValue;
            bool hasEnd = details.EndTime != DateTimeUtc.MinValue;
            if ((!hasStart && !hasEnd) || ((!hasStart || !hasEnd) && details.NumValuesPerNode == 0))
            {
                return StatusCodes.BadInvalidArgument;
            }

            cursor = new HistoryCursor(request.NodeId, details, timestampsToReturn,
                SelectHistory(entry.Values, details));
        }

        ArrayOf<DataValue> values = cursor.ReadPage();
        result.HistoryData = new ExtensionObject((IEncodeable)new HistoryData { DataValues = values });
        result.StatusCode = values.Count == 0 ? StatusCodes.GoodNoData : StatusCodes.Good;
        if (cursor.HasMore)
        {
            context.OperationContext.Session.ContinuationPoints.SaveHistory(cursor);
            result.ContinuationPoint = ByteString.From(cursor.Id.ToByteArray());
        }
        else
        {
            cursor.Dispose();
        }

        return result.StatusCode;
    }

    private static List<DataValue> SelectHistory(Queue<DataValue> archive, ReadRawModifiedDetails details)
    {
        bool hasStart = details.StartTime != DateTimeUtc.MinValue;
        bool hasEnd = details.EndTime != DateTimeUtc.MinValue;
        bool reverse = !hasStart || (hasEnd && details.EndTime < details.StartTime);
        DateTimeUtc first = hasStart ? details.StartTime : details.EndTime;
        DateTimeUtc last = hasStart && hasEnd ? details.EndTime :
            (reverse ? DateTimeUtc.MinValue : DateTimeUtc.MaxValue);
        var selected = archive.Where(value => first == last
            ? value.SourceTimestamp == first
            : reverse
                ? value.SourceTimestamp <= first && value.SourceTimestamp > last
                : value.SourceTimestamp >= first && value.SourceTimestamp < last).ToList();

        if (reverse)
        {
            selected.Reverse();
        }

        if (details.ReturnBounds)
        {
            if (selected.Count == 0 || selected[0].SourceTimestamp != first)
            {
                DataValue bound = reverse
                    ? archive.FirstOrDefault(value => value.SourceTimestamp >= first)
                    : archive.LastOrDefault(value => value.SourceTimestamp <= first);
                selected.Insert(0, bound.IsNull
                    ? new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, first, first) : bound);
            }

            if (hasStart && hasEnd)
            {
                DataValue bound = reverse
                    ? archive.LastOrDefault(value => value.SourceTimestamp <= last)
                    : archive.FirstOrDefault(value => first == last
                        ? value.SourceTimestamp > last : value.SourceTimestamp >= last);
                selected.Add(bound.IsNull
                    ? new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, last, last) : bound);
            }
            else if (selected.Count < details.NumValuesPerNode)
            {
                DateTime timestamp = selected[^1].SourceTimestamp.ToDateTime();
                long ticks = Math.Clamp(
                    timestamp.Ticks + (reverse ? -TimeSpan.TicksPerSecond : TimeSpan.TicksPerSecond),
                    DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);
                timestamp = new DateTime(ticks, DateTimeKind.Utc);
                selected.Add(new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, timestamp, timestamp));
            }
        }

        if ((!hasStart || !hasEnd) && details.NumValuesPerNode < selected.Count)
        {
            selected.RemoveRange((int)details.NumValuesPerNode, selected.Count - (int)details.NumValuesPerNode);
        }

        return selected;
    }

    private sealed class HistoryCursor(
        NodeId nodeId, ReadRawModifiedDetails details, TimestampsToReturn timestamps, List<DataValue> values)
        : IHistoryContinuationPoint
    {
        private readonly ReadRawModifiedDetails _details = (ReadRawModifiedDetails)details.Clone();
        private int _position;

        public Guid Id { get; } = Guid.NewGuid();

        public NodeId NodeId { get; } = nodeId;

        public bool HasMore => _position < values.Count;

        public bool Matches(ReadRawModifiedDetails request, TimestampsToReturn selection)
        {
            return request.StartTime == _details.StartTime && request.EndTime == _details.EndTime
                && request.NumValuesPerNode == _details.NumValuesPerNode
                && request.ReturnBounds == _details.ReturnBounds
                && timestamps == selection;
        }

        public ArrayOf<DataValue> ReadPage()
        {
            uint limit = _details.NumValuesPerNode == 0
                ? HistoryPageSize : Math.Min(_details.NumValuesPerNode, HistoryPageSize);
            var page = new List<DataValue>();
            while (HasMore && page.Count < limit)
            {
                DataValue value = values[_position++];
                if (timestamps is TimestampsToReturn.Neither or TimestampsToReturn.Server)
                {
                    value = value.WithSourceTimestamp(DateTimeUtc.MinValue);
                }

                if (timestamps is TimestampsToReturn.Neither or TimestampsToReturn.Source)
                {
                    value = value.WithServerTimestamp(DateTimeUtc.MinValue);
                }

                page.Add(value);
            }

            return page.ToArrayOf();
        }

        public void Dispose() => values.Clear();
    }
}
