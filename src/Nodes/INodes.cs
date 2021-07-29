using Opc.Ua;
using System;

namespace OpcPlc.Nodes
{
    public interface INodes<T>
    {
        string Prototype { get; set; }
        string Description { get; set; }
        Action<T> Action { get; set; }

        void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager);
        void StartSimulation(PlcServer server);
        void StopSimulation();
    }
}
