namespace OpcPlc;

using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlcNodeManager
{
    private const string HistorianEventsName = "HistorianEvents";
    private readonly Queue<BaseEventState> _eventHistory = new(HistoryCapacity);
    private BaseObjectState _eventNotifier;
    private uint _eventSequence;

    internal void EnableEventHistory(FolderState folder)
    {
        lock (Lock)
        {
            ushort namespaceIndex = NamespaceIndexes[(int)NamespaceType.OpcPlcApplications];
            _eventNotifier = new BaseObjectState(folder) {
                SymbolicName = HistorianEventsName,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.BaseObjectType,
                NodeId = new NodeId(HistorianEventsName, namespaceIndex),
                BrowseName = new QualifiedName(HistorianEventsName, namespaceIndex),
                DisplayName = new LocalizedText("en", HistorianEventsName),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.SubscribeToEvents | EventNotifiers.HistoryRead,
            };
            folder.AddChild(_eventNotifier);
            AddRootNotifier(_eventNotifier);

            DateTime now = _timeService.UtcNow();
            for (int sample = 0; sample <= 1000; sample++)
            {
                _eventHistory.Enqueue(CreateHistorianEvent(now.AddSeconds((sample - 1000) * 10)));
            }

            HistoryServerCapabilitiesState capabilities = Server.DiagnosticsNodeManager.GetDefaultHistoryCapabilities();
            capabilities.AccessHistoryEventsCapability.Value = true;
            capabilities.MaxReturnEventValues.Value = HistoryPageSize;
        }
    }

    internal void UpdateEventHistory()
    {
        BaseEventState historianEvent;
        lock (Lock)
        {
            DateTime now = _timeService.UtcNow();
            if (_eventNotifier == null || (_eventHistory.Count > 0 && now <= _eventHistory.Last().Time.Value))
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

    protected override void HistoryReadEvents(
        ServerSystemContext context,
        ReadEventDetails details,
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
                errors[handle.Index] = ReadEventHistory(
                    context, details, nodesToRead[handle.Index], results[handle.Index]);
            }
        }
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

        if (!QualifiedName.IsNull(request.DataEncoding))
        {
            return StatusCodes.BadDataEncodingUnsupported;
        }

        EventHistoryCursor cursor;
        if (request.ContinuationPoint is { Length: > 0 })
        {
            cursor = context.OperationContext.Session
                .RestoreHistoryContinuationPoint(request.ContinuationPoint) as EventHistoryCursor;
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
            bool hasStart = details.StartTime != DateTime.MinValue;
            bool hasEnd = details.EndTime != DateTime.MinValue;
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

        HistoryEventFieldListCollection page = cursor.ReadPage();
        result.HistoryData = new ExtensionObject(new HistoryEvent { Events = page });
        result.StatusCode = page.Count == 0 ? StatusCodes.GoodNoData : StatusCodes.Good;
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

    private List<BaseEventState> SelectEvents(ReadEventDetails details)
    {
        bool hasStart = details.StartTime != DateTime.MinValue;
        bool hasEnd = details.EndTime != DateTime.MinValue;
        bool reverse = !hasStart || (hasEnd && details.EndTime < details.StartTime);
        DateTime first = hasStart ? details.StartTime : details.EndTime;
        DateTime last = hasStart && hasEnd ? details.EndTime : (reverse ? DateTime.MinValue : DateTime.MaxValue);
        var selected = _eventHistory.Where(historianEvent => {
            DateTime time = historianEvent.Time.Value;
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

    private static HistoryEventFieldList GetEventFields(
        FilterContext context, EventFilter filter, BaseEventState historianEvent)
    {
        var fields = new HistoryEventFieldList();
        foreach (SimpleAttributeOperand clause in filter.SelectClauses)
        {
            object value = historianEvent.GetAttributeValue(
                context, clause.TypeDefinitionId, clause.BrowsePath, clause.AttributeId, clause.ParsedIndexRange);
            fields.EventFields.Add(value == null ? Variant.Null : new Variant(value));
        }

        return fields;
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

        public HistoryEventFieldListCollection ReadPage()
        {
            uint limit = _details.NumValuesPerNode == 0
                ? HistoryPageSize : Math.Min(_details.NumValuesPerNode, HistoryPageSize);
            var page = new HistoryEventFieldListCollection();
            while (HasMore && page.Count < limit)
            {
                page.Add(events[_position++]);
            }

            return page;
        }

        public void Dispose() => events.Clear();
    }
}
