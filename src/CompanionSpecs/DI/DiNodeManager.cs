namespace OpcPlc.CompanionSpecs.DI;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using OpcPlc.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Node manager for a server that exposes the Device Information (DI) companion spec.
/// https://opcfoundation.org/developer-tools/documents/view/197
/// </summary>
public sealed class DiNodeManager : AsyncCustomNodeManager
{
    internal const string NodeSetXmlRelativePath = "CompanionSpecs/DI/Opc.Ua.DI.NodeSet2.xml";

    public override async ValueTask CreateAddressSpaceAsync(
        IDictionary<NodeId, IList<IReference>> externalReferences, CancellationToken cancellationToken = default)
    {
        var logger = Server.Telemetry.CreateLogger<DiNodeManager>();
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Creating DI address space");
        await base.CreateAddressSpaceAsync(externalReferences, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("DI address space created in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiNodeManager"/> class.
    /// </summary>
    public DiNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _)
    {
        SystemContext.NodeIdFactory = this;

        // Set one namespace for the type model and one namespace for dynamically created nodes.
        string[] namespaceUrls = [OpcPlc.Namespaces.DI];
        SetNamespaces(namespaceUrls);
    }

    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
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
