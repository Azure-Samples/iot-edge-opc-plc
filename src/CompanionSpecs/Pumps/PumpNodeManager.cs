namespace OpcPlc.CompanionSpecs.Pumps;

using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>
/// Node manager for a trimmed subset of the OPC UA Pumps companion spec.
/// https://reference.opcfoundation.org/Pumps/v100/docs/
/// </summary>
public sealed class PumpNodeManager : CustomNodeManager2
{
    /// <summary>
    /// NodeId (in the Pumps namespace) of the SystemRequirementsType defined by the Pumps companion spec.
    /// </summary>
    public const uint SystemRequirementsTypeId = 1022;

    private const string NodeSetXmlRelativePath = "CompanionSpecs/Pumps/Opc.Ua.Pumps.NodeSet2.xml";

    private static IReadOnlyList<PumpTypeMember> _systemRequirementsMembers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PumpNodeManager"/> class.
    /// </summary>
    public PumpNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _)
    {
        SystemContext.NodeIdFactory = this;

        // Set one namespace for the Pumps type model.
        string[] namespaceUrls = [OpcPlc.Namespaces.Pumps];
        SetNamespaces(namespaceUrls);
    }

    /// <summary>
    /// Reads the member declarations (BrowseName, DataType and ValueRank) of the SystemRequirementsType
    /// directly from the Pumps NodeSet2 XML, so instances can be populated without hardcoding the
    /// member list. The result is parsed once and cached.
    /// </summary>
    public static IReadOnlyList<PumpTypeMember> GetSystemRequirementsMembers()
    {
        return _systemRequirementsMembers ??= ReadTypeMembers(SystemRequirementsTypeId);
    }

    /// <summary>
    /// Loads the Pumps node set from the NodeSet2 XML file and adds them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var predefinedNodes = new NodeStateCollection();
        ReadNodeSet().Import(context, predefinedNodes);

        return predefinedNodes;
    }

    private static UANodeSet ReadNodeSet()
    {
        var xmlPath = NodeSetXmlRelativePath;
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap.
            xmlPath = Path.Join(snapLocation, xmlPath);
        }

        using var stream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read);
        return UANodeSet.Read(stream);
    }

    /// <summary>
    /// Parses the NodeSet2 model and returns the instance-declaration members (variables) of the given
    /// type. DataTypes are returned as <see cref="ExpandedNodeId"/> carrying the namespace URI, so the
    /// caller can resolve them against the running server's namespace table.
    /// </summary>
    private static List<PumpTypeMember> ReadTypeMembers(uint typeId)
    {
        var nodeSet = ReadNodeSet();

        var aliases = new Dictionary<string, string>();
        foreach (NodeIdAlias alias in nodeSet.Aliases ?? [])
        {
            aliases[alias.Alias] = alias.Value;
        }

        var nodesById = new Dictionary<string, UANode>();
        foreach (UANode node in nodeSet.Items ?? [])
        {
            nodesById[node.NodeId] = node;
        }

        // In a NodeSet2, ns index 0 is the base UA namespace; model namespaces start at index 1.
        int pumpsModelIndex = Array.IndexOf(nodeSet.NamespaceUris ?? [], OpcPlc.Namespaces.Pumps);
        string typeNodeIdString = $"ns={pumpsModelIndex + 1};i={typeId}";

        var members = new List<PumpTypeMember>();
        if (!nodesById.TryGetValue(typeNodeIdString, out UANode typeNode))
        {
            return members;
        }

        foreach (Reference reference in typeNode.References ?? [])
        {
            // HasComponent forward references from the type point at its instance-declaration members.
            if (!reference.IsForward || ResolveAlias(reference.ReferenceType, aliases) != "i=47")
            {
                continue;
            }

            if (nodesById.TryGetValue(reference.Value, out UANode memberNode) && memberNode is UAVariable variable)
            {
                members.Add(new PumpTypeMember(
                    StripNamespacePrefix(variable.BrowseName),
                    ResolveDataType(variable.DataType, aliases, nodeSet.NamespaceUris),
                    ResolveTypeDefinition(variable.References, aliases, nodeSet.NamespaceUris),
                    variable.ValueRank));
            }
        }

        return members;
    }

    private static string ResolveAlias(string value, Dictionary<string, string> aliases)
    {
        return aliases.TryGetValue(value, out string resolved) ? resolved : value;
    }

    private static string StripNamespacePrefix(string browseName)
    {
        int separator = browseName.IndexOf(':');
        return separator < 0 ? browseName : browseName[(separator + 1)..];
    }

    private static ExpandedNodeId ResolveTypeDefinition(Reference[] references, Dictionary<string, string> aliases, string[] modelNamespaceUris)
    {
        foreach (Reference reference in references ?? [])
        {
            if (ResolveAlias(reference.ReferenceType, aliases) == "i=40")
            {
                return ResolveExpandedNodeId(reference.Value, aliases, modelNamespaceUris);
            }
        }

        return ExpandedNodeId.Null;
    }

    /// <summary>
    /// Resolves a DataType attribute value (alias or NodeId string) into an <see cref="ExpandedNodeId"/>
    /// carrying the namespace URI, so it can be mapped onto the running server's namespace table.
    /// </summary>
    private static ExpandedNodeId ResolveDataType(string dataType, Dictionary<string, string> aliases, string[] modelNamespaceUris)
    {
        string idString = ResolveAlias(dataType, aliases);

        return ResolveExpandedNodeId(idString, aliases, modelNamespaceUris);
    }

    private static ExpandedNodeId ResolveExpandedNodeId(string value, Dictionary<string, string> aliases, string[] modelNamespaceUris)
    {
        string idString = ResolveAlias(value, aliases);

        int modelNamespaceIndex = 0;
        string identifierPart = idString;
        if (idString.StartsWith("ns=", StringComparison.Ordinal))
        {
            int separator = idString.IndexOf(';');
            modelNamespaceIndex = int.Parse(idString[3..separator], CultureInfo.InvariantCulture);
            identifierPart = idString[(separator + 1)..];
        }

        var nodeId = NodeId.Parse(identifierPart);

        // Model namespaces start at index 1; index 0 is the base UA namespace.
        if (modelNamespaceIndex == 0)
        {
            return new ExpandedNodeId(nodeId);
        }

        string namespaceUri = modelNamespaceUris[modelNamespaceIndex - 1];
        return new ExpandedNodeId(nodeId.Identifier, 0, namespaceUri, 0);
    }
}

/// <summary>
/// Describes an instance-declaration member of a Pumps companion-spec type, extracted from the NodeSet.
/// </summary>
public sealed record PumpTypeMember(string BrowseName, ExpandedNodeId DataType, ExpandedNodeId TypeDefinition, int ValueRank);

