namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Nodes that are configured via JSON file.
/// </summary>
public partial class UserDefinedPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private string _nodesFileName;
    private PlcNodeManager _plcNodeManager;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "nf|nodesfile=",
            "the filename that contains the list of nodes to be created in the OPC UA address space.",
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
        // No simulation.
    }

    public void StopSimulation()
    {
        // No simulation.
    }

    private void AddNodes(FolderState folder)
    {
        try
        {
            string json = File.ReadAllText(_nodesFileName);

            var cfgFolder = JsonConvert.DeserializeObject<ConfigFolder>(json, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None,
            });

            LogProcessingNodeInformation(_nodesFileName);

            Nodes = AddNodes(folder, cfgFolder).ToList();
        }
        catch (Exception e)
        {
            LogErrorLoadingUserDefinedNodeFile(e, _nodesFileName, e.Message);
        }


        LogCompletedProcessingUserDefinedNodeFile();
    }

    private IEnumerable<NodeWithIntervals> AddNodes(FolderState folder, ConfigFolder cfgFolder)
    {
        LogCreateFolder(cfgFolder.Folder);
        FolderState userNodesFolder = _plcNodeManager.CreateFolder(
            folder,
            path: cfgFolder.Folder,
            name: cfgFolder.Folder,
            NamespaceType.OpcPlcApplications);

        foreach (var node in cfgFolder.NodeList)
        {
            bool isDecimal = node.NodeId is long;
            bool isString = node.NodeId is string;

            if (!isDecimal && !isString)
            {
                LogUnsupportedNodeType(node.Name, node.NodeId.GetType().ToString());
                node.NodeId = node.NodeId.ToString();
            }

            bool isGuid = false;
            if (Guid.TryParse(node.NodeId.ToString(), out Guid guidNodeId))
            {
                isGuid = true;
                node.NodeId = guidNodeId;
            }

            string typedNodeId = isDecimal
                ? $"i={node.NodeId.ToString()}"
                : isGuid
                    ? $"g={node.NodeId.ToString()}"
                    : $"s={node.NodeId.ToString()}";

            if (node.ValueRank == 1 && node.Value is JArray jArrayValue)
            {
                node.Value = UpdateArrayValue(node, jArrayValue);
            }

            if (string.IsNullOrEmpty(node.Name))
            {
                node.Name = typedNodeId;
            }

            if (string.IsNullOrEmpty(node.Description))
            {
                node.Description = node.Name;
            }

            LogCreateNode(typedNodeId, node.Name, (string)node.NodeId.GetType().Name, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications]);

            CreateBaseVariable(userNodesFolder, node);

            var nodeId = isString
                ? new NodeId(node.NodeId, _plcNodeManager.NamespaceIndexes[(int)NamespaceType.OpcPlcApplications])
                : (NodeId)node.NodeId;

            yield return PluginNodesHelper.GetNodeWithIntervals(nodeId, _plcNodeManager);
        }

        foreach (var childNode in AddFolders(userNodesFolder, cfgFolder))
        {
            yield return childNode;
        }
    }

    private IEnumerable<NodeWithIntervals> AddFolders(FolderState folder, ConfigFolder cfgFolder)
    {
        if (cfgFolder.FolderList is null)
        {
            yield break;
        }

        foreach (var childFolder in cfgFolder.FolderList)
        {
            foreach (var node in AddNodes(folder, childFolder))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Creates a new variable.
    /// </summary>
    public void CreateBaseVariable(NodeState parent, ConfigNode node)
    {
        if (!Enum.TryParse(node.DataType, out BuiltInType nodeDataType))
        {
            LogCannotParseDataType(node.DataType, node.NodeId.ToString());
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
            LogUnsupportedAccessLevel(node.AccessLevel, node.Name);
            node.AccessLevel = "CurrentRead";
            accessLevel = AccessLevels.CurrentReadOrWrite;
        }

        _plcNodeManager.CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, NamespaceType.OpcPlcApplications, node?.Value);
    }

    private static object UpdateArrayValue(ConfigNode node, JArray jArrayValue)
    {
        return node.DataType switch {
            "String" => jArrayValue.ToObject<string[]>(),
            "Boolean" => jArrayValue.ToObject<bool[]>(),
            "Float" => jArrayValue.ToObject<float[]>(),
            "UInt32" => jArrayValue.ToObject<uint[]>(),
            "Int32" => jArrayValue.ToObject<int[]>(),
            _ => throw new NotImplementedException($"Node type not implemented: {node.DataType}."),
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing node information configured in {NodesFileName}")]
    partial void LogProcessingNodeInformation(string nodesFileName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading user defined node file {File}: {Error}")]
    partial void LogErrorLoadingUserDefinedNodeFile(Exception exception, string file, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed processing user defined node file")]
    partial void LogCompletedProcessingUserDefinedNodeFile();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Create folder {Folder}")]
    partial void LogCreateFolder(string folder);

    [LoggerMessage(Level = LogLevel.Error, Message = "The type of the node configuration for node with name {Name} ({NodeIdType}) is not supported. Only decimal, string, and GUID are supported. Defaulting to string.")]
    partial void LogUnsupportedNodeType(string name, string nodeIdType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Create node with Id {TypedNodeId}, BrowseName {Name} and type {Type} in namespace with index {NamespaceIndex}")]
    partial void LogCreateNode(string typedNodeId, string name, string type, ushort namespaceIndex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Value {DataType} of node {NodeId} cannot be parsed. Defaulting to Int32")]
    partial void LogCannotParseDataType(string dataType, string nodeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "AccessLevel {AccessLevel} of node {Name} is not supported. Defaulting to CurrentReadOrWrite")]
    partial void LogUnsupportedAccessLevel(string accessLevel, string name);
}
