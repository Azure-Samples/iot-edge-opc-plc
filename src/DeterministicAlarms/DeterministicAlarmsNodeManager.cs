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
using System.Threading;
using System.Threading.Tasks;

public partial class DeterministicAlarmsNodeManager : AsyncCustomNodeManager
{
    internal object SyncRoot { get; } = new();

    private readonly SimBackendService _system;
    private readonly List<SimFolderState> _folders = new();
    private uint _nodeIdCounter;
    private readonly Dictionary<string, SimSourceNodeState> _sourceNodes = new();
    private readonly Configuration.Configuration _scriptConfiguration;
    private readonly TimeService _timeService;
    private readonly ILogger _logger;
    private Dictionary<string, string> _scriptAlarmToSources;
    private ScriptEngine _scriptEngine;

    /// <summary>
    /// Initializes the node manager.
    /// </summary>
    public DeterministicAlarmsNodeManager(IServerInternal server, ApplicationConfiguration configuration, TimeService timeService, string scriptFileName, ILogger logger) : base(server, configuration)
    {
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
            _scriptEngine = new ScriptEngine(scriptConfiguration.Script, OnScriptStepAvailable, _timeService);
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
        lock (SyncRoot)
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

    #region AsyncCustomNodeManager overrides

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref _scriptEngine, null)?.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DeleteAddressSpaceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ScriptEngine engine = Interlocked.Exchange(ref _scriptEngine, null);
        if (engine is not null)
        {
            await engine.DisposeAsync().ConfigureAwait(false);
        }
        await base.DeleteAddressSpaceAsync(cancellationToken).ConfigureAwait(false);
        lock (SyncRoot)
        {
            _folders.Clear();
            _sourceNodes.Clear();
            _system.SourceNodes.Clear();
            _scriptAlarmToSources?.Clear();
        }
    }





    /// <summary>
    /// Does any initialization required before the address space can be used.
    /// </summary>
    /// <remarks>
    /// The externalReferences is an out parameter that allows the node manager to link to nodes
    /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
    /// should have a reference to the root folder node(s) exposed by this node manager.
    /// </remarks>
    public override async ValueTask CreateAddressSpaceAsync(
        IDictionary<NodeId, IList<IReference>> externalReferences,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!externalReferences.TryGetValue(ObjectIds.Server, out IList<IReference> references))
        {
            externalReferences[ObjectIds.Server] = references = new List<IReference>();
        }

        // Folders Nodes
        foreach (var folder in _scriptConfiguration.Folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SimFolderState simFolderState;
            lock (SyncRoot)
            {
                simFolderState = new SimFolderState(SystemContext, null, new NodeId(folder.Name, NamespaceIndex), folder.Name);
                simFolderState.AddReference(ReferenceTypeIds.HasNotifier, true, ObjectIds.Server);
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
            }

            await AddRootNotifierAsync(simFolderState, cancellationToken).ConfigureAwait(false);
            references.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, simFolderState.NodeId));
            await AddPredefinedNodeAsync(SystemContext, simFolderState, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReplayScriptStart(_scriptConfiguration);
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
