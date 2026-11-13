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
    private const string HistorianEventsName = "HistorianEvents";
    private readonly Queue<BaseEventState> _eventHistory = new(HistoryCapacity);
    private BaseObjectState _eventNotifier;
    private uint _eventSequence;

    internal async ValueTask EnableEventHistoryAsync(FolderState folder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_historyLock)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];
            _eventNotifier = new BaseObjectState(folder) {
                SymbolicName = HistorianEventsName,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = ObjectTypeIds.BaseObjectType,
                NodeId = new NodeId(HistorianEventsName, namespaceIndex),
                BrowseName = new QualifiedName(HistorianEventsName, namespaceIndex),
                DisplayName = new LocalizedText("en", HistorianEventsName),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead,
            };
            folder.AddChild(_eventNotifier);

            DateTime now = _timeService.UtcNow();
            for (int sample = 0; sample <= 1000; sample++)
            {
                _eventHistory.Enqueue(CreateHistorianEvent(now.AddSeconds((sample - 1000) * 10)));
            }
        }

        await AddRootNotifierAsync(_eventNotifier, cancellationToken).ConfigureAwait(false);
        HistoryServerCapabilitiesState capabilities = await Server.DiagnosticsNodeManager
            .GetDefaultHistoryCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        capabilities.AccessHistoryEventsCapability.Value = true;
        capabilities.MaxReturnEventValues.Value = HistoryPageSize;
    }

    internal void UpdateEventHistory()
    {
        BaseEventState historianEvent;
        lock (_historyLock)
        {
            DateTime now = _timeService.UtcNow();
            if (_eventNotifier == null || (_eventHistory.Count > 0 && (DateTimeUtc)now <= GetEventTime(_eventHistory.Last())))
            {
                return;
            }

            historianEvent = CreateHistorianEvent(now);
            _eventHistory.Enqueue(historianEvent);
            if (_eventHistory.Count > HistoryCapacity)
            {
                _eventHistory.Dequeue();
            }
        }

        _eventNotifier.ReportEvent(SystemContext, historianEvent);
    }

    protected override ValueTask HistoryReadEventsAsync(
        ServerSystemContext context,
        ReadEventDetails details,
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
                errors[handle.Index] = ReadEventHistory(
                    context, details, nodesToRead[handle.Index], results[handle.Index]);
            }
        }
        return ValueTask.CompletedTask;
    }

    private static DateTimeUtc GetEventTime(BaseEventState historianEvent) => historianEvent.Time.Value;

    private static HistoryEventFieldList GetEventFields(
        FilterContext context, EventFilter filter, BaseEventState historianEvent)
    {
        var fields = new List<Variant>();
        foreach (SimpleAttributeOperand clause in filter.SelectClauses)
        {
            fields.Add(historianEvent.GetAttributeValue(
                context, clause.TypeDefinitionId, clause.BrowsePath, clause.AttributeId,
                NumericRange.Parse(clause.IndexRange)));
        }

        return new HistoryEventFieldList { EventFields = fields.ToArrayOf() };
    }

    private BaseEventState CreateHistorianEvent(DateTime time)
    {
        var historianEvent = new BaseEventState(null);
        historianEvent.Initialize(
            SystemContext, _eventNotifier, EventSeverity.Medium,
            new LocalizedText($"Historian event {_eventSequence++}"));
        historianEvent.Time.Value = time;
        historianEvent.ReceiveTime.Value = time;
        return historianEvent;
    }

    private ServiceResult ReadEventHistory(
        ServerSystemContext context, ReadEventDetails details, HistoryReadValueId request, HistoryReadResult result)
    {
        if (_eventNotifier == null || request.NodeId != _eventNotifier.NodeId)
        {
            return StatusCodes.BadHistoryOperationUnsupported;
        }

        if (!string.IsNullOrEmpty(request.IndexRange))
        {
            return StatusCodes.BadIndexRangeNoData;
        }

        if (!request.DataEncoding.IsNull)
        {
            return StatusCodes.BadDataEncodingUnsupported;
        }

        EventHistoryCursor cursor;
        if (!request.ContinuationPoint.IsEmpty)
        {
            cursor = context.OperationContext.Session.ContinuationPoints
                .RestoreHistory(request.ContinuationPoint) as EventHistoryCursor;
            if (cursor == null)
            {
                return StatusCodes.BadContinuationPointInvalid;
            }

            if (cursor.NodeId != request.NodeId || !cursor.Matches(details))
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

            if (details.Filter == null || details.Filter.SelectClauses.Count == 0)
            {
                return StatusCodes.BadEventFilterInvalid;
            }

            var filterContext = new FilterContext(
                Server.NamespaceUris, Server.TypeTree, context.OperationContext, Server.Telemetry);
            if (ServiceResult.IsBad(details.Filter.Validate(filterContext).Status))
            {
                return StatusCodes.BadEventFilterInvalid;
            }

            List<HistoryEventFieldList> events = SelectEvents(details)
                .Where(historianEvent => details.Filter.WhereClause.Evaluate(filterContext, historianEvent))
                .Select(historianEvent => GetEventFields(filterContext, details.Filter, historianEvent))
                .ToList();
            if ((!hasStart || !hasEnd) && details.NumValuesPerNode < events.Count)
            {
                events.RemoveRange((int)details.NumValuesPerNode, events.Count - (int)details.NumValuesPerNode);
            }

            cursor = new EventHistoryCursor(request.NodeId, details, events);
        }

        ArrayOf<HistoryEventFieldList> page = cursor.ReadPage();
        result.HistoryData = new ExtensionObject((IEncodeable)new HistoryEvent { Events = page });
        result.StatusCode = page.Count == 0 ? StatusCodes.GoodNoData : StatusCodes.Good;
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

    private List<BaseEventState> SelectEvents(ReadEventDetails details)
    {
        bool hasStart = details.StartTime != DateTimeUtc.MinValue;
        bool hasEnd = details.EndTime != DateTimeUtc.MinValue;
        bool reverse = !hasStart || (hasEnd && details.EndTime < details.StartTime);
        DateTimeUtc first = hasStart ? details.StartTime : details.EndTime;
        DateTimeUtc last = hasStart && hasEnd ? details.EndTime :
            (reverse ? DateTimeUtc.MinValue : DateTimeUtc.MaxValue);
        var selected = _eventHistory.Where(historianEvent => {
            DateTimeUtc time = GetEventTime(historianEvent);
            return first == last
                ? time == first
                : reverse ? time <= first && time > last : time >= first && time < last;
        }).ToList();

        if (reverse)
        {
            selected.Reverse();
        }

        return selected;
    }

    private sealed class EventHistoryCursor(NodeId nodeId, ReadEventDetails details, List<HistoryEventFieldList> events)
        : IHistoryContinuation
    {
        private readonly ReadEventDetails _details = (ReadEventDetails)details.Clone();
        private int _position;

        public Guid Id { get; } = Guid.NewGuid();

        public NodeId NodeId { get; } = nodeId;

        public bool HasMore => _position < events.Count;

        public bool Matches(ReadEventDetails request)
        {
            return request.StartTime == _details.StartTime && request.EndTime == _details.EndTime
                && request.NumValuesPerNode == _details.NumValuesPerNode
                && Utils.IsEqual(request.Filter, _details.Filter);
        }

        public ArrayOf<HistoryEventFieldList> ReadPage()
        {
            uint limit = _details.NumValuesPerNode == 0
                ? HistoryPageSize : Math.Min(_details.NumValuesPerNode, HistoryPageSize);
            var page = new List<HistoryEventFieldList>();
            while (HasMore && page.Count < limit)
            {
                page.Add(events[_position++]);
            }

            return page.ToArrayOf();
        }

        public void Dispose() => events.Clear();
    }
}
