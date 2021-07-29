namespace OpcPlc.Nodes
{
    using Opc.Ua;
    using System;

    public interface INodes<TParam>
    {
        string Prototype { get; set; }
        string Description { get; set; }
        Action<TParam> Action { get; set; }
        bool IsEnabled { get; }

        void AddToAddressSpace(FolderState parentFolder, PlcNodeManager plcNodeManager);
        void StartSimulation(PlcServer server);
        void StopSimulation();
    }
}
