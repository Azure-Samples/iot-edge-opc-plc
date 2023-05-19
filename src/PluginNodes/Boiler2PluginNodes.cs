namespace OpcPlc.PluginNodes;

using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Boiler node that inherits from DI.
/// </summary>
public class Boiler2PluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    ////private BoilerState _node;
    private float _tempSpeedDegrees = 1.0f;
    private float _baseTempDegrees = 10.0f;
    private float _targetTempDegrees = 80.0f;
    private uint _maintenanceIntervalMinutes = 60;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        _isEnabled = true;

        optionSet.Add(
            "b2ts|boiler2tempspeed=",
            $"Boiler #2 temperature change speed in degree per seconds\nDefault: {_tempSpeedDegrees}",
            (string s) => _tempSpeedDegrees = CliHelper.ParseFloat(s, min: 1.0f, max: 10.0f, optionName: "boiler2tempspeed", digits: 1));

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
            $"Boiler #2 required maintenance interval in minutes\nDefault: {_maintenanceIntervalMinutes}",
            (string s) => _maintenanceIntervalMinutes = (uint)CliHelper.ParseInt(s, min: 1, max: int.MaxValue, optionName: "boiler2maintint"));

        //Temperature change speed in degree per seconds (float with 1 decimal place, [1.0, 1.1, ..., 9.9, 10.0], read/write, default: 1.0): Configures heater power
        //Base temperature(float, [1.0, ..., 10.0, ...], write, default: 10.0): Temperature to reach when not heating
        //Target temperature(float, [Base_temp + 10.0, ..., 80.0, ...], read / write, default: 80): Temperature to reach when heating
        //Maintenance interval(integer, [1, ..., 60, ...], read / write, default: 60): Interval system requires maintenance in minutes
        //Overheated threshold temperature(float, Target_temp + 10, read)
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            AddNodes(methodsFolder);
        }
    }

    public void StartSimulation()
    {
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);
    }

    public void StopSimulation()
    {
    }

    private void AddNodes(FolderState methodsFolder)
    {
        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

        // Find the Boiler2 configuration nodes.
        var tempSpeedDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        var baseTempNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        var targetTempNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TargetTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        var maintenanceIntervalNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));
        var overheatThresholdDegreesNode = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseDataVariableState));

        SetValue(tempSpeedDegreesNode, _tempSpeedDegrees);
        SetValue(baseTempNode, _baseTempDegrees);
        SetValue(targetTempNode, _targetTempDegrees);
        SetValue(maintenanceIntervalNode, _maintenanceIntervalMinutes);
        SetValue(overheatThresholdDegreesNode, _targetTempDegrees + 10.0);

        // Convert to node that can be manipulated within the server.
        var node1 = new BaseDataVariableState<float>(null);
        node1.Create(_plcNodeManager.SystemContext, tempSpeedDegreesNode);
        ////_node = new BoilerState(null);
        ////_node.Create(_plcNodeManager.SystemContext, passiveNode);

        _plcNodeManager.AddPredefinedNode(node1);
        _plcNodeManager.AddPredefinedNode(baseTempNode);
        _plcNodeManager.AddPredefinedNode(targetTempNode);
        _plcNodeManager.AddPredefinedNode(maintenanceIntervalNode);
        _plcNodeManager.AddPredefinedNode(overheatThresholdDegreesNode);
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
        variable.StatusCode = StatusCodes.Good;
        variable.Value = value;
        variable.Timestamp = Program.TimeService.Now();
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }
}
