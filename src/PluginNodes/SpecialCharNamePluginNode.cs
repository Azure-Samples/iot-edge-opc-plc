namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Web;

/// <summary>
/// Node with special chars in name and ID.
/// </summary>
public class SpecialCharNamePluginNode(PlcSimulation plcSimulation, TimeService timeService, ILogger logger) : PluginNodeBase(plcSimulation, timeService, logger)
{
    private PlcNodeManager _plcNodeManager;
    private SimulatedVariableNode<uint> _node;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        // scn|specialcharname
        // Add node with special characters in name.
        // Enabled by default.
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        FolderState folder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "Special",
            name: "Special",
            NamespaceType.OpcPlcApplications);

        AddNodes(folder);
    }

    public void StartSimulation()
    {
        _node.Start(value => value + 1, periodMs: 1000);
    }

    public void StopSimulation()
    {
        _node.Stop();
    }

    private void AddNodes(FolderState folder)
    {
        string SpecialChars = HttpUtility.HtmlDecode(@"&quot;!&#167;$%&amp;/()=?`&#180;\+~*&#39;#_-:.;,&lt;&gt;|@^&#176;€&#181;{[]}");

        BaseDataVariableState variable = _plcNodeManager.CreateBaseVariable(
            folder,
            path: "Special_" + SpecialChars,
            name: SpecialChars,
            new NodeId((uint)BuiltInType.UInt32),
            ValueRanks.Scalar,
            AccessLevels.CurrentReadOrWrite,
            "Constantly increasing value",
            NamespaceType.OpcPlcApplications,
            defaultValue: (uint)0);

        _node = _plcNodeManager.CreateVariableNode<uint>(variable);

        // Add to node list for creation of pn.json.
        Nodes = new List<NodeWithIntervals>
        {
            PluginNodesHelper.GetNodeWithIntervals(variable.NodeId, _plcNodeManager),
        };
    }
}
