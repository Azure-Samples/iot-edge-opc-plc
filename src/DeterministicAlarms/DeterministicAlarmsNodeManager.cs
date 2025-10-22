namespace OpcPlc.DeterministicAlarms;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.DeterministicAlarms.Configuration;
using OpcPlc.DeterministicAlarms.Model;
using OpcPlc.DeterministicAlarms.SimBackend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class DeterministicAlarmsNodeManager : CustomNodeManager2
{
    private readonly SimBackendService _system;
    private readonly List<SimFolderState> _folders = new();
    private uint _nodeIdCounter;
    private List<NodeState> _rootNotifiers;
    private readonly IServerInternal _server;
    private readonly ServerSystemContext _defaultSystemContext;
    private readonly Dictionary<string, SimSourceNodeState> _sourceNodes = new();
    private readonly Configuration.Configuration _scriptConfiguration;
    private readonly TimeService _timeService;
    private readonly ILogger _logger;
    private Dictionary<string, string> _scriptAlarmToSources;

    /// <summary>
    /// Initializes the node manager.
    /// </summary>
    public DeterministicAlarmsNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService, string scriptFileName, ILogger logger) : base(server, configuration)
    {
        _server = server;
        _defaultSystemContext = _server.DefaultSystemContext.Copy();
        SystemContext.NodeIdFactory = this;
        SystemContext.SystemHandle = _system = new SimBackendService();
        _timeService = timeService;
        _logger = logger;

        // set one namespace for the type model and one names for dynamically created nodes.
        string[] namespaceUrls =
        [
            OpcPlc.Namespaces.OpcPlcDeterministicAlarmsInstance,
        ];

        SetNamespaces(namespaceUrls);

        // read script configuration file
        try
        {
            string json = File.ReadAllText(scriptFileName);
            _scriptConfiguration = Configuration.Configuration.FromJson(json);
        }
        catch (Exception ex)
        {
            LogCannotReadOrDecodeScriptFile(ex);
        }
    }

    /// <summary>
    /// Verifies the script configuration file
    /// </summary>
    /// <param name="scriptConfiguration"></param>
    private void VerifyScriptConfiguration(Configuration.Configuration scriptConfiguration)
    {
        _scriptAlarmToSources = new Dictionary<string, string>();
        foreach (var folder in scriptConfiguration.Folders)
        {
            foreach (var source in folder.Sources)
            {
                if (!_sourceNodes.ContainsKey(source.Name))
                {
                    throw new ScriptException($"Source Name: {source.Name} doesn't exist");
                }

                foreach (var alarm in source.Alarms)
                {
                    if (_scriptAlarmToSources.ContainsKey(alarm.Id))
                    {
                        throw new ScriptException($"AlarmId: {alarm.Id} already exist");
                    }
                    else
                    {
                        _scriptAlarmToSources[alarm.Id] = source.Name;
                    }
                }
            }
        }

        var uniqueEventIds = new HashSet<string>();
        foreach (var step in scriptConfiguration.Script.Steps)
        {
            if (step.Event != null)
            {
                if (!uniqueEventIds.Add(step.Event.EventId))
                {
                    throw new ScriptException($"EventId: {step.Event.EventId} already exist");
                }

                if (!_scriptAlarmToSources.ContainsKey(step.Event.AlarmId))
                {
                    throw new ScriptException($"AlarmId: {step.Event.AlarmId} is not defined");
                }

                if (step.Event.StateChanges == null || step.Event.StateChanges.Count == 0)
                {
                    throw new ScriptException($"{step.Event.EventId} doesn't have any StateChanges");
                }
            }
        }
    }

    /// <summary>
    /// Starts the script replay
    /// </summary>
    /// <param name="scriptConfiguration"></param>
    private void ReplayScriptStart(Configuration.Configuration scriptConfiguration)
    {
        try
        {
            VerifyScriptConfiguration(scriptConfiguration);
            LogScriptStartsExecuting();
            _ = new ScriptEngine(scriptConfiguration.Script, OnScriptStepAvailable, _timeService);
        }
        catch (ScriptException ex)
        {
            LogScriptEngineException(ex.Message, ex);
            throw;
        }
    }

    /// <summary>
    /// Called when a new script step are available
    /// </summary>
    /// <param name="step"></param>
    /// <param name="loopNumber"></param>
    private void OnScriptStepAvailable(Step step, long loopNumber)
    {
        if (step == null)
        {
            LogScriptEnded();
        }
        else
        {
            if (step.Event != null)
            {
                var alarm = GetAlarm(step);
                UpdateAlarm(alarm, step.Event);
                var sourceNodeId = _scriptAlarmToSources[step.Event.AlarmId];
                _sourceNodes[sourceNodeId].UpdateAlarmInSource(alarm, $"{step.Event.EventId} ({loopNumber})");
            }

            PrintScriptStep(step, loopNumber);
        }
    }

    /// <summary>
    /// Update Alarm information
    /// </summary>
    /// <param name="alarm"></param>
    /// <param name="scriptEvent"></param>
    private void UpdateAlarm(SimAlarmStateBackend alarm, Event scriptEvent)
    {
        alarm.Reason = scriptEvent.Reason;
        alarm.Severity = scriptEvent.Severity;
        alarm.Time = DateTime.UtcNow;

        foreach (var stateChange in scriptEvent.StateChanges)
        {
            switch (stateChange.StateType)
            {
                case ConditionStates.Enabled:
                    alarm.SetStateBits(SimConditionStatesEnum.Enabled, stateChange.State);
                    alarm.EnableTime = DateTime.UtcNow;
                    break;
                case ConditionStates.Activated:
                    alarm.SetStateBits(SimConditionStatesEnum.Active, stateChange.State);
                    alarm.ActiveTime = DateTime.UtcNow;
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Get Alarm information
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    private SimAlarmStateBackend GetAlarm(Step step)
    {
        var sourceNodeId = _scriptAlarmToSources[step.Event.AlarmId];
        return _system.SourceNodes[sourceNodeId].Alarms[step.Event.AlarmId];
    }

    /// <summary>
    /// Print script current step
    /// </summary>
    /// <param name="step"></param>
    /// <param name="loopNumber"></param>
    private void PrintScriptStep(Step step, long loopNumber)
    {
        if (step.Event != null)
        {
            LogScriptStepEvent(loopNumber, step.Event.AlarmId, step.Event.Reason);
            foreach (var sc in step.Event.StateChanges)
            {
                LogStateChange(sc.StateType.ToString(), sc.State);
            }
        }

        if (step.SleepInSeconds > 0)
        {
            LogScriptSleep(loopNumber, step.SleepInSeconds);
        }
    }

    #region CustomNodeManager2 overrides

    /// <summary>
    /// Creates a new set of monitored items for a set of variables.
    /// </summary>
    /// <remarks>
    /// This method only handles data change subscriptions. Event subscriptions are created by the SDK.
    /// </remarks>
    public override void CreateMonitoredItems(
        OperationContext context,
        uint subscriptionId,
        double publishingInterval,
        TimestampsToReturn timestampsToReturn,
        IList<MonitoredItemCreateRequest> itemsToCreate,
        IList<ServiceResult> errors,
        IList<MonitoringFilterResult> filterErrors,
        IList<IMonitoredItem> monitoredItems,
        bool createDurable,
        ref long globalIdCounter)
    {
        ServerSystemContext systemContext = _defaultSystemContext.Copy(context);
        IDictionary<NodeId, NodeState> operationCache = new NodeIdDictionary<NodeState>();
        List<NodeHandle> nodesToValidate = new();
        List<IMonitoredItem> createdItems = new();

        lock (Lock)
        {
            for (int ii = 0; ii < itemsToCreate.Count; ii++)
            {
                MonitoredItemCreateRequest monitoredItemCreateRequest = itemsToCreate[ii];

                // skip items that have already been processed.
                if (monitoredItemCreateRequest.Processed)
                {
                    continue;
                }

                ReadValueId itemToMonitor = monitoredItemCreateRequest.ItemToMonitor;

                // check for valid handle.
                NodeHandle handle = GetManagerHandle(systemContext, itemToMonitor.NodeId, operationCache);

                if (handle == null)
                {
                    continue;
                }

                // owned by this node manager.
                monitoredItemCreateRequest.Processed = true;

                // must validate node in a separate operation.
                errors[ii] = StatusCodes.BadNodeIdUnknown;

                handle.Index = ii;
                nodesToValidate.Add(handle);
            }

            // check for nothing to do.
            if (nodesToValidate.Count == 0)
            {
                return;
            }
        }

        // validates the nodes (reads values from the underlying data source if required).
        for (int ii = 0; ii < nodesToValidate.Count; ii++)
        {
            NodeHandle handle = nodesToValidate[ii];

            MonitoringFilterResult filterResult = null;
            IMonitoredItem monitoredItem = null;

            lock (Lock)
            {
                // validate node.
                NodeState source = ValidateNode(systemContext, handle, operationCache);

                if (source == null)
                {
                    continue;
                }

                MonitoredItemCreateRequest itemToCreate = itemsToCreate[handle.Index];

                // create monitored item.
                errors[handle.Index] = CreateMonitoredItem(
                    systemContext,
                    handle,
                    subscriptionId,
                    publishingInterval,
                    context.DiagnosticsMask,
                    timestampsToReturn,
                    itemToCreate,
                    createDurable,
                    ref globalIdCounter,
                    out filterResult,
                    out monitoredItem);
            }

            // save any filter error details.
            filterErrors[handle.Index] = filterResult;

            if (ServiceResult.IsBad(errors[handle.Index]))
            {
                continue;
            }

            // save the monitored item.
            monitoredItems[handle.Index] = monitoredItem;
            createdItems.Add(monitoredItem);
        }

        // do any post processing.
        OnCreateMonitoredItemsComplete(systemContext, createdItems);
    }

    /// <summary>
    /// Verifies that the specified node exists.
    /// </summary>
    protected override NodeState ValidateNode(
        ServerSystemContext context,
        NodeHandle handle,
        IDictionary<NodeId, NodeState> cache)
    {
        // lookup in cache.
        NodeState target = FindNodeInCache(context, handle, cache);

        if (target != null)
        {
            handle.Node = target;
            handle.Validated = true;
            return handle.Node;
        }

        // return default.
        return handle.Node;
    }

    /// <summary>
    /// Subscribes or unsubscribes to events produced by all event sources.
    /// </summary>
    /// <remarks>
    /// This method is called when a event subscription is created or deleted. The node
    /// manager must start/stop reporting events for all objects that it manages.
    /// </remarks>
    public override ServiceResult SubscribeToAllEvents(
        OperationContext context,
        uint subscriptionId,
        IEventMonitoredItem monitoredItem,
        bool unsubscribe)
    {
        ServerSystemContext serverSystemContext = SystemContext.Copy(context);

        lock (Lock)
        {
            // A client has subscribed to the Server object which means all events produced
            // by this manager must be reported. This is done by incrementing the monitoring
            // reference count for all root notifiers.
            if (_rootNotifiers != null)
            {
                for (int ii = 0; ii < _rootNotifiers.Count; ii++)
                {
                    SubscribeToEvents(serverSystemContext, _rootNotifiers[ii], monitoredItem, unsubscribe);
                }
            }

            return ServiceResult.Good;
        }
    }

    /// <summary>
    /// Subscribes to events.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="source">The source.</param>
    /// <param name="monitoredItem">The monitored item.</param>
    /// <param name="unsubscribe">if set to <c>true</c> [unsubscribe].</param>
    /// <returns>Any error code.</returns>
    protected override ServiceResult SubscribeToEvents(
        ServerSystemContext context,
        NodeState source,
        IEventMonitoredItem monitoredItem,
        bool unsubscribe)
    {
        // handle unsubscribe.
        if (unsubscribe)
        {
            // check for existing monitored node.
            if (!MonitoredNodes.TryGetValue(source.NodeId, out MonitoredNode2 monitoredNode2))
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            monitoredNode2.Remove(monitoredItem);

            // check if node is no longer being monitored.
            if (!monitoredNode2.HasMonitoredItems)
            {
                MonitoredNodes.Remove(source.NodeId);
            }

            // update flag.
            source.SetAreEventsMonitored(context, !unsubscribe, true);

            // call subclass.
            OnSubscribeToEvents(context, monitoredNode2, unsubscribe);

            // all done.
            return ServiceResult.Good;
        }

        // only objects or views can be subscribed to.

        if ((source is not BaseObjectState instance || (instance.EventNotifier & EventNotifiers.SubscribeToEvents) == 0) &&
            (source is not ViewState view || (view.EventNotifier & EventNotifiers.SubscribeToEvents) == 0))
        {
            return StatusCodes.BadNotSupported;
        }

        // check for existing monitored node.
        if (!MonitoredNodes.TryGetValue(source.NodeId, out MonitoredNode2 monitoredNode))
        {
            MonitoredNodes[source.NodeId] = monitoredNode = new MonitoredNode2(this, source);
        }

        // this links the node to specified monitored item and ensures all events
        // reported by the node are added to the monitored item's queue.
        monitoredNode.Add(monitoredItem);

        // This call recursively updates a reference count all nodes in the notifier
        // hierarchy below the area. Sources with a reference count of 0 do not have
        // any active subscriptions so they do not need to report events.
        source.SetAreEventsMonitored(context, !unsubscribe, true);

        // signal update.
        OnSubscribeToEvents(context, monitoredNode, unsubscribe);

        // all done.
        return ServiceResult.Good;
    }

    /// <summary>
    /// Does any initialization required before the address space can be used.
    /// </summary>
    /// <remarks>
    /// The externalReferences is an out parameter that allows the node manager to link to nodes
    /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
    /// should have a reference to the root folder node(s) exposed by this node manager.
    /// </remarks>
    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            if (!externalReferences.TryGetValue(ObjectIds.Server, out IList<IReference> references))
            {
                externalReferences[ObjectIds.Server] = references = new List<IReference>();
            }

            // Folders Nodes
            foreach (var folder in _scriptConfiguration.Folders)
            {
                var simFolderState = new SimFolderState(SystemContext, null, new NodeId(folder.Name, NamespaceIndex), folder.Name);
                simFolderState.AddReference(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server);
                AddRootNotifier(simFolderState);
                _folders.Add(simFolderState);

                // Source Nodes
                foreach (var source in folder.Sources)
                {
                    SimSourceNodeState simSourceNodeState;
                    _sourceNodes[source.Name] = simSourceNodeState =
                        new SimSourceNodeState(this, new NodeId(source.Name, NamespaceIndex), source.Name, source.Alarms);

                    simFolderState.AddChild(simSourceNodeState);

                    simSourceNodeState.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, true, simFolderState);
                    simFolderState.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, false, simSourceNodeState);
                }

                references.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, simFolderState.NodeId));

                AddPredefinedNode(SystemContext, simFolderState);
            }
        }

        ReplayScriptStart(_scriptConfiguration);
    }

    /// <summary>
    /// Tells the node manager to refresh any conditions associated with the specified monitored items.
    /// </summary>
    /// <remarks>
    /// This method is called when the condition refresh method is called for a subscription.
    /// The node manager must create a refresh event for each condition monitored by the subscription.
    /// </remarks>
    public override ServiceResult ConditionRefresh(
        OperationContext context,
        IList<IEventMonitoredItem> monitoredItems)
    {
        foreach (var monitoredItem in monitoredItems.Cast<MonitoredItem>())
        {
            if (monitoredItem == null)
            {
                continue;
            }

            List<IFilterTarget> events = new();
            List<NodeState> nodesToRefresh = new();

            lock (Lock)
            {
                // check for server subscription.
                if (monitoredItem.NodeId == ObjectIds.Server)
                {
                    if (_rootNotifiers != null)
                    {
                        nodesToRefresh.AddRange(_rootNotifiers);
                    }
                }
                else
                {
                    if (!MonitoredNodes.TryGetValue(monitoredItem.NodeId, out MonitoredNode2 monitoredNode))
                    {
                        continue;
                    }

                    // get the refresh events.
                    nodesToRefresh.Add(monitoredNode.Node);
                }
            }

            foreach (var node in nodesToRefresh)
            {
                node.ConditionRefresh(SystemContext, events, true);
            }

            foreach (var @event in events)
            {
                monitoredItem.QueueEvent(@event);
            }
        }

        return ServiceResult.Good;
    }

    /// <summary>
    /// Adds a root notifier.
    /// </summary>
    /// <param name="notifier">The notifier.</param>
    /// <remarks>
    /// A root notifier is a notifier owned by the NodeManager that is not the target of a
    /// HasNotifier reference. These nodes need to be linked directly to the Server object.
    /// </remarks>
    protected override void AddRootNotifier(NodeState notifier)
    {
        if (_rootNotifiers == null)
        {
            _rootNotifiers = new List<NodeState>();
        }

        for (int ii = 0; ii < _rootNotifiers.Count; ii++)
        {
            if (ReferenceEquals(notifier, _rootNotifiers[ii]))
            {
                return;
            }
        }

        _rootNotifiers.Add(notifier);

        // need to prevent recursion with the server object.
        if (notifier.NodeId != ObjectIds.Server)
        {
            notifier.OnReportEvent = OnReportEvent;

            if (!notifier.ReferenceExists(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server))
            {
                notifier.AddReference(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server);
            }
        }

        // subscribe to existing events.
        if (_server.EventManager != null)
        {
            IList<IEventMonitoredItem> monitoredItems = _server.EventManager.GetMonitoredItems();

            for (int ii = 0; ii < monitoredItems.Count; ii++)
            {
                if (monitoredItems[ii].MonitoringAllEvents)
                {
                    SubscribeToEvents(
                        SystemContext,
                        notifier,
                        monitoredItems[ii],
                        true);
                }
            }
        }
    }

    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="node">The node.</param>
    /// <returns>The new NodeId.</returns>
    /// <remarks>
    /// This method is called by the NodeState.Create() method which initializes a Node from
    /// the type model. During initialization a number of child nodes are created and need to
    /// have NodeIds assigned to them. This implementation constructs NodeIds by constructing
    /// strings. Other implementations could assign unique integers or Guids and save the new
    /// Node in a dictionary for later lookup.
    /// </remarks>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        return new NodeId(++_nodeIdCounter, NamespaceIndex);
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        NodeStateCollection predefinedNodes = new();

        return predefinedNodes;
    }
    #endregion

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Cannot read or decode deterministic alarm script file")]
    partial void LogCannotReadOrDecodeScriptFile(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Script starts executing")]
    partial void LogScriptStartsExecuting();

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Script Engine Exception {Message}\nSCRIPT WILL NOT START")]
    partial void LogScriptEngineException(string message, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SCRIPT ENDED")]
    partial void LogScriptEnded();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "({LoopNumber}) -\t{AlarmId}\t{Reason}")]
    partial void LogScriptStepEvent(long loopNumber, string alarmId, string reason);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "\t\t{StateType} - {State}")]
    partial void LogStateChange(string stateType, bool state);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "({LoopNumber}) -\tSleep: {SleepInSeconds}")]
    partial void LogScriptSleep(long loopNumber, int SleepInSeconds);
}
