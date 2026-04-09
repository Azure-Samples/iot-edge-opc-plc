namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Timers;

/// <summary>
/// Stacklight simulation with 3 lamp elements (Red, Yellow, Green) that cycle states.
/// Based on the OPC UA IA companion specification stacklight model.
/// </summary>
public partial class StacklightPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled;
    private PlcNodeManager _plcNodeManager;
    private OpcPlc.ITimer _nodeGenerator;

    // Stacklight properties.
    private BaseDataVariableState _stacklightModeNode;

    // Lamp nodes: [0] = Red, [1] = Yellow, [2] = Green.
    private readonly BaseDataVariableState[] _signalOnNodes = new BaseDataVariableState[3];
    private readonly BaseDataVariableState[] _signalColorNodes = new BaseDataVariableState[3];
    private readonly BaseDataVariableState[] _signalModeNodes = new BaseDataVariableState[3];

    private int _cycleCounter;

    // StacklightOperationMode enum values (from IA spec).
    private const int StacklightModeSegmented = 0;

    // SignalColor enum values (from IA spec).
    private const int SignalColorRed = 1;
    private const int SignalColorGreen = 2;
    private const int SignalColorYellow = 4;

    // SignalModeLight enum values (from IA spec).
    private const int SignalModeContinuous = 0;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "sl|stacklight",
            $"add stacklight simulation to address space.\nDefault: {_isEnabled}",
            (string s) => _isEnabled = s != null);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;
        AddNodes(telemetryFolder);
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _nodeGenerator = _timeService.NewTimer(UpdateStacklight, intervalInMilliseconds: 1000);
        }
    }

    public void StopSimulation()
    {
        if (_nodeGenerator is not null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes(FolderState telemetryFolder)
    {
        FolderState stacklightFolder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Stacklight",
            name: "Stacklight",
            NamespaceType.OpcPlcApplications);

        _stacklightModeNode = _plcNodeManager.CreateBaseVariable(
            stacklightFolder,
            path: "Stacklight_StacklightMode",
            name: "StacklightMode",
            dataType: DataTypeIds.UInt32,
            valueRank: ValueRanks.Scalar,
            accessLevel: AccessLevels.CurrentReadOrWrite,
            description: "Shows how the stacklight unit is used (0=Segmented, 1=Levelmeter, 2=RunningLight, 3=Other).",
            NamespaceType.OpcPlcApplications,
            defaultValue: (uint)StacklightModeSegmented);

        AddLamp(stacklightFolder, 0, "Red", SignalColorRed);
        AddLamp(stacklightFolder, 1, "Yellow", SignalColorYellow);
        AddLamp(stacklightFolder, 2, "Green", SignalColorGreen);

        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[0].NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[1].NodeId, _plcNodeManager),
            PluginNodesHelper.GetNodeWithIntervals(_signalOnNodes[2].NodeId, _plcNodeManager),
        };
    }

    private void AddLamp(FolderState parent, int index, string colorName, int signalColor)
    {
        string lampPath = $"Stacklight_Lamp_{colorName}";

        FolderState lampFolder = _plcNodeManager.CreateFolder(
            parent,
            path: lampPath,
            name: $"Lamp_{colorName}",
            NamespaceType.OpcPlcApplications);

        _signalOnNodes[index] = _plcNodeManager.CreateBaseVariable(
            lampFolder,
            path: $"{lampPath}_SignalOn",
            name: "SignalOn",
            dataType: DataTypeIds.Boolean,
            valueRank: ValueRanks.Scalar,
            accessLevel: AccessLevels.CurrentReadOrWrite,
            description: "Indicates if the lamp is currently switched on.",
            NamespaceType.OpcPlcApplications,
            defaultValue: false);

        _signalColorNodes[index] = _plcNodeManager.CreateBaseVariable(
            lampFolder,
            path: $"{lampPath}_SignalColor",
            name: "SignalColor",
            dataType: DataTypeIds.UInt32,
            valueRank: ValueRanks.Scalar,
            accessLevel: AccessLevels.CurrentRead,
            description: "Colour of the lamp (0=Off, 1=Red, 2=Green, 3=Blue, 4=Yellow, 5=Purple, 6=Cyan, 7=White).",
            NamespaceType.OpcPlcApplications,
            defaultValue: (uint)signalColor);

        _signalModeNodes[index] = _plcNodeManager.CreateBaseVariable(
            lampFolder,
            path: $"{lampPath}_SignalMode",
            name: "SignalMode",
            dataType: DataTypeIds.UInt32,
            valueRank: ValueRanks.Scalar,
            accessLevel: AccessLevels.CurrentReadOrWrite,
            description: "Light mode (0=Continuous, 1=Blinking, 2=Flashing, 3=Other).",
            NamespaceType.OpcPlcApplications,
            defaultValue: (uint)SignalModeContinuous);
    }

    private void UpdateStacklight(object state, ElapsedEventArgs elapsedEventArgs)
    {
        _cycleCounter++;

        // Cycle through steady states every 5 seconds:
        // Phase 0: Green ON
        // Phase 1: Yellow ON
        // Phase 2: Red ON
        int phase = (_cycleCounter / 5) % 3;

        switch (phase)
        {
            case 0: // Green ON
                SetLampState(0, signalOn: false, SignalModeContinuous);
                SetLampState(1, signalOn: false, SignalModeContinuous);
                SetLampState(2, signalOn: true, SignalModeContinuous);
                break;
            case 1: // Yellow ON
                SetLampState(0, signalOn: false, SignalModeContinuous);
                SetLampState(1, signalOn: true, SignalModeContinuous);
                SetLampState(2, signalOn: false, SignalModeContinuous);
                break;
            case 2: // Red ON
                SetLampState(0, signalOn: true, SignalModeContinuous);
                SetLampState(1, signalOn: false, SignalModeContinuous);
                SetLampState(2, signalOn: false, SignalModeContinuous);
                break;
        }
    }

    /// <summary>
    /// Gets the current state of all lamps as a JSON-serializable structure.
    /// </summary>
    public StacklightState GetState()
    {
        if (_signalOnNodes[0] is null)
        {
            return null;
        }

        return new StacklightState
        {
            StacklightMode = (uint)(_stacklightModeNode?.Value ?? 0u),
            Lamps =
            [
                GetLampState(0, "Red"),
                GetLampState(1, "Yellow"),
                GetLampState(2, "Green"),
            ],
        };
    }

    private LampState GetLampState(int index, string name)
    {
        return new LampState
        {
            Name = name,
            SignalOn = (bool)(_signalOnNodes[index]?.Value ?? false),
            SignalColor = (uint)(_signalColorNodes[index]?.Value ?? 0u),
            SignalMode = (uint)(_signalModeNodes[index]?.Value ?? 0u),
        };
    }

    public sealed class StacklightState
    {
        public uint StacklightMode { get; set; }

        public LampState[] Lamps { get; set; }
    }

    public sealed class LampState
    {
        public string Name { get; set; }

        public bool SignalOn { get; set; }

        public uint SignalColor { get; set; }

        public uint SignalMode { get; set; }
    }

    private void SetLampState(int index, bool signalOn, int signalMode)
    {
        _signalOnNodes[index].Value = signalOn;
        _signalOnNodes[index].Timestamp = _timeService.Now();
        _signalOnNodes[index].ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);

        _signalModeNodes[index].Value = (uint)signalMode;
        _signalModeNodes[index].Timestamp = _timeService.Now();
        _signalModeNodes[index].ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }
}
