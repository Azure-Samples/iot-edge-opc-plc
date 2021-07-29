using Opc.Ua;
using System;

namespace OpcPlc.Nodes
{
    public interface INodes<TParam>
    {
        string Prototype { get; set; }
        string Description { get; set; }
        Action<TParam> Action { get; set; }

        void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager);
        void StartSimulation(PlcServer server);
        void StopSimulation();
    }
}
