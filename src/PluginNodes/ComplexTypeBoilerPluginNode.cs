namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using OpcPlc.PluginNodes.Models;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Timers;
    using static OpcPlc.Program;

    /// <summary>
    /// Complex type boiler node.
    /// </summary>
    public class ComplexTypeBoilerPluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private static bool _isEnabled;
        private PlcNodeManager _plcNodeManager;
        private BoilerModel.BoilerState _node;
        private ITimer _nodeGenerator;

        public void AddOptions(Mono.Options.OptionSet optionSet)
        {
            optionSet.Add(
                "ctb|complextypeboiler",
                $"add complex type (boiler) to address space.\nDefault: {_isEnabled}",
                (string s) => _isEnabled = s != null);
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
            if (_isEnabled)
            {
                _nodeGenerator = TimeService.NewTimer(UpdateBoiler1, 1000);
            }
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
            var passiveNode = (BaseObjectState)_plcNodeManager.FindPredefinedNode(new NodeId(BoilerModel.Objects.Boiler1, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.Boiler]), typeof(BaseObjectState));

            // Convert to node that can be manipulated within the server.
            _node = new BoilerModel.BoilerState(null);
            _node.Create(_plcNodeManager.SystemContext, passiveNode);

            _plcNodeManager.AddPredefinedNode(_node);

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

        /// <summary>
        /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
        /// </summary>
        private NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var predefinedNodes = new NodeStateCollection();

            predefinedNodes.LoadFromBinaryResource(context,
                "Boiler/BoilerModel.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
                typeof(PlcNodeManager).GetTypeInfo().Assembly,
                updateTables: true);

            return predefinedNodes;
        }

        public void UpdateBoiler1(object state, ElapsedEventArgs elapsedEventArgs)
        {
            var newValue = new BoilerModel.BoilerDataType
            {
                HeaterState = _node.BoilerStatus.Value.HeaterState,
            };

            int currentTemperatureBottom = _node.BoilerStatus.Value.Temperature.Bottom;
            BoilerModel.BoilerTemperatureType newTemperature = newValue.Temperature;

            if (_node.BoilerStatus.Value.HeaterState == BoilerModel.BoilerHeaterStateType.On)
            {
                // Heater on, increase by 1.
                newTemperature.Bottom = currentTemperatureBottom + 1;
            }
            else
            {
                // Heater off, decrease down to a minimum of 20.
                newTemperature.Bottom = currentTemperatureBottom > 20
                    ? currentTemperatureBottom - 1
                    : currentTemperatureBottom;
            }

            // Top is always 5 degrees less than bottom, with a minimum value of 20.
            newTemperature.Top = Math.Max(20, newTemperature.Bottom - 5);

            // Pressure is always 100_000 + bottom temperature.
            newValue.Pressure = 100_000 + newTemperature.Bottom;

            // Change complex value in one atomic step.
            _node.BoilerStatus.Value = newValue;
            _node.BoilerStatus.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: true);
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
            _node.BoilerStatus.Value.HeaterState = BoilerModel.BoilerHeaterStateType.On;
            Logger.Debug("OnHeaterOnCall method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to turn the heater off. Executes synchronously.
        /// </summary>
        private ServiceResult OnHeaterOffCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _node.BoilerStatus.Value.HeaterState = BoilerModel.BoilerHeaterStateType.Off;
            Logger.Debug("OnHeaterOffCall method called");
            return ServiceResult.Good;
        }
    }
}
