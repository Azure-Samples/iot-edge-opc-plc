namespace OpcPlc.CompanionSpecs.Machinery;

using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.IO;

/// <summary>
/// Node manager for the OPC UA Machinery companion spec.
/// https://reference.opcfoundation.org/Machinery/v100/docs/
///
/// Machinery is a required model of the Pumps companion spec, so it must be loaded
/// before <see cref="OpcPlc.CompanionSpecs.Pumps.PumpNodeManager"/>. It depends on the
/// DI companion spec, which is already loaded by the DiNodeManager.
/// </summary>
public sealed class MachineryNodeManager : CustomNodeManager2
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineryNodeManager"/> class.
    /// </summary>
    public MachineryNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _)
    {
        SystemContext.NodeIdFactory = this;

        // Set one namespace for the Machinery type model.
        string[] namespaceUrls = [OpcPlc.Namespaces.Machinery];
        SetNamespaces(namespaceUrls);
    }

    /// <summary>
    /// Loads the Machinery node set from the NodeSet2 XML file and adds them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        var xmlPath = "CompanionSpecs/Machinery/Opc.Ua.Machinery.NodeSet2.xml";
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
