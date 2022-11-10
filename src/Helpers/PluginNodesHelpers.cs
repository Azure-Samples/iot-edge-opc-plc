
namespace OpcPlc.Helpers;

using Opc.Ua;
using OpcPlc.PluginNodes.Models;
using System;

/// <summary>
/// Helper class for plugin nodes.
/// </summary>
public class PluginNodesHelpers
{
    /// <summary>
    /// Get node with correct type prefix, id, namespace, and intervals.
    /// </summary>
    public static NodeWithIntervals GetNodeWithIntervals(NodeId nodeId, PlcNodeManager plcNodeManager)
    {
        ExpandedNodeId expandedNodeId = NodeId.ToExpandedNodeId(nodeId, plcNodeManager.Server.NamespaceUris);

        return new NodeWithIntervals
        {
            NodeId = expandedNodeId.IdType == IdType.Opaque
                ? Convert.ToBase64String((byte[])expandedNodeId.Identifier)
                : expandedNodeId.Identifier.ToString(),
            NodeIdTypePrefix = GetTypePrefix(expandedNodeId.IdType),
            Namespace = expandedNodeId.NamespaceUri,
        };
    }

    /// <summary>
    /// Get a single lowercase character that represents the type of the node.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static string GetTypePrefix(IdType idType)
    {
        return idType switch
        {
            IdType.Numeric => "i",
            IdType.String => "s",
            IdType.Guid => "g",
            IdType.Opaque => "b",
            _ => throw new ArgumentOutOfRangeException(nameof(idType), idType.ToString(), message: null),
        };
    }
}
