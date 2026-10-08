namespace OpcPlc;

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlcNodeManager
{
    private const int HistoryCapacity = 2000;
    private const uint HistoryPageSize = 100;
    private readonly Dictionary<NodeId,
        (BaseDataVariableState Variable, HistoricalDataConfigurationState Configuration, Queue<DataValue> Values)>
        _history = new();

    internal void EnableHistory(BaseDataVariableState variable)
    {
        lock (Lock)
        {
            DateTime now = _timeService.UtcNow();
            var values = new Queue<DataValue>(HistoryCapacity);
            for (int sample = 0; sample <= 1000; sample++)
            {
                values.Enqueue(new DataValue(new Variant(sample), StatusCodes.Good,
                    now.AddSeconds((sample - 1000) * 10), now.AddSeconds((sample - 1000) * 10)));
            }

            DataValue latest = values.Last();
            variable.Value = latest.Value;
            variable.Timestamp = latest.SourceTimestamp;
            variable.UserAccessLevel = variable.AccessLevel;

            var configuration = new HistoricalDataConfigurationState(variable);
            configuration.StartOfArchive = new PropertyState<DateTime>(configuration);
            configuration.StartOfOnlineArchive = new PropertyState<DateTime>(configuration);
            configuration.Create(SystemContext,
                new NodeId($"{variable.NodeId.Identifier}_HAConfiguration", variable.NodeId.NamespaceIndex),
                new QualifiedName(BrowseNames.HAConfiguration), null, true);
            configuration.ReferenceTypeId = ReferenceTypeIds.HasHistoricalConfiguration;
            configuration.Stepped.Value = true;
            configuration.MinTimeInterval.Value = 10000;
            configuration.MaxTimeInterval.Value = 10000;
            configuration.StartOfArchive.Value = values.Peek().SourceTimestamp;
            configuration.StartOfOnlineArchive.Value = values.Peek().SourceTimestamp;
            variable.AddChild(configuration);
            _history.Add(variable.NodeId, (variable, configuration, values));

            HistoryServerCapabilitiesState capabilities = Server.DiagnosticsNodeManager.GetDefaultHistoryCapabilities();
            capabilities.AccessHistoryDataCapability.Value = true;
            capabilities.MaxReturnDataValues.Value = HistoryPageSize;
            capabilities.ServerTimestampSupported.Value = true;
        }
    }

    internal void UpdateHistory()
    {
        lock (Lock)
        {
            DateTime now = _timeService.UtcNow();
            foreach (var entry in _history.Values)
            {
                if (!entry.Variable.Historizing || now <= entry.Variable.Timestamp)
                {
                    continue;
                }

                int value = (int)entry.Variable.Value + 1;
                entry.Values.Enqueue(new DataValue(new Variant(value), StatusCodes.Good, now, now));
                if (entry.Values.Count > HistoryCapacity)
                {
                    entry.Values.Dequeue();
                    entry.Configuration.StartOfArchive.Value = entry.Values.Peek().SourceTimestamp;
                    entry.Configuration.StartOfOnlineArchive.Value = entry.Values.Peek().SourceTimestamp;
                    entry.Configuration.ClearChangeMasks(SystemContext, false);
                }

                entry.Variable.Value = value;
                entry.Variable.Timestamp = now;
                entry.Variable.ClearChangeMasks(SystemContext, false);
            }
        }
    }

    protected override void HistoryReadRawModified(
        ServerSystemContext context,
        ReadRawModifiedDetails details,
        TimestampsToReturn timestampsToReturn,
        IList<HistoryReadValueId> nodesToRead,
        IList<HistoryReadResult> results,
        IList<ServiceResult> errors,
        List<NodeHandle> nodesToProcess,
        IDictionary<NodeId, NodeState> cache)
    {
        lock (Lock)
        {
            foreach (NodeHandle handle in nodesToProcess)
            {
                errors[handle.Index] = ReadHistory(
                    context, details, timestampsToReturn, nodesToRead[handle.Index], results[handle.Index]);
            }
        }
    }

    protected override void HistoryReleaseContinuationPoints(
        ServerSystemContext context,
        IList<HistoryReadValueId> nodesToRead,
        IList<ServiceResult> errors,
        List<NodeHandle> nodesToProcess,
        IDictionary<NodeId, NodeState> cache)
    {
        foreach (NodeHandle handle in nodesToProcess)
        {
            HistoryReadValueId request = nodesToRead[handle.Index];
            using var cursor = context.OperationContext.Session
                .RestoreHistoryContinuationPoint(request.ContinuationPoint) as HistoryCursor;
            errors[handle.Index] = cursor != null && cursor.NodeId == request.NodeId
                ? ServiceResult.Good
                : StatusCodes.BadContinuationPointInvalid;
        }
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

        if (!QualifiedName.IsNull(request.DataEncoding))
        {
            return StatusCodes.BadDataEncodingUnsupported;
        }

        HistoryCursor cursor;
        if (request.ContinuationPoint is { Length: > 0 })
        {
            cursor = context.OperationContext.Session
                .RestoreHistoryContinuationPoint(request.ContinuationPoint) as HistoryCursor;
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
            bool hasStart = details.StartTime != DateTime.MinValue;
            bool hasEnd = details.EndTime != DateTime.MinValue;
            if ((!hasStart && !hasEnd) || ((!hasStart || !hasEnd) && details.NumValuesPerNode == 0))
            {
                return StatusCodes.BadInvalidArgument;
            }

            cursor = new HistoryCursor(request.NodeId, details, timestampsToReturn,
                SelectHistory(entry.Values, details));
        }

        DataValueCollection values = cursor.ReadPage();
        result.HistoryData = new ExtensionObject(new HistoryData { DataValues = values });
        result.StatusCode = values.Count == 0 ? StatusCodes.GoodNoData : StatusCodes.Good;
        if (cursor.HasMore)
        {
            context.OperationContext.Session.SaveHistoryContinuationPoint(cursor.Id, cursor);
            result.ContinuationPoint = cursor.Id.ToByteArray();
        }
        else
        {
            cursor.Dispose();
        }

        return result.StatusCode;
    }

    private static List<DataValue> SelectHistory(Queue<DataValue> archive, ReadRawModifiedDetails details)
    {
        bool hasStart = details.StartTime != DateTime.MinValue;
        bool hasEnd = details.EndTime != DateTime.MinValue;
        bool reverse = !hasStart || (hasEnd && details.EndTime < details.StartTime);
        DateTime first = hasStart ? details.StartTime : details.EndTime;
        DateTime last = hasStart && hasEnd ? details.EndTime : (reverse ? DateTime.MinValue : DateTime.MaxValue);
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
                selected.Insert(0, bound ?? new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, first, first));
            }

            if (hasStart && hasEnd)
            {
                DataValue bound = reverse
                    ? archive.LastOrDefault(value => value.SourceTimestamp <= last)
                    : archive.FirstOrDefault(value => first == last
                        ? value.SourceTimestamp > last : value.SourceTimestamp >= last);
                selected.Add(bound ?? new DataValue(Variant.Null, StatusCodes.BadBoundNotFound, last, last));
            }
            else if (selected.Count < details.NumValuesPerNode)
            {
                DateTime timestamp = selected[^1].SourceTimestamp;
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
        : IDisposable
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

        public DataValueCollection ReadPage()
        {
            uint limit = _details.NumValuesPerNode == 0
                ? HistoryPageSize : Math.Min(_details.NumValuesPerNode, HistoryPageSize);
            var page = new DataValueCollection();
            while (HasMore && page.Count < limit)
            {
                var value = (DataValue)values[_position++].Clone();
                if (timestamps is TimestampsToReturn.Neither or TimestampsToReturn.Server)
                {
                    value.SourceTimestamp = DateTime.MinValue;
                }

                if (timestamps is TimestampsToReturn.Neither or TimestampsToReturn.Source)
                {
                    value.ServerTimestamp = DateTime.MinValue;
                }

                page.Add(value);
            }

            return page;
        }

        public void Dispose() => values.Clear();
    }
}