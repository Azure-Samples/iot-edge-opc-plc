namespace OpcPlc.PluginNodes;

using Opc.Ua;
using Opc.Ua.DI;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using static OpcPlc.Program;

/// <summary>
/// Boiler that inherits from DI companion spec.
/// </summary>
public class Boiler2PluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private PlcNodeManager _plcNodeManager;
    private BaseDataVariableState _tempSpeedDegreesPerSecNode;
    private BaseDataVariableState _baseTempDegreesNode;
    private BaseDataVariableState _targetTempDegreesNode;
    private BaseDataVariableState _maintenanceIntervalSecondsNode;
    private BaseDataVariableState _overheatIntervalSecondsNode;
    private BaseDataVariableState _overheatThresholdDegreesNode;
    private BaseDataVariableState _currentTempDegreesNode;
    private BaseDataVariableState _overheatedNode;
    private BaseDataVariableState _heaterStateNode;
    private BaseDataVariableState _deviceHealth;
    private ITimer _nodeGenerator;

    private float _tempSpeedDegreesPerSec = 1.0f;
    private float _baseTempDegrees = 10.0f;
    private float _targetTempDegrees = 80.0f;
    private uint _maintenanceIntervalSeconds = 60;
    private uint _overheatIntervalSeconds = 60;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "b2ts|boiler2tempspeedboiler2tempspeed=",
            $"Boiler #2 temperature change speed in degrees per second\nDefault: {_tempSpeedDegreesPerSec}",
            (string s) => _tempSpeedDegreesPerSec = CliHelper.ParseFloat(s, min: 1.0f, max: 10.0f, optionName: "boiler2tempspeed", digits: 1));

        optionSet.Add(
            "b2bt|boiler2basetemp=",
            $"Boiler #2 base temperature to reach when not heating\nDefault: {_baseTempDegrees}",
            (string s) => _baseTempDegrees = CliHelper.ParseFloat(s, min: 1.0f, max: float.MaxValue, optionName: "boiler2basetemp", digits: 1));

        optionSet.Add(
            "b2tt|boiler2targettemp=",
            $"Boiler #2 target temperature to reach when heating\nDefault: {_targetTempDegrees}",
            (string s) => _targetTempDegrees = CliHelper.ParseFloat(s, min: _baseTempDegrees + 10.0f, max: float.MaxValue, optionName: "boiler2targettemp", digits: 1));

        optionSet.Add(
            "b2mi|boiler2maintinterval=",
            $"Boiler #2 required maintenance interval in seconds\nDefault: {_maintenanceIntervalSeconds}",
            (string s) => _maintenanceIntervalSeconds = (uint)CliHelper.ParseInt(s, min: 1, max: int.MaxValue, optionName: "boiler2maintinterval"));

        optionSet.Add(
            "b2oi|boiler2overheatinterval=",
            $"Boiler #2 overheat interval in seconds\nDefault: {_overheatIntervalSeconds}",
            (string s) => _overheatIntervalSeconds = (uint)CliHelper.ParseInt(s, min: 1, max: int.MaxValue, optionName: "boiler2overheatinterval"));
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        // Check again if targetTemp is within range, because the minimum uses baseTemp as lower bound and
        // the order in which the CLI options are specified affects the calculation.
        _ = CliHelper.ParseFloat(_targetTempDegrees.ToString(), min: _baseTempDegrees + 10.0f, max: float.MaxValue, optionName: "boiler2targettemp", digits: 1);

        _plcNodeManager = plcNodeManager;

        AddNodes();
    }

    public void StartSimulation()
    {
        _nodeGenerator = TimeService.NewTimer(UpdateBoiler2, intervalInMilliseconds: 1000);
    }

    public void StopSimulation()
    {
        if (_nodeGenerator != null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes()
    {
        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

        // Find the Boiler2 configuration nodes.
        _tempSpeedDegreesPerSecNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _baseTempDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _targetTempDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TargetTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _maintenanceIntervalSecondsNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _overheatIntervalSecondsNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _overheatThresholdDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));

        SetValue(_tempSpeedDegreesPerSecNode, _tempSpeedDegreesPerSec);
        SetValue(_baseTempDegreesNode, _baseTempDegrees);
        SetValue(_targetTempDegreesNode, _targetTempDegrees);
        SetValue(_maintenanceIntervalSecondsNode, _maintenanceIntervalSeconds);
        SetValue(_overheatIntervalSecondsNode, _overheatIntervalSeconds);
        SetValue(_overheatThresholdDegreesNode, _targetTempDegrees + 10.0f);

        // Find the Boiler2 data nodes.
        _currentTempDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _overheatedNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_Overheated, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        _heaterStateNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));

        SetValue(_heaterStateNode, true);

        // Find the Boiler2 deviceHealth nodes.
        _deviceHealth = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        SetValue(_deviceHealth, DeviceHealthEnumeration.NORMAL);

        AddMethods();
        AddEvents();

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(_currentTempDegreesNode.NodeId, _plcNodeManager),
        };
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinaryResource(context,
            "Boilers/Boiler2/BoilerModel2.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
            typeof(PlcNodeManager).GetTypeInfo().Assembly,
            updateTables: true);

        return predefinedNodes;
    }

    private void SetValue<T>(BaseVariableState variable, T value)
    {
        variable.Value = value;
        variable.Timestamp = TimeService.Now();
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    public void UpdateBoiler2(object state, ElapsedEventArgs elapsedEventArgs)
    {
        float currentTemperature = (float)_currentTempDegreesNode.Value;
        float newTemperature;
        float tempSpeedDegreesPerSec = (float)_tempSpeedDegreesPerSecNode.Value;
        float baseTempDegrees = (float)_baseTempDegreesNode.Value;
        float targetTempDegrees = (float)_targetTempDegreesNode.Value;
        float overheatThresholdDegrees = (float)_overheatThresholdDegreesNode.Value;

        if ((bool)_heaterStateNode.Value)
        {
            // Heater on, increase by specified speed.
            newTemperature = Math.Min(currentTemperature + tempSpeedDegreesPerSec, targetTempDegrees);

            // Target temp reached, turn off heater.
            if (newTemperature == targetTempDegrees)
            {
                SetValue(_heaterStateNode, false);
            }
        }
        else
        {
            // Heater off, decrease by specified speed to a minimum of baseTemp.
            newTemperature = Math.Max(baseTempDegrees, currentTemperature - tempSpeedDegreesPerSec);

            // Base temp reached, turn on heater.
            if (newTemperature == baseTempDegrees)
            {
                SetValue(_heaterStateNode, true);
            }
        }

        // Change other values.
        SetValue(_currentTempDegreesNode, newTemperature);
        SetValue(_overheatedNode, newTemperature > overheatThresholdDegrees);

        // Update DeviceHealth status.
        SetDeviceHealth(currentTemperature, baseTempDegrees, targetTempDegrees, overheatThresholdDegrees);

        // TODO:
        // The simulation should inject a problem every couple of minutes which will increase the Current_temp to 10 degrees over Overheated_temp, switch off the Heater and:
        // - Will emit a "CheckFunctionAlarmType" event, when DeviceHealth updates to CHECK_FUNCTION
        // - Will emit a "FailureAlarmType" event, when DeviceHealth updates to FAILURE
        // - Will emit an "OffSpecAlarmType" event, when DeviceHealth updates to OFF_SPEC
        // - Will emit a "MaintenanceRequiredAlarmType", when DeviceHealth updates to MAINTENANCE_REQUIRED

        // TODO: Add maintenance required event using (int)_maintenanceIntervalMinutesNode.Value.
    }

    private void AddMethods()
    {
        MethodState switchMethodNode = (MethodState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Methods.Boilers_Boiler__2_MethodSet_Switch, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(MethodState));

        switchMethodNode.OnCallMethod += SwitchOnCall;
    }

    /// <summary>
    /// Set the heater on/off. Executes synchronously.
    /// </summary>
    private ServiceResult SwitchOnCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        SetValue(_heaterStateNode, inputArguments.First());
        Logger.Debug($"SwitchOnCall method called with argument: {inputArguments.First()}");

        return ServiceResult.Good;
    }

    private void AddEvents()
    {
        // Construct the events.
        var failureEv = new DeviceHealthDiagnosticAlarmTypeState(parent: null);
        var checkFunctionEv = new DeviceHealthDiagnosticAlarmTypeState(parent: null);
        var offSpecEv = new DeviceHealthDiagnosticAlarmTypeState(parent: null);
        var MaintenanceRequiredEv = new DeviceHealthDiagnosticAlarmTypeState(parent: null);

        // Init the events
        failureEv.Initialize(_plcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.Max,
                    new LocalizedText($"Failure Alarm."));

        checkFunctionEv.Initialize(_plcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.Low,
                    new LocalizedText($"CheckFunctionAlarm."));

        offSpecEv.Initialize(_plcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.MediumLow,
                    new LocalizedText($"OffSpecAlarm."));

        MaintenanceRequiredEv.Initialize(_plcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.Medium,
                    new LocalizedText($"MaintenanceRequiredAlarm."));
    }

    private void SetDeviceHealth(float currentTemp, float baseTemp, float targetTemp, float overheatedTemp)
    {
        if (currentTemp > baseTemp && currentTemp < targetTemp)
        {
            SetValue(_deviceHealth, DeviceHealthEnumeration.NORMAL);
        }
        else if (currentTemp < baseTemp || currentTemp > overheatedTemp + 5)
        {
            SetValue(_deviceHealth, DeviceHealthEnumeration.OFF_SPEC);
        }
        else if (currentTemp > overheatedTemp)
        {
            SetValue(_deviceHealth, DeviceHealthEnumeration.FAILURE);
        }
        else if (currentTemp > targetTemp && currentTemp < overheatedTemp)
        {
            SetValue(_deviceHealth, DeviceHealthEnumeration.CHECK_FUNCTION);
        }
    }
}
