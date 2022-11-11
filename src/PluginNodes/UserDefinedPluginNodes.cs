namespace OpcPlc.PluginNodes;

using Newtonsoft.Json;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using static OpcPlc.Program;

/// <summary>
/// Nodes that are configuration via JSON file.
/// </summary>
public class UserDefinedPluginNodes : IPluginNodes
{
    public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

    private static string _nodesFileName;
    private PlcNodeManager _plcNodeManager;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "nf|nodesfile=",
            "the filename which contains the list of nodes to be created in the OPC UA address space.",
            (string s) => _nodesFileName = s);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (!string.IsNullOrEmpty(_nodesFileName))
        {
            AddNodes((FolderState)telemetryFolder.Parent); // Root.
        }
    }

    public void StartSimulation()
    {
    }

    public void StopSimulation()
    {
    }

    private void AddNodes(FolderState folder)
    {
        if (!File.Exists(_nodesFileName))
        {
            string error = $"The user node configuration file {_nodesFileName} does not exist.";
            Logger.Error(error);
            throw new Exception(error);
        }

        string json = File.ReadAllText(_nodesFileName);

        var cfgFolder = JsonConvert.DeserializeObject<ConfigFolder>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
        });

        Logger.Information($"Processing node information configured in {_nodesFileName}");
        Logger.Debug($"Create folder {cfgFolder.Folder}");
        FolderState userNodesFolder = _plcNodeManager.CreateFolder(
            folder,
            path: cfgFolder.Folder,
            name: cfgFolder.Folder,
            NamespaceType.OpcPlcApplications);

        var nodes = new List<NodeWithIntervals>();

        foreach (var node in cfgFolder.NodeList)
        {
            if (node.NodeId.GetType() != Type.GetType("System.Int64") && node.NodeId.GetType() != Type.GetType("System.String"))
            {
                Logger.Error($"The type of the node configuration for node with name {node.Name} ({node.NodeId.GetType()}) is not supported. Only decimal and string are supported. Defaulting to string.");
                node.NodeId = node.NodeId.ToString();
            }

            string typedNodeId = $"{(node.NodeId.GetType() == Type.GetType("System.Int64") ? "i=" : "s=")}{node.NodeId.ToString()}";
            if (string.IsNullOrEmpty(node.Name))
            {
                node.Name = typedNodeId;
            }

            if (string.IsNullOrEmpty(node.Description))
            {
                node.Description = node.Name;
            }

            Logger.Debug("Create node with Id {typedNodeId} and BrowseName {name} in namespace with index {namespaceIndex}",
                typedNodeId,
                node.Name,
                _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]);

            CreateBaseVariable(userNodesFolder, node);

            nodes.Add(PluginNodesHelpers.GetNodeWithIntervals((NodeId)node.NodeId, _plcNodeManager));
        }

        Logger.Information("Completed processing user defined node information");

        Nodes = nodes;
    }

    /// <summary>
    /// Creates a new variable.
    /// </summary>
    public void CreateBaseVariable(NodeState parent, ConfigNode node)
    {
        if (!Enum.TryParse(node.DataType, out BuiltInType nodeDataType))
        {
            Logger.Error($"Value '{node.DataType}' of node '{node.NodeId}' cannot be parsed. Defaulting to 'Int32'");
            node.DataType = "Int32";
        }

        // We have to hard code the conversion here, because AccessLevel is defined as byte in OPC UA lib.
        byte accessLevel;
        try
        {
            accessLevel = (byte)(typeof(AccessLevels).GetField(node.AccessLevel).GetValue(null));
        }
        catch
        {
            Logger.Error($"AccessLevel '{node.AccessLevel}' of node '{node.Name}' is not supported. Defaulting to 'CurrentReadOrWrite'");
            node.AccessLevel = "CurrentRead";
            accessLevel = AccessLevels.CurrentReadOrWrite;
        }

        _plcNodeManager.CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, NamespaceType.OpcPlcApplications, node?.Value);
    }
}
