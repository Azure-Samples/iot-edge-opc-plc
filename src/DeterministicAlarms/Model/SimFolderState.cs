namespace OpcPlc.DeterministicAlarms.Model
{
    using Opc.Ua;

    public class SimFolderState : FolderState
    {
        public SimFolderState(ISystemContext context, NodeState parent, NodeId nodeId, string name) : base(parent)
        {
            Initialize(context);

            // initialize the area with the fixed metadata.
            SymbolicName = name;
            NodeId = nodeId;
            BrowseName = new QualifiedName(name, nodeId.NamespaceIndex);
            DisplayName = BrowseName.Name;
            Description = null;
            ReferenceTypeId = ReferenceTypeIds.HasNotifier;
            TypeDefinitionId = ObjectTypeIds.FolderType;
            EventNotifier = EventNotifiers.SubscribeToEvents;
        }
    }
}