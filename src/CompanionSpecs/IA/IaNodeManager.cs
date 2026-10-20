namespace OpcPlc.CompanionSpecs.IA;

using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using OpcPlc.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Node manager for the Industrial Automation (IA) companion spec (includes Stacklight types).
/// https://reference.opcfoundation.org/IA/v101/docs/
/// </summary>
public sealed class IaNodeManager : AsyncCustomNodeManager
{
    internal const string NodeSetXmlRelativePath = "CompanionSpecs/IA/Opc.Ua.IA.NodeSet2.xml";

    /// <summary>
    /// Initializes a new instance of the <see cref="IaNodeManager"/> class.
    /// </summary>
    public IaNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _)
    {
        SystemContext.NodeIdFactory = this;

        // Set one namespace for the IA type model.
        string[] namespaceUrls = [OpcPlc.Namespaces.IA];
        SetNamespaces(namespaceUrls);
    }

    /// <summary>
    /// Loads the IA node set from the NodeSet2 XML file and adds them to the set of predefined nodes.
    /// </summary>
    protected override ValueTask<NodeStateCollection> LoadPredefinedNodesAsync(
        ISystemContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var predefinedNodes = new NodeStateCollection();
        var nodeSet = NodeSetXmlCache.Read(NodeSetXmlRelativePath);
        nodeSet.Import(context, predefinedNodes);

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(predefinedNodes);
    }
}
