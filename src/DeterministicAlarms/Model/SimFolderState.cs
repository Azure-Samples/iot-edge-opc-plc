using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcPlc.DeterministicAlarms.Model
{
    class SimFolderState : FolderState
    {
        public SimFolderState(ISystemContext context, NodeState parent, NodeId nodeId, string name) : base(parent)
        {
            Initialize(context);

            // initialize the area with the fixed metadata.
            this.SymbolicName = name;
            this.NodeId = nodeId;
            this.BrowseName = new QualifiedName(name, nodeId.NamespaceIndex);
            this.DisplayName = BrowseName.Name;
            this.Description = null;
            this.ReferenceTypeId = ReferenceTypeIds.HasNotifier;
            this.TypeDefinitionId = ObjectTypeIds.FolderType;
            this.EventNotifier = EventNotifiers.SubscribeToEvents;
        }
    }
}
