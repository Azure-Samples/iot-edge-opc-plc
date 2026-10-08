namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

/// <summary>
/// Complex type boiler node.
/// </summary>
public partial class ComplexTypeBoilerPluginNode(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private PlcNodeManager _plcNodeManager;
    private BaseDataVariableState _boilerStatus;
    private OpcPlc.ITimer _nodeGenerator;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        // ctb|complextypeboiler
        // Add complex type (boiler) to address space.
        // Enabled by default.
    }

    public async ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _plcNodeManager = plcNodeManager;

        await AddNodesAsync(methodsFolder, cancellationToken).ConfigureAwait(false);
    }

    public void OnAddressSpaceReady()
    {
        IStructure temperature = CreateStructure(RuntimeModelIds.Boiler1.DataTypeIds.BoilerTemperatureType);
        temperature["Bottom"] = 100;
        temperature["Top"] = 95;
        IStructure status = CreateStructure(RuntimeModelIds.Boiler1.DataTypeIds.BoilerDataType);
        status["Temperature"] = new Variant(new ExtensionObject((IEncodeable)temperature));
        status["Pressure"] = 99_000;
        status["HeaterState"] = VariantHelper.CastFrom(HeaterState.On);
        _boilerStatus.Value = new Variant(new ExtensionObject((IEncodeable)status));
        _boilerStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
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

    private async ValueTask AddNodesAsync(FolderState methodsFolder, CancellationToken cancellationToken)
    {
        await _plcNodeManager.LoadPredefinedNodesAsync(LoadPredefinedNodes, cancellationToken).ConfigureAwait(false);

        // Find the Boiler1 node that was created when the model was loaded.
        var boilersNode = _plcNodeManager.FindPredefinedNode<BaseObjectState>(new NodeId(RuntimeModelIds.Boiler2.Objects.Boilers, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));
        var boiler2Node = _plcNodeManager.FindPredefinedNode<BaseObjectState>(new NodeId(5017, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]));

        _boilerStatus = _plcNodeManager.FindPredefinedNode<BaseDataVariableState>(ExpandedNodeId.ToNodeId(RuntimeModelIds.Boiler1.VariableIds.Boiler1_BoilerStatus, _plcNodeManager.Server.NamespaceUris));
        AllowReadAndWrite(_boilerStatus);

        // Put Boiler #2 into Boilers folder.
        // TODO: Find a better solution to avoid this dependency between boilers.
        boilersNode.AddChild(boiler2Node);

        AddMethods(methodsFolder);

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(_boilerStatus.NodeId, _plcNodeManager),
        };
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    private static NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var xmlPath = "Boilers/Boiler1/BoilerModel1.NodeSet2.xml";
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap
            xmlPath = Path.Join(snapLocation, xmlPath);
        }

        using var stream = File.OpenRead(xmlPath);
        var nodeSet = Opc.Ua.Export.UANodeSet.Read(stream);
        var predefinedNodes = new NodeStateCollection();
        nodeSet.Import(context, predefinedNodes);
        return predefinedNodes;
    }

    public void UpdateBoiler1(object state, ElapsedEventArgs elapsedEventArgs)
    {
        IStructure currentValue = ReadStructure(_boilerStatus.Value);
        IStructure newValue = CreateStructure(RuntimeModelIds.Boiler1.DataTypeIds.BoilerDataType);
        newValue["HeaterState"] = currentValue["HeaterState"];
        int currentTemperatureBottom = ReadStructure(currentValue["Temperature"])["Bottom"].GetInt32();
        IStructure newTemperature = CreateStructure(RuntimeModelIds.Boiler1.DataTypeIds.BoilerTemperatureType);
        int bottom;

        if (currentValue["HeaterState"].GetInt32() == (int)HeaterState.On)
        {
            // Heater on, increase by 1.
            bottom = currentTemperatureBottom + 1;
        }
        else
        {
            // Heater off, decrease down to a minimum of 20.
            bottom = Math.Max(20, currentTemperatureBottom - 1);
        }

        // Top is always 5 degrees less than bottom, with a minimum value of 20.
        newTemperature["Bottom"] = bottom;
        newTemperature["Top"] = Math.Max(20, bottom - 5);

        // Pressure is always 100_000 + bottom temperature.
        newValue["Temperature"] = new Variant(new ExtensionObject((IEncodeable)newTemperature));
        newValue["Pressure"] = 100_000 + bottom;

        // Change complex value in one atomic step.
        _boilerStatus.Value = new Variant(new ExtensionObject((IEncodeable)newValue));
        _boilerStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
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
    private ServiceResult OnHeaterOnCall(ISystemContext context, MethodState method,
        ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        ReadStructure(_boilerStatus.Value)["HeaterState"] = VariantHelper.CastFrom(HeaterState.On);
        LogOnHeaterOnCallMethodCalled();
        return ServiceResult.Good;
    }

    /// <summary>
    /// Method to turn the heater off. Executes synchronously.
    /// </summary>
    private ServiceResult OnHeaterOffCall(ISystemContext context, MethodState method,
        ArrayOf<Variant> inputArguments, List<Variant> outputArguments)
    {
        ReadStructure(_boilerStatus.Value)["HeaterState"] = VariantHelper.CastFrom(HeaterState.Off);
        LogOnHeaterOffCallMethodCalled();
        return ServiceResult.Good;
    }
    private IStructure CreateStructure(ExpandedNodeId typeId)
    {
        bool registered = _plcNodeManager.Server.Factory.TryGetEncodeableType(typeId, out var type);
        IEncodeable instance = registered ? type.CreateInstance() : null;
        if (instance is not IStructure value)
        {
            throw new InvalidOperationException($"Runtime structure {typeId} is not registered.");
        }
        return value;
    }

    private static IStructure ReadStructure(Variant value)
    {
        if (!value.TryGetStructure(out IEncodeable body) || body is not IStructure structure)
        {
            throw new InvalidOperationException("Boiler value is not a runtime structure.");
        }
        return structure;
    }

    private enum HeaterState
    {
        Off,
        On
    }

    private void AllowReadAndWrite(BaseDataVariableState variable)
    {
        variable.Timestamp = _timeService.Now();
        variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "OnHeaterOnCall method called")]
    partial void LogOnHeaterOnCallMethodCalled();

    [LoggerMessage(Level = LogLevel.Debug, Message = "OnHeaterOffCall method called")]
    partial void LogOnHeaterOffCallMethodCalled();
}
