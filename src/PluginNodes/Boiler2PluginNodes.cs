namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DeviceHealthEnumeration = OpcPlc.RuntimeModelIds.Di.DeviceHealth;

/// <summary>
/// Boiler that inherits from DI companion spec.
/// </summary>
public partial class Boiler2PluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _plcNodeManager;
    private BaseDataVariableState _tempSpeedDegreesPerSecNode;
    private BaseDataVariableState _baseTempDegreesNode;
    private BaseDataVariableState _targetTempDegreesNode;
    private BaseDataVariableState _overheatThresholdDegreesNode;
    private BaseDataVariableState _maintenanceIntervalInSecondsNode;
    private BaseDataVariableState _overheatIntervalInSecondsNode;
    private BaseDataVariableState _currentTempDegreesNode;
    private BaseDataVariableState _overheatedNode;
    private BaseDataVariableState _heaterStateNode;
    private BaseDataVariableState _deviceHealth;
    private OffNormalAlarmState _failureEv;
    private OffNormalAlarmState _checkFunctionEv;
    private OffNormalAlarmState _offSpecEv;
    private OffNormalAlarmState _maintenanceRequiredEv;
    private OpcPlc.ITimer _nodeGenerator;
    private OpcPlc.ITimer _maintenanceGenerator;
    private OpcPlc.ITimer _overheatGenerator;
    private BaseObjectState _boiler2Object;

    private float _tempSpeedDegreesPerSec = 1.0f;
    private float _baseTempDegrees = 10.0f;
    private float _targetTempDegrees = 80.0f;
    private TimeSpan _maintenanceInterval = TimeSpan.FromSeconds(300); // 5 min.
    private TimeSpan _overheatInterval = TimeSpan.FromSeconds(120); // 2 min.

    private bool _isOverheated;
    private volatile bool _stopped;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly object _callbacksLock = new();
    private int _activeCallbacks;
    private TaskCompletionSource _callbacksDrained;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "b2ts|boiler2tempspeed=",
            $"Boiler #2 temperature change speed in degrees per second.\nDefault: {_tempSpeedDegreesPerSec}",
            (string s) => _tempSpeedDegreesPerSec = CliHelper.ParseFloat(s, min: 1.0f, max: 10.0f, optionName: "boiler2tempspeed", digits: 1));

        optionSet.Add(
            "b2bt|boiler2basetemp=",
            $"Boiler #2 base temperature to reach when not heating.\nDefault: {_baseTempDegrees}",
            (string s) => _baseTempDegrees = CliHelper.ParseFloat(s, min: 1.0f, max: float.MaxValue, optionName: "boiler2basetemp", digits: 1));

        optionSet.Add(
            "b2tt|boiler2targettemp=",
            $"Boiler #2 target temperature to reach when heating.\nDefault: {_targetTempDegrees}",
            (string s) => _targetTempDegrees = CliHelper.ParseFloat(s, min: _baseTempDegrees + 10.0f, max: float.MaxValue, optionName: "boiler2targettemp", digits: 1));

        optionSet.Add(
            "b2mi|boiler2maintinterval=",
            $"Boiler #2 required maintenance interval in seconds.\nDefault: {_maintenanceInterval.TotalSeconds}",
            (string s) => _maintenanceInterval = TimeSpan.FromSeconds(CliHelper.ParseInt(s, min: 1, max: int.MaxValue, optionName: "boiler2maintinterval")));

        optionSet.Add(
            "b2oi|boiler2overheatinterval=",
            $"Boiler #2 overheat interval in seconds.\nDefault: {_overheatInterval.TotalSeconds}",
            (string s) => _overheatInterval = TimeSpan.FromSeconds(CliHelper.ParseInt(s, min: 1, max: int.MaxValue, optionName: "boiler2overheatinterval")));
    }

    public async ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Check again if targetTemp is within range, because the minimum uses baseTemp as lower bound and
        // the order in which the CLI options are specified affects the calculation.
        _ = CliHelper.ParseFloat(_targetTempDegrees.ToString(), min: _baseTempDegrees + 10.0f, max: float.MaxValue, optionName: "boiler2targettemp", digits: 1);

        _plcNodeManager = plcNodeManager;

        await AddNodesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void StartSimulation()
    {
        lock (_callbacksLock)
        {
            if (_activeCallbacks != 0)
            {
                throw new InvalidOperationException("Previous Boiler2 callbacks have not drained.");
            }
            _callbacksDrained = null;
            _stopped = false;
        }
        _nodeGenerator = _timeService.NewTimer(UpdateBoiler2, intervalInMilliseconds: 1000);
        StartTimers();
    }

    public void StopSimulation()
    {
        _stopped = true;

        if (_nodeGenerator is not null)
        {
            _nodeGenerator.Enabled = false;
        }

        if (_maintenanceGenerator is not null)
        {
            _maintenanceGenerator.Enabled = false;
        }

        if (_overheatGenerator is not null)
        {
            _overheatGenerator.Enabled = false;
        }
    }

    public ValueTask DrainSimulationAsync()
    {
        lock (_callbacksLock)
        {
            if (_activeCallbacks == 0)
            {
                return ValueTask.CompletedTask;
            }
            _callbacksDrained ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return new ValueTask(_callbacksDrained.Task);
        }
    }

    private async ValueTask AddNodesAsync(CancellationToken cancellationToken)
    {
        await _plcNodeManager.LoadPredefinedNodesAsync(LoadPredefinedNodes, cancellationToken).ConfigureAwait(false);

        // Locate Boiler #2 object itself.
        _boiler2Object = _plcNodeManager.FindPredefinedNode<BaseObjectState>(new NodeId(5017, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        if (_boiler2Object != null)
        {
            _boiler2Object.EventNotifier = EventNotifiers.SubscribeToEvents;
            // Set the event handler that routes events through the subscription system.
            _boiler2Object.OnReportEvent = OnReportBoilerEvent;

            // Boiler #2 derives from the DI DeviceType, so additionally link it under the DI
            // DeviceSet folder for standard device discovery. This adds an extra Organizes
            // reference only; the boiler keeps its primary location in the Boilers folder and its
            // NodeId/namespace are unchanged.
            _plcNodeManager.LinkNodeToDeviceSet(_boiler2Object);
        }

        // Find the Boiler2 configuration nodes.
        _tempSpeedDegreesPerSecNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _baseTempDegreesNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _targetTempDegreesNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_TargetTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _maintenanceIntervalInSecondsNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _overheatIntervalInSecondsNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _overheatThresholdDegreesNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));

        AllowReadAndWrite(_overheatIntervalInSecondsNode);
        AllowReadAndWrite(_overheatThresholdDegreesNode);
        AllowReadAndWrite(_maintenanceIntervalInSecondsNode);

        // Find and enable write access to device properties for testing.
        var assetIdNode = _plcNodeManager.FindPredefinedNode<PropertyState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_AssetId, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        var deviceManualNode = _plcNodeManager.FindPredefinedNode<PropertyState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_DeviceManual, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));

        if (assetIdNode is not null)
        {
            AllowReadAndWrite(assetIdNode);
        }

        if (deviceManualNode is not null)
        {
            AllowReadAndWrite(deviceManualNode);
        }

        SetValue(_tempSpeedDegreesPerSecNode, _tempSpeedDegreesPerSec);
        SetValue(_baseTempDegreesNode, _baseTempDegrees);
        SetValue(_targetTempDegreesNode, _targetTempDegrees);
        SetValue(_maintenanceIntervalInSecondsNode, (uint)_maintenanceInterval.TotalSeconds);
        SetValue(_overheatIntervalInSecondsNode, (uint)_overheatInterval.TotalSeconds);
        SetValue(_overheatThresholdDegreesNode, _targetTempDegrees + 10.0f);

        _maintenanceIntervalInSecondsNode.OnSimpleWriteValue = OnWriteMaintenanceIntervalInSeconds;
        _overheatIntervalInSecondsNode.OnSimpleWriteValue = OnWriteOverheatIntervalInSeconds;

        // Find the Boiler2 data nodes.
        _currentTempDegreesNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _overheatedNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_Overheated, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        _heaterStateNode = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));

        SetValue(_currentTempDegreesNode, _baseTempDegrees);
        SetValue(_overheatedNode, false);
        SetValue(_heaterStateNode, true);

        // Find the Boiler2 deviceHealth nodes.
        _deviceHealth = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(new NodeId(RuntimeModelIds.Boiler2.Variables.Boilers_Boiler__2_DeviceHealth, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        SetValue(_deviceHealth, DeviceHealthEnumeration.NORMAL);

        AddMethods();
        InitEvents();

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(_currentTempDegreesNode.NodeId, _plcNodeManager),
        };
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private static NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var xmlPath = "Boilers/Boiler2/BoilerModel2.NodeSet2.xml";
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap
            xmlPath = Path.Join(snapLocation, xmlPath);
        }

        var predefinedNodes = new NodeStateCollection();
        using var stream = File.OpenRead(xmlPath);
        Opc.Ua.Export.UANodeSet.Read(stream).Import(context, predefinedNodes, stateFactory: null);
        Opc.Ua.Export.UANodeSet.LinkParentChildRelationships(
            context, predefinedNodes, new Opc.Ua.Export.NodeSetImportLinkOptions());
        return predefinedNodes;
    }

    private void SetValue<T>(BaseVariableState variable, T value)
    {
        variable.Value = VariantHelper.CastFrom(value);
        variable.Timestamp = _timeService.Now();
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private void AllowReadAndWrite(BaseVariableState variable)
    {
        variable.Timestamp = _timeService.Now();
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private ServiceResult OnWriteMaintenanceIntervalInSeconds(ISystemContext context, NodeState node, ref Variant value)
    {
        _maintenanceInterval = TimeSpan.FromSeconds(value.GetUInt32());
        _maintenanceGenerator?.Dispose();
        _maintenanceGenerator = _timeService.NewTimer(UpdateMaintenance, intervalInMilliseconds: (uint)_maintenanceInterval.TotalMilliseconds);
        return ServiceResult.Good;
    }

    private ServiceResult OnWriteOverheatIntervalInSeconds(ISystemContext context, NodeState node, ref Variant value)
    {
        _overheatInterval = TimeSpan.FromSeconds(value.GetUInt32());
        _overheatGenerator?.Dispose();
        _overheatGenerator = _timeService.NewTimer(UpdateOverheat, intervalInMilliseconds: (uint)_overheatInterval.TotalMilliseconds);
        return ServiceResult.Good;
    }

    public void UpdateBoiler2(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _ = ExecuteTimerCallbackAsync(nameof(UpdateBoiler2), () =>
        {
            float currentTemperatureDegrees = (float)_currentTempDegreesNode.Value;
            float newTemperature;
            float tempSpeedDegreesPerSec = (float)_tempSpeedDegreesPerSecNode.Value;
            float baseTempDegrees = (float)_baseTempDegreesNode.Value;
            float targetTempDegrees = (float)_targetTempDegreesNode.Value;
            float overheatThresholdDegrees = (float)_overheatThresholdDegreesNode.Value;

            if ((bool)_heaterStateNode.Value)
            {
                // Heater on, increase by specified speed, but the step should not be bigger than targetTemp.
                newTemperature = currentTemperatureDegrees + Math.Min(tempSpeedDegreesPerSec, Math.Abs(targetTempDegrees - currentTemperatureDegrees));

                // Target temp reached, turn off heater.
                if (newTemperature >= targetTempDegrees)
                {
                    SetValue(_heaterStateNode, false);
                }
            }
            else
            {
                // Heater off, decrease by specified speed, but the step should not be bigger than baseTemp.
                newTemperature = currentTemperatureDegrees - Math.Min(tempSpeedDegreesPerSec, Math.Abs(currentTemperatureDegrees - baseTempDegrees));

                // Base temp reached, turn on heater.
                if (newTemperature <= baseTempDegrees)
                {
                    SetValue(_heaterStateNode, true);
                }
            }

            // Change other values.
            SetValue(_currentTempDegreesNode, newTemperature);
            SetValue(_overheatedNode, newTemperature > overheatThresholdDegrees);

            // Update DeviceHealth status.
            SetDeviceHealth(newTemperature, baseTempDegrees, targetTempDegrees, overheatThresholdDegrees);

            EmitEvents();
        });
    }

    private void AddMethods()
    {
        MethodState switchMethodNode = _plcNodeManager.FindPredefinedNode<MethodState>(new NodeId(RuntimeModelIds.Boiler2.Methods.Boilers_Boiler__2_MethodSet_Switch, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));

        switchMethodNode.OnCallMethod += SwitchOnCall;
    }

    /// <summary>
    /// Set the heater on/off. Executes synchronously.
    /// </summary>
    private ServiceResult SwitchOnCall(ISystemContext context, MethodState method,
        ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        SetValue(_heaterStateNode, inputArguments[0].GetBoolean());
        LogSwitchOnCallMethodCalled(inputArguments[0].ToString());

        return ServiceResult.Good;
    }

    private void InitEvents()
    {
        // Use the boiler object as the event source so subscriptions on the boiler NodeId receive the events.
        NodeState sourceNodeForEvents = _boiler2Object != null
            ? (NodeState)_boiler2Object
            : _currentTempDegreesNode;

        _failureEv = CreateHealthEvent(sourceNodeForEvents, EventSeverity.Max,
            "Temperature is above or equal to the overheat threshold!");
        _checkFunctionEv = CreateHealthEvent(sourceNodeForEvents, EventSeverity.Low, "Temperature is above target!");
        _offSpecEv = CreateHealthEvent(sourceNodeForEvents, EventSeverity.MediumLow, "Temperature is off spec!");
        _maintenanceRequiredEv = CreateHealthEvent(sourceNodeForEvents, EventSeverity.Medium, "Maintenance required!");

        _maintenanceRequiredEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.SourceName, value: "Maintenance", copy: false);
    }

    private void SetDeviceHealth(float currentTemp, float baseTemp, float targetTemp, float overheatedTemp)
    {
        DeviceHealthEnumeration deviceHealth = currentTemp switch {
            _ when currentTemp >= baseTemp && currentTemp <= targetTemp => DeviceHealthEnumeration.NORMAL,
            _ when currentTemp > targetTemp && currentTemp < overheatedTemp => DeviceHealthEnumeration.CHECK_FUNCTION,
            _ when currentTemp >= overheatedTemp => DeviceHealthEnumeration.FAILURE,
            _ when currentTemp < baseTemp || currentTemp > overheatedTemp + 5 => DeviceHealthEnumeration.OFF_SPEC,
            _ => throw new ArgumentOutOfRangeException(nameof(currentTemp))
        };

        SetValue(_deviceHealth, deviceHealth);
    }

    private void StartTimers()
    {
        _maintenanceGenerator = _timeService.NewTimer(UpdateMaintenance, intervalInMilliseconds: (uint)_maintenanceInterval.TotalMilliseconds);
        _overheatGenerator = _timeService.NewTimer(UpdateOverheat, intervalInMilliseconds: (uint)_overheatInterval.TotalMilliseconds);
    }

    private void UpdateMaintenance(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _ = ExecuteTimerCallbackAsync(nameof(UpdateMaintenance), () =>
        {
            SetValue(_deviceHealth, DeviceHealthEnumeration.MAINTENANCE_REQUIRED);

            _maintenanceRequiredEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.Time, value: DateTimeUtc.Now, copy: false);

            // Report event through the boiler object.
            _boiler2Object?.ReportEvent(_plcNodeManager.SystemContext, _maintenanceRequiredEv);
        });
    }

    private void UpdateOverheat(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _ = ExecuteTimerCallbackAsync(nameof(UpdateOverheat), () =>
        {
            SetValue(_currentTempDegreesNode, (float)_overheatThresholdDegreesNode.Value + 10.0f);
            SetValue(_heaterStateNode, false);
            SetValue(_deviceHealth, DeviceHealthEnumeration.OFF_SPEC);

            _offSpecEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.Time, value: DateTimeUtc.Now, copy: false);

            // Report event through the boiler object.
            _boiler2Object?.ReportEvent(_plcNodeManager.SystemContext, _offSpecEv);

            _isOverheated = true;
        });
    }

    /// <summary>
    /// Serializes timer callbacks without skipping ticks or blocking thread-pool threads:
    /// waiters are queued asynchronously and run once the lock is released.
    /// </summary>
    private async Task ExecuteTimerCallbackAsync(string callbackName, Action callback)
    {
        lock (_callbacksLock)
        {
            if (_stopped)
            {
                return;
            }
            _activeCallbacks++;
        }
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_stopped)
            {
                return;
            }

            callback();
        }
        catch (Exception ex)
        {
            LogTimerCallbackFailed(ex, callbackName);
        }
        finally
        {
            _lock.Release();
            lock (_callbacksLock)
            {
                _activeCallbacks--;
                if (_activeCallbacks == 0)
                {
                    _callbacksDrained?.TrySetResult();
                }
            }
        }
    }

    private void EmitEvents()
    {
        if (_isOverheated)
        {
            switch ((DeviceHealthEnumeration)_deviceHealth.Value.GetInt32())
            {
                case DeviceHealthEnumeration.NORMAL:
                    _isOverheated = false;
                    break;
                case DeviceHealthEnumeration.CHECK_FUNCTION:
                    _checkFunctionEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.Time, value: DateTimeUtc.Now, copy: false);

                    // Report event through the boiler object.
                    _boiler2Object?.ReportEvent(_plcNodeManager.SystemContext, _checkFunctionEv);
                    break;
                case DeviceHealthEnumeration.FAILURE:
                    _failureEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.Time, value: DateTimeUtc.Now, copy: false);

                    // Report event through the boiler object.
                    _boiler2Object?.ReportEvent(_plcNodeManager.SystemContext, _failureEv);
                    break;
            }
        }

        if ((DeviceHealthEnumeration)_deviceHealth.Value.GetInt32() == DeviceHealthEnumeration.OFF_SPEC)
        {
            _offSpecEv.SetChildValue(_plcNodeManager.SystemContext, Opc.Ua.BrowseNames.Time, value: DateTimeUtc.Now, copy: false);

            // Report event through the boiler object.
            _boiler2Object?.ReportEvent(_plcNodeManager.SystemContext, _offSpecEv);
        }
    }

    private void OnReportBoilerEvent(ISystemContext context, NodeState node, IFilterTarget @event)
    {
        _plcNodeManager.Server.ReportEvent(@event);
    }

    private OffNormalAlarmState CreateHealthEvent(NodeState source, EventSeverity severity, string message)
    {
        var alarm = new OffNormalAlarmState(null);
        alarm.Initialize(_plcNodeManager.SystemContext, source, severity, new LocalizedText(message));
        alarm.TypeDefinitionId = NodeId.Create(RuntimeModelIds.Di.ObjectTypes.DeviceHealthDiagnosticAlarmType,
            OpcPlc.Namespaces.DI, _plcNodeManager.Server.NamespaceUris);
        alarm.EventType.Value = alarm.TypeDefinitionId;
        return alarm;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "SwitchOnCall method called with argument: {Argument}")]
    partial void LogSwitchOnCallMethodCalled(string argument);

    [LoggerMessage(Level = LogLevel.Error, Message = "{CallbackName} failed.")]
    partial void LogTimerCallbackFailed(Exception exception, string callbackName);
}
