namespace OpcPlc.PluginNodes
{
    using Opc.Ua;
    using System.Collections.Generic;

    public interface IPluginNodes
    {
        IReadOnlyCollection<string> NodeIDs { get; }

        void AddOptions(Mono.Options.OptionSet optionSet);
        void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager);
        void StartSimulation();
        void StopSimulation();
    }
}
