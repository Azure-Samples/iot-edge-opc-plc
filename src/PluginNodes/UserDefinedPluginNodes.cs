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
public class UserDefinedPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
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

            _logger.LogInformation($"Processing node information configured in {_nodesFileName}");

            Nodes = AddNodes(folder, cfgFolder).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error loading user defined node file {file}: {error}", _nodesFileName, e.Message);
        }


        _logger.LogInformation("Completed processing user defined node file");
    }

    private IEnumerable<NodeWithIntervals> AddNodes(FolderState folder, ConfigFolder cfgFolder, string parentNamespaceUri = null)
    {
        // Get namespace index for this folder (or use parent's if not specified)
        string effectiveNamespaceUri = cfgFolder.NamespaceUri ?? parentNamespaceUri;
        ushort folderNamespaceIndex = _plcNodeManager.GetNamespaceIndex(effectiveNamespaceUri);

        _logger.LogDebug($"Create folder {cfgFolder.Folder}");
        FolderState userNodesFolder = _plcNodeManager.CreateFolder(
            folder,
            path: cfgFolder.Folder,
            name: cfgFolder.Folder,
            NamespaceType.OpcPlcApplications);

        foreach (var node in cfgFolder.NodeList)
        {
            // Get namespace index for this node (node-level overrides folder-level)
            ushort nodeNamespaceIndex = string.IsNullOrEmpty(node.NamespaceUri)
                ? folderNamespaceIndex
                : _plcNodeManager.GetNamespaceIndex(node.NamespaceUri);

            bool isDecimal = node.NodeId is long;
            bool isString = node.NodeId is string;

            if (!isDecimal && !isString)
            {
                _logger.LogError($"The type of the node configuration for node with name {node.Name} ({node.NodeId.GetType()}) is not supported. Only decimal, string, and GUID are supported. Defaulting to string.");
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

            _logger.LogDebug("Create node with Id {typedNodeId}, BrowseName {name} and type {type} in namespace with index {namespaceIndex}",
                typedNodeId,
                node.Name,
                (string)node.NodeId.GetType().Name,
                nodeNamespaceIndex);

            CreateBaseVariable(userNodesFolder, node, nodeNamespaceIndex);

            NodeId nodeId;
            if (isString)
            {
                nodeId = new NodeId((string)node.NodeId, nodeNamespaceIndex);
            }
            else if (isGuid)
            {
                nodeId = new NodeId((Guid)node.NodeId, nodeNamespaceIndex);
            }
            else
            {
                nodeId = new NodeId((uint)node.NodeId, nodeNamespaceIndex);
            }

            yield return PluginNodesHelper.GetNodeWithIntervals(nodeId, _plcNodeManager);
        }

        foreach (var childNode in AddFolders(userNodesFolder, cfgFolder, effectiveNamespaceUri))
        {
            yield return childNode;
        }
    }

    private IEnumerable<NodeWithIntervals> AddFolders(FolderState folder, ConfigFolder cfgFolder, string parentNamespaceUri)
    {
        if (cfgFolder.FolderList is null)
        {
            yield break;
        }

        foreach (var childFolder in cfgFolder.FolderList)
        {
            foreach (var node in AddNodes(folder, childFolder, parentNamespaceUri))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Creates a new variable.
    /// </summary>
    public void CreateBaseVariable(NodeState parent, ConfigNode node, ushort namespaceIndex)
    {
        if (!Enum.TryParse(node.DataType, out BuiltInType nodeDataType))
        {
            _logger.LogError($"Value {node.DataType} of node {node.NodeId} cannot be parsed. Defaulting to Int32");
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
            _logger.LogError($"AccessLevel {node.AccessLevel} of node {node.Name} is not supported. Defaulting to CurrentReadOrWrite");
            node.AccessLevel = "CurrentRead";
            accessLevel = AccessLevels.CurrentReadOrWrite;
        }

        CreateBaseVariableWithNamespace(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, namespaceIndex, node?.Value);
    }

    /// <summary>
    /// Creates a new variable with a specific namespace index.
    /// </summary>
    private void CreateBaseVariableWithNamespace(NodeState parent, dynamic path, string name, NodeId dataType, int valueRank, byte accessLevel, string description, ushort namespaceIndex, object defaultValue = null)
    {
        var baseDataVariableState = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
        };

        if (path is uint or long)
        {
            baseDataVariableState.NodeId = new NodeId((uint)path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(((uint)path).ToString(), namespaceIndex);
        }
        else if (path is string)
        {
            baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(path, namespaceIndex);
        }
        else if (path is Guid)
        {
            baseDataVariableState.NodeId = new NodeId((Guid)path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(name, namespaceIndex);
        }
        else
        {
            _logger.LogDebug($"NodeId type is {path.GetType()}");
            baseDataVariableState.NodeId = new NodeId(path, namespaceIndex);
            baseDataVariableState.BrowseName = new QualifiedName(name, namespaceIndex);
        }

        baseDataVariableState.DisplayName = new LocalizedText("en", name);
        baseDataVariableState.WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
        baseDataVariableState.UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description;
        baseDataVariableState.DataType = dataType;
        baseDataVariableState.ValueRank = valueRank;
        baseDataVariableState.AccessLevel = accessLevel;
        baseDataVariableState.UserAccessLevel = accessLevel;
        baseDataVariableState.Historizing = false;
        baseDataVariableState.Value = defaultValue ?? TypeInfo.GetDefaultValue(dataType, valueRank, _plcNodeManager.Server.TypeTree);
        baseDataVariableState.StatusCode = StatusCodes.Good;
        baseDataVariableState.Timestamp = _timeService.UtcNow();
        baseDataVariableState.Description = new LocalizedText(description);

        if (valueRank == ValueRanks.OneDimension)
        {
            baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0 });
        }
        else if (valueRank == ValueRanks.TwoDimensions)
        {
            baseDataVariableState.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> { 0, 0 });
        }

        parent?.AddChild(baseDataVariableState);
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
}
