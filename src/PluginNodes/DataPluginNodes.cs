namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;

/// <summary>
/// Nodes with values: Cycling step-up, alternating boolean, random signed 32-bit integer and random unsigend 32-bit integer.
/// </summary>
public class DataPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private bool _isEnabled = true;
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint> _stepUpNode;
    private SimulatedVariableNode<bool> _alternatingBooleanNode;
    private SimulatedVariableNode<int> _randomSignedInt32;
    private SimulatedVariableNode<uint> _randomUnsignedInt32;
    private readonly Random _random = new();
    private bool _stepUpStarted;
    private int _stepUpCycleInPhase;
    private int _alternatingBooleanCycleInPhase;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "nv|nodatavalues",
            $"do not generate data values.\nDefault: {!_isEnabled}",
            (string s) => _isEnabled = s == null);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (_isEnabled)
        {
            FolderState folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Basic",
                name: "Basic",
                NamespaceType.OpcPlcApplications);

            AddNodes(folder);
            AddMethods(methodsFolder);
        }
    }

    public void StartSimulation()
    {
        if (_isEnabled)
        {
            _stepUpCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
            _stepUpStarted = true;
            _alternatingBooleanCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;

            _stepUpNode.Start(StepUpGenerator, _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
            _alternatingBooleanNode.Start(AlternatingBooleanGenerator, _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
            _randomSignedInt32.Start(value => _random.Next(int.MinValue, int.MaxValue), _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
            _randomUnsignedInt32.Start(value => (uint)_random.Next(), _plcNodeManager.PlcSimulationInstance.SimulationCycleLength);
        }
    }

    public void StopSimulation()
    {
        if (_isEnabled)
        {
            _stepUpNode.Stop();
            _alternatingBooleanNode.Stop();
            _randomSignedInt32.Stop();
            _randomUnsignedInt32.Stop();
        }
    }

    private void AddNodes(FolderState folder)
    {
        _stepUpNode = _plcNodeManager.CreateVariableNode<uint>(
            _plcNodeManager.CreateBaseVariable(
                folder,
                path: "StepUp",
                name: "StepUp",
                new NodeId((uint)BuiltInType.UInt32),
                ValueRanks.Scalar,
                AccessLevels.CurrentReadOrWrite,
                "Constantly increasing value",
                NamespaceType.OpcPlcApplications));

        _alternatingBooleanNode = _plcNodeManager.CreateVariableNode<bool>(
            _plcNodeManager.CreateBaseVariable(
                folder,
                path: "AlternatingBoolean",
                name: "AlternatingBoolean",
                new NodeId((uint)BuiltInType.Boolean),
                ValueRanks.Scalar,
                AccessLevels.CurrentRead,
                "Alternating boolean value",
                NamespaceType.OpcPlcApplications));

        _randomSignedInt32 = _plcNodeManager.CreateVariableNode<int>(
            _plcNodeManager.CreateBaseVariable(
                folder,
                path: "RandomSignedInt32",
                name: "RandomSignedInt32",
                new NodeId((uint)BuiltInType.Int32),
                ValueRanks.Scalar,
                AccessLevels.CurrentRead,
                "Random signed 32 bit integer value",
                NamespaceType.OpcPlcApplications));

        _randomUnsignedInt32 = _plcNodeManager.CreateVariableNode<uint>(
            _plcNodeManager.CreateBaseVariable(
                folder,
                path: "RandomUnsignedInt32",
                "RandomUnsignedInt32",
                new NodeId((uint)BuiltInType.UInt32),
                ValueRanks.Scalar,
                AccessLevels.CurrentRead,
                "Random unsigned 32 bit integer value",
                NamespaceType.OpcPlcApplications));

        Nodes = new List<NodeWithIntervals>
            {
                new NodeWithIntervals
                {
                    NodeId = "StepUp",
                    Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                },
                new NodeWithIntervals
                {
                    NodeId = "AlternatingBoolean",
                    Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                },
                new NodeWithIntervals
                {
                    NodeId = "RandomSignedInt32",
                    Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                },
                new NodeWithIntervals
                {
                    NodeId = "RandomUnsignedInt32",
                    Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                },
            };
    }

    private void AddMethods(FolderState parentFolder)
    {
        MethodState resetStepUpMethod = _plcNodeManager.CreateMethod(parentFolder, "ResetStepUp", "ResetStepUp", "Resets the StepUp counter to 0", NamespaceType.OpcPlcApplications);
        SetResetStepUpMethodProperties(ref resetStepUpMethod);

        MethodState startStepUpMethod = _plcNodeManager.CreateMethod(parentFolder, "StartStepUp", "StartStepUp", "Starts the StepUp counter", NamespaceType.OpcPlcApplications);
        SetStartStepUpMethodProperties(ref startStepUpMethod);

        MethodState stopStepUpMethod = _plcNodeManager.CreateMethod(parentFolder, "StopStepUp", "StopStepUp", "Stops the StepUp counter", NamespaceType.OpcPlcApplications);
        SetStopStepUpMethodProperties(ref stopStepUpMethod);
    }

    private void SetResetStepUpMethodProperties(ref MethodState method)
    {
        method.OnCallMethod += OnResetStepUpCall;
    }

    private void SetStartStepUpMethodProperties(ref MethodState method)
    {
        method.OnCallMethod += OnStartStepUpCall;
    }

    private void SetStopStepUpMethodProperties(ref MethodState method)
    {
        method.OnCallMethod += OnStopStepUpCall;
    }

    /// <summary>
    /// Method to reset the step-up value. Executes synchronously.
    /// </summary>
    private ServiceResult OnResetStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        ResetStepUpData();
        _logger.LogDebug("ResetStepUp method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method to start the step-up value. Executes synchronously.
    /// </summary>
    private ServiceResult OnStartStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        StartStepUp();
        _logger.LogDebug("StartStepUp method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method to stop the step-up value. Executes synchronously.
    /// </summary>
    private ServiceResult OnStopStepUpCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        StopStepUp();
        _logger.LogDebug("StopStepUp method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Updates simulation values. Called each SimulationCycleLength ms.
    /// Using SimulationCycleCount cycles per simulation phase.
    /// </summary>
    private uint StepUpGenerator(uint value)
    {
        // increase step-up value
        if (_stepUpStarted && (_stepUpCycleInPhase % (_plcNodeManager.PlcSimulationInstance.SimulationCycleCount / 50) == 0))
        {
            value++;
        }

        // end of cycle: reset cycle count
        if (--_stepUpCycleInPhase == 0)
        {
            _stepUpCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
        }

        return value;
    }

    /// <summary>
    /// Updates simulation values. Called each SimulationCycleLength ms.
    /// Using SimulationCycleCount cycles per simulation phase.
    /// </summary>
    private bool AlternatingBooleanGenerator(bool value)
    {
        // calculate next boolean value
        bool nextAlternatingBoolean = _alternatingBooleanCycleInPhase % _plcNodeManager.PlcSimulationInstance.SimulationCycleCount == 0 ? !value : value;
        if (value != nextAlternatingBoolean)
        {
            _logger.LogTrace($"Data change to: {nextAlternatingBoolean}");
        }

        // end of cycle: reset cycle count
        if (--_alternatingBooleanCycleInPhase == 0)
        {
            _alternatingBooleanCycleInPhase = _plcNodeManager.PlcSimulationInstance.SimulationCycleCount;
        }

        return nextAlternatingBoolean;
    }

    /// <summary>
    /// Method implementation to reset the StepUp data.
    /// </summary>
    public void ResetStepUpData()
    {
        _stepUpNode.Value = 0;
    }

    /// <summary>
    /// Method implementation to start the StepUp.
    /// </summary>
    public void StartStepUp()
    {
        _stepUpStarted = true;
    }

    /// <summary>
    /// Method implementation to stop the StepUp.
    /// </summary>
    public void StopStepUp()
    {
        _stepUpStarted = false;
    }
}
