namespace OpcPlc.PluginNodes;

using BoilerModel1;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;

/// <summary>
/// Complex type boiler node.
/// </summary>
public class ComplexTypeBoilerPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _plcNodeManager;
    private Boiler1State _node;
    private ITimer _nodeGenerator;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        // ctb|complextypeboiler
        // Add complex type (boiler) to address space.
        // Enabled by default.
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        AddNodes(methodsFolder);
    }

    public void StartSimulation()
    {
        _nodeGenerator = _timeService.NewTimer(UpdateBoiler1, intervalInMilliseconds: 1000);
    }

    public void StopSimulation()
    {
        if (_nodeGenerator != null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes(FolderState methodsFolder)
    {
        // Load complex types from binary uanodes file.
        _plcNodeManager.LoadPredefinedNodes(LoadPredefinedNodes);

        // Find the Boiler1 node that was created when the model was loaded.
        var passiveBoiler1Node = (BaseObjectState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel1.Objects.Boiler1, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseObjectState));
        var boilersNode = (BaseObjectState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel2.Objects.Boilers, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseObjectState));
        var boiler2Node = (BaseObjectState)_plcNodeManager.FindPredefinedNode(new NodeId(5017, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseObjectState));

        var boilerStatus = (BaseDataVariableState)_plcNodeManager.FindPredefinedNode(ExpandedNodeId.ToNodeId(BoilerModel1.VariableIds.Boiler1_BoilerStatus, _plcNodeManager.Server.NamespaceUris), typeof(BaseDataVariableState));
        AllowReadAndWrite(boilerStatus);

        // Convert to node that can be manipulated within the server.
        _node = new Boiler1State(null);
        _node.Create(_plcNodeManager.SystemContext, passiveBoiler1Node);
        _node.BoilerStatus.Value = new BoilerDataType {
            Pressure = 99_000,
            Temperature = new BoilerTemperatureType { Bottom = 100, Top = 95 },
            HeaterState = BoilerHeaterStateType.On,
        };
        _node.BoilerStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);

        // Put Boiler #2 into Boilers folder.
        // TODO: Find a better solution to avoid this dependency between boilers.
        boilersNode.AddChild(boiler2Node);

        _plcNodeManager.AddPredefinedNode(_node);

        AddMethods(methodsFolder);

        // Get BoilerStatus complex type variable.
        var children = new List<BaseInstanceState>();
        _node.GetChildren(_plcNodeManager.SystemContext, children);

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(children[0].NodeId, _plcNodeManager),
        };
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private static NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var uanodesPath = "Boilers/Boiler1/BoilerModel1.PredefinedNodes.uanodes";
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap
            uanodesPath = Path.Join(snapLocation, uanodesPath);
        }

        var predefinedNodes = new NodeStateCollection();

        predefinedNodes.LoadFromBinaryResource(context,
            uanodesPath, // CopyToOutputDirectory -> PreserveNewest.
            typeof(PlcNodeManager).GetTypeInfo().Assembly,
            updateTables: true);

        return predefinedNodes;
    }

    public void UpdateBoiler1(object state, ElapsedEventArgs elapsedEventArgs)
    {
        var newValue = new BoilerDataType
        {
            HeaterState = _node.BoilerStatus.Value.HeaterState,
        };

        int currentTemperatureBottom = _node.BoilerStatus.Value.Temperature.Bottom;
        BoilerTemperatureType newTemperature = newValue.Temperature;

        if (_node.BoilerStatus.Value.HeaterState == BoilerHeaterStateType.On)
        {
            // Heater on, increase by 1.
            newTemperature.Bottom = currentTemperatureBottom + 1;
        }
        else
        {
            // Heater off, decrease down to a minimum of 20.
            newTemperature.Bottom = Math.Max(20, currentTemperatureBottom - 1);
        }

        // Top is always 5 degrees less than bottom, with a minimum value of 20.
        newTemperature.Top = Math.Max(20, newTemperature.Bottom - 5);

        // Pressure is always 100_000 + bottom temperature.
        newValue.Pressure = 100_000 + newTemperature.Bottom;

        // Change complex value in one atomic step.
        _node.BoilerStatus.Value = newValue;
        _node.BoilerStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
    }

    private void AddMethods(NodeState methodsFolder)
    {
        // Create heater on/off methods.
        MethodState heaterOnMethod = _plcNodeManager.CreateMethod(
            methodsFolder,
            path: "HeaterOn",
            name: "HeaterOn",
            "Turn the heater on",
            NamespaceType.Boiler);

        SetHeaterOnMethodProperties(ref heaterOnMethod);

        MethodState heaterOffMethod = _plcNodeManager.CreateMethod(
            methodsFolder,
            path: "HeaterOff",
            name: "HeaterOff",
            "Turn the heater off",
            NamespaceType.Boiler);

        SetHeaterOffMethodProperties(ref heaterOffMethod);
    }

    private void SetHeaterOnMethodProperties(ref MethodState method)
    {
        method.OnCallMethod += OnHeaterOnCall;
    }

    private void SetHeaterOffMethodProperties(ref MethodState method)
    {
        method.OnCallMethod += OnHeaterOffCall;
    }

    /// <summary>
    /// Method to turn the heater on. Executes synchronously.
    /// </summary>
    private ServiceResult OnHeaterOnCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        _node.BoilerStatus.Value.HeaterState = BoilerHeaterStateType.On;
        _logger.LogDebug("OnHeaterOnCall method called");
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method to turn the heater off. Executes synchronously.
    /// </summary>
    private ServiceResult OnHeaterOffCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
    {
        _node.BoilerStatus.Value.HeaterState = BoilerHeaterStateType.Off;
        _logger.LogDebug("OnHeaterOffCall method called");
        return ServiceResult.Good;
    }
    private void AllowReadAndWrite(BaseDataVariableState variable)
    {
        variable.Timestamp = _timeService.Now();
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }
}
