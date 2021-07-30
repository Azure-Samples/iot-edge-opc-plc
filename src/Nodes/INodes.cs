namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System.Collections.Generic;

    public interface INodes
    {
        IReadOnlyCollection<string> NodeIDs { get; }

        void AddOption(Mono.Options.OptionSet optionSet);
        void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager);
        void StartSimulation(PlcServer server);
        void StopSimulation();
    }
}
