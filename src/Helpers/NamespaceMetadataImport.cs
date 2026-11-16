namespace OpcPlc.Helpers;

using Opc.Ua;
using Opc.Ua.Export;
using System.Collections.Generic;

/// <summary>
/// Imports NodeSet2 documents so that their NamespaceMetadata objects become typed
/// <see cref="NamespaceMetadataState"/> nodes.
/// </summary>
/// <remarks>
/// The SDK only recognizes typed metadata nodes under Server/Namespaces. A generic import leaves them as
/// <see cref="BaseObjectState"/>, so the SDK publishes a second, empty metadata object for the same namespace.
/// </remarks>
internal static class NamespaceMetadataImport
{
    /// <summary>
    /// Imports the node set, typing its NamespaceMetadata objects and their properties.
    /// </summary>
    public static void Import(UANodeSet nodeSet, ISystemContext context, NodeStateCollection nodes)
    {
        var factory = new MetadataStateFactory(nodeSet, context);
        if (!factory.HasMetadata)
        {
            nodeSet.Import(context, nodes);
            return;
        }

        nodeSet.Import(context, nodes, factory.Create);
        factory.ClearDeclaredParents();
    }

    private sealed class MetadataStateFactory
    {
        private readonly ISystemContext _context;
        private readonly Dictionary<NodeId, NamespaceMetadataState> _metadata = [];
        private readonly Dictionary<NodeId, (NodeId Parent, QualifiedName BrowseName)> _properties = [];
        private readonly List<BaseInstanceState> _attached = [];

        public MetadataStateFactory(UANodeSet nodeSet, ISystemContext context)
        {
            _context = context;
            var metadataIds = new Dictionary<string, NodeId>();
            foreach (UANode node in nodeSet.Items ?? [])
            {
                if (node is UAObject metadataObject && IsNamespaceMetadata(nodeSet, metadataObject) &&
                    TryImportNodeId(nodeSet, metadataObject.NodeId, out NodeId nodeId))
                {
                    metadataIds[metadataObject.NodeId] = nodeId;
                    _metadata[nodeId] = new NamespaceMetadataState(null);
                }
            }

            foreach (UANode node in nodeSet.Items ?? [])
            {
                if (node is UAVariable property && property.ParentNodeId is not null &&
                    metadataIds.TryGetValue(property.ParentNodeId, out NodeId parentId) &&
                    TryImportNodeId(nodeSet, property.NodeId, out NodeId nodeId) &&
                    TryImportBrowseName(nodeSet, property.BrowseName, out QualifiedName browseName))
                {
                    _properties[nodeId] = (parentId, browseName);
                }
            }
        }

        public bool HasMetadata => _metadata.Count > 0;

        public NodeState Create(NodeClass nodeClass, NodeId nodeId, NodeId discriminatorId)
        {
            if (nodeClass == NodeClass.Object && _metadata.TryGetValue(nodeId, out NamespaceMetadataState metadata))
            {
                return metadata;
            }

            if (nodeClass == NodeClass.Variable && _properties.TryGetValue(nodeId, out var property))
            {
                // The generated slot types the property and attaches it to its parent in one step.
                BaseInstanceState child = _metadata[property.Parent]
                    .CreateChild(_context, property.BrowseName, assignInstanceNodeIds: false);
                if (child is not null)
                {
                    _attached.Add(child);
                }

                return child;
            }

            return null;
        }

        /// <summary>
        /// Clears the declared parent of the attached properties, as the SDK linker does for linked children,
        /// so a later link pass does not attach them a second time.
        /// </summary>
        public void ClearDeclaredParents()
        {
            foreach (BaseInstanceState child in _attached)
            {
                child.Handle = null;
            }
        }

        private static bool IsNamespaceMetadata(UANodeSet nodeSet, UAObject node)
        {
            foreach (Reference reference in node.References ?? [])
            {
                if (reference.IsForward &&
                    NodeId.TryParse(ResolveAlias(nodeSet, reference.ReferenceType), out NodeId referenceType) &&
                    referenceType == ReferenceTypeIds.HasTypeDefinition &&
                    NodeId.TryParse(ResolveAlias(nodeSet, reference.Value), out NodeId typeDefinition))
                {
                    return typeDefinition == ObjectTypeIds.NamespaceMetadataType;
                }
            }

            return false;
        }

        private bool TryImportNodeId(UANodeSet nodeSet, string text, out NodeId nodeId)
        {
            nodeId = NodeId.Null;
            if (!NodeId.TryParse(ResolveAlias(nodeSet, text), out NodeId parsed) ||
                !TryImportNamespaceIndex(nodeSet, parsed.NamespaceIndex, out ushort namespaceIndex))
            {
                return false;
            }

            nodeId = parsed.WithNamespaceIndex(namespaceIndex);
            return true;
        }

        private bool TryImportBrowseName(UANodeSet nodeSet, string text, out QualifiedName browseName)
        {
            browseName = QualifiedName.Null;
            QualifiedName parsed = QualifiedName.Parse(text);
            if (!TryImportNamespaceIndex(nodeSet, parsed.NamespaceIndex, out ushort namespaceIndex))
            {
                return false;
            }

            browseName = new QualifiedName(parsed.Name, namespaceIndex);
            return true;
        }

        // Only namespaces the node manager already registered are mapped, so the server namespace table is
        // never modified here; nodes in other namespaces keep the generic import.
        private bool TryImportNamespaceIndex(UANodeSet nodeSet, ushort documentIndex, out ushort namespaceIndex)
        {
            namespaceIndex = documentIndex;
            if (documentIndex == 0)
            {
                return true;
            }

            if (nodeSet.NamespaceUris is null || documentIndex > nodeSet.NamespaceUris.Length)
            {
                return false;
            }

            int index = _context.NamespaceUris.GetIndex(nodeSet.NamespaceUris[documentIndex - 1]);
            if (index < 0)
            {
                return false;
            }

            namespaceIndex = (ushort)index;
            return true;
        }

        private static string ResolveAlias(UANodeSet nodeSet, string text)
        {
            foreach (NodeIdAlias alias in nodeSet.Aliases ?? [])
            {
                if (alias.Alias == text)
                {
                    return alias.Value;
                }
            }

            return text;
        }
    }
}
