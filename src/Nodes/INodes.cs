namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    public interface INodes<TParam>
    {
        string Prototype { get; }
        string Description { get; }
        Action<TParam> Action { get; }
        bool IsEnabled { get; }
        IReadOnlyCollection<string> NodeIDs { get; }

        void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager);
        void StartSimulation(PlcServer server);
        void StopSimulation();
    }
}
