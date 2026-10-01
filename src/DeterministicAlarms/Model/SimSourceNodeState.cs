namespace OpcPlc.DeterministicAlarms.Model;

using Opc.Ua;
using OpcPlc.DeterministicAlarms.Configuration;
using OpcPlc.DeterministicAlarms.SimBackend;
using System;
using System.Collections.Generic;

public class SimSourceNodeState : BaseObjectState
{
    private readonly DeterministicAlarmsNodeManager _nodeManager;
    private readonly SimSourceNodeBackend _simSourceNodeBackend;
    private readonly Dictionary<string, ConditionState> _alarmNodes = new();
    private readonly Dictionary<string, ConditionState> _events = new();
    private bool _deleted;

    public SimSourceNodeState(DeterministicAlarmsNodeManager nodeManager, NodeId nodeId, string name, List<Alarm> alarms) : base(null)
    {
        _nodeManager = nodeManager;

        Initialize(_nodeManager.SystemContext);

        // Creates the whole backend object model for one source
        _simSourceNodeBackend = ((SimBackendService)_nodeManager.SystemContext.SystemHandle)
            .CreateSourceNodeBackend(name, alarms, OnAlarmChanged);

        // initialize the area with the fixed metadata.
        SymbolicName = name;
        NodeId = nodeId;
        BrowseName = new QualifiedName(name, nodeId.NamespaceIndex);
        DisplayName = new LocalizedText(BrowseName.Name);
        Description = default;
        ReferenceTypeId = NodeId.Null;
        TypeDefinitionId = ObjectTypeIds.BaseObjectType;
        EventNotifier = EventNotifiers.SubscribeToEvents;

        // This is to create all alarms
        _simSourceNodeBackend.Refresh();
    }

    public override void ConditionRefresh(ISystemContext context, List<IFilterTarget> events, bool includeChildren)
    {
        lock (_nodeManager.SyncRoot)
        {
            if (_deleted)
            {
                return;
            }
            foreach (var @event in events)
            {
                if (@event is InstanceStateSnapshot instanceSnapShotForExistingEvent &&
                    Object.ReferenceEquals(instanceSnapShotForExistingEvent.Handle, this))
                {
                    return;
                }
            }

            foreach (var alarm in _alarmNodes.Values)
            {
                if (!alarm.Retain.Value)
                {
                    continue;
                }

                var instanceStateSnapshotNewAlarm = new InstanceStateSnapshot();
                instanceStateSnapshotNewAlarm.Initialize(context, alarm);
                instanceStateSnapshotNewAlarm.Handle = this;
                events.Add(instanceStateSnapshotNewAlarm);
            }
        }
    }

    protected override void OnAfterDelete(ISystemContext context)
    {
        lock (_nodeManager.SyncRoot)
        {
            _deleted = true;
            _simSourceNodeBackend.OnAlarmChanged = null;
            _events.Clear();
            base.OnAfterDelete(context);
        }
    }

    private void OnAlarmChanged(SimAlarmStateBackend alarm)
    {
        UpdateAlarmInSource(alarm);
    }

    public void UpdateAlarmInSource(SimAlarmStateBackend alarm, string eventId = null)
    {
        lock (_nodeManager.SyncRoot)
        {
            if (_deleted)
            {
                return;
            }
            if (!_alarmNodes.TryGetValue(alarm.Name, out ConditionState node))
            {
                _alarmNodes[alarm.Name] = node = CreateAlarmOrCondition(alarm, NodeId.Null);
            }

            UpdateAlarm(node, alarm, eventId);
            ReportChanges(node);
        }
    }

    private ConditionState CreateAlarmOrCondition(SimAlarmStateBackend alarm, NodeId branchId)
    {
        ConditionState node = SimAlarmNodeModel.Create(_nodeManager.SystemContext, this, alarm, branchId);

        // don't add branches to the address space.
        if (branchId.IsNull)
        {
            AddChild(node);
        }

        return node;
    }

    private void UpdateAlarm(ConditionState node, SimAlarmStateBackend alarm, string eventId = null)
    {
        // remove old event.
        if (!node.EventId.Value.IsNull)
        {
            _events.Remove(node.EventId.Value.ToHexString());
        }

        SimAlarmNodeModel.Update(_nodeManager.SystemContext, node, alarm, eventId);

        // save the event for later lookup.
        _events[node.EventId.Value.ToHexString()] = node;

    }

    private void ReportChanges(ConditionState alarm)
    {
        // report changes to node attributes.
        alarm.ClearChangeMasks(_nodeManager.SystemContext, true);

        // check if events are being monitored for the source.
        if (AreEventsMonitored)
        {
            // create a snapshot.
            var e = new InstanceStateSnapshot();
            e.Initialize(_nodeManager.SystemContext, alarm);

            // report the event.
            alarm.ReportEvent(_nodeManager.SystemContext, e);
        }
    }
}
