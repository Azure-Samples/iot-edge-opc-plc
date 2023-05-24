namespace OpcPlc.CompanionSpecs.DI;

using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Node manager for a server that exposes the Device Information (DI) companion spec.
/// https://opcfoundation.org/developer-tools/documents/view/197
/// </summary>
public sealed class DiNodeManager : CustomNodeManager2
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiNodeManager"/> class.
    /// </summary>
    public DiNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _)
    {
        SystemContext.NodeIdFactory = this;

        // Set one namespace for the type model and one namespace for dynamically created nodes.
        string[] namespaceUrls = new string[1];
        namespaceUrls[0] = OpcPlc.Namespaces.DI;
        SetNamespaces(namespaceUrls);
    }

    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        return node.NodeId;
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();
        predefinedNodes.LoadFromBinaryResource(context,
            "CompanionSpecs/DI/Opc.Ua.DI.PredefinedNodes.uanodes", // CopyToOutputDirectory -> PreserveNewest.
            typeof(DiNodeManager).GetTypeInfo().Assembly,
            updateTables: true);

        return predefinedNodes;
    }

    /// <summary>
    /// Does any initialization required before the address space can be used.
    /// </summary>
    /// <remarks>
    /// The externalReferences is an out parameter that allows the node manager to link to nodes
    /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
    /// should have a reference to the root folder node(s) exposed by this node manager.
    /// </remarks>
    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            LoadPredefinedNodes(SystemContext, externalReferences);
        }
    }

    /// <summary>
    /// Frees any resources allocated for the address space.
    /// </summary>
    public override void DeleteAddressSpace()
    {
        lock (Lock)
        {
            base.DeleteAddressSpace();
        }
    }

    /// <summary>
    /// Returns a unique handle for the node.
    /// </summary>
    protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
    {
        lock (Lock)
        {
            // quickly exclude nodes that are not in the namespace.
            if (!IsNodeIdInNamespace(nodeId))
            {
                return null;
            }

            // check for predefined nodes.
            if (PredefinedNodes != null)
            {
                NodeState node = null;

                if (PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    var handle = new NodeHandle
                    {
                        NodeId = nodeId,
                        Validated = true,
                        Node = node,
                    };

                    return handle;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Verifies that the specified node exists.
    /// </summary>
    protected override NodeState ValidateNode(
        ServerSystemContext context,
        NodeHandle handle,
        IDictionary<NodeId, NodeState> cache)
    {
        // not valid if no root.
        if (handle == null)
        {
            return null;
        }

        // check if previously validated.
        if (handle.Validated)
        {
            return handle.Node;
        }

        return null;
    }
}
