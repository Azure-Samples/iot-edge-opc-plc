namespace OpcPlc.CompanionSpecs.Pumps;

using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.IO;

/// <summary>
/// Node manager for a trimmed subset of the OPC UA Pumps companion spec.
/// https://reference.opcfoundation.org/Pumps/v100/docs/
/// </summary>
public sealed class PumpNodeManager : CustomNodeManager2
{
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
    /// Loads the Pumps node set from the NodeSet2 XML file and adds them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var xmlPath = "CompanionSpecs/Pumps/Opc.Ua.Pumps.NodeSet2.xml";
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap.
            xmlPath = Path.Join(snapLocation, xmlPath);
        }

        var predefinedNodes = new NodeStateCollection();

        using var stream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read);
        var nodeSet = UANodeSet.Read(stream);
        nodeSet.Import(context, predefinedNodes);

        return predefinedNodes;
    }
}
