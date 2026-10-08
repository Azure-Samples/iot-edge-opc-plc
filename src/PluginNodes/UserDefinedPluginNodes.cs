namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Nodes that are configured via JSON file.
/// </summary>
public partial class UserDefinedPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private string _nodesFileName;
    private PlcNodeManager _plcNodeManager;
    private static readonly JsonSerializerOptions ConfigurationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "nf|nodesfile=",
            "the filename that contains the list of nodes to be created in the OPC UA address space.",
            (string s) => _nodesFileName = s);
    }

    public async ValueTask AddToAddressSpaceAsync(
        FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _plcNodeManager = plcNodeManager;

        if (!string.IsNullOrEmpty(_nodesFileName))
        {
            await AddNodesAsync((FolderState)telemetryFolder.Parent, cancellationToken).ConfigureAwait(false); // Root.
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

    private async Task AddNodesAsync(FolderState folder, CancellationToken cancellationToken)
    {
        try
        {
            var stream = new FileStream(
                _nodesFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await using var streamLifetime = stream.ConfigureAwait(false);
            var cfgFolder = await JsonSerializer.DeserializeAsync<ConfigFolder>(
                stream, ConfigurationJsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException("The nodes file must contain a configuration folder.");

            LogProcessingNodeInformation(_nodesFileName);

            Nodes = AddNodes(folder, cfgFolder, cancellationToken).ToList();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogErrorLoadingUserDefinedNodeFile(e, _nodesFileName, e.Message);
        }

        LogCompletedProcessingUserDefinedNodeFile();
    }

    internal static ConfigFolder DeserializeConfiguration(string json)
    {
        return JsonSerializer.Deserialize<ConfigFolder>(json, ConfigurationJsonOptions)
            ?? throw new JsonException("The nodes file must contain a configuration folder.");
    }

    private IEnumerable<NodeWithIntervals> AddNodes(
        FolderState folder, ConfigFolder cfgFolder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogCreateFolder(cfgFolder.Folder);
        cancellationToken.ThrowIfCancellationRequested();
        FolderState userNodesFolder = _plcNodeManager.CreateFolder(
            folder,
            path: cfgFolder.Folder,
            name: cfgFolder.Folder,
            NamespaceType.OpcPlcApplications);

        foreach (var node in cfgFolder.NodeList)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

            if (node.ValueRank == 1 && node.Value is JsonElement { ValueKind: JsonValueKind.Array } arrayValue)
            {
                node.Value = UpdateArrayValue(node, arrayValue);
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

        foreach (var childNode in AddFolders(userNodesFolder, cfgFolder, cancellationToken))
        {
            yield return childNode;
        }
    }

    private IEnumerable<NodeWithIntervals> AddFolders(
        FolderState folder, ConfigFolder cfgFolder, CancellationToken cancellationToken)
    {
        if (cfgFolder.FolderList is null)
        {
            yield break;
        }

        foreach (var childFolder in cfgFolder.FolderList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var node in AddNodes(folder, childFolder, cancellationToken))
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

        _plcNodeManager.CreateBaseVariable(parent, node.NodeId, node.Name, new NodeId((uint)nodeDataType), node.ValueRank, accessLevel, node.Description, NamespaceType.OpcPlcApplications, GetScalarValue(node));
    }

    internal static object GetScalarValue(ConfigNode node)
    {
        return node.DataType == "DateTime" && node.Value is string text
            ? DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            : node.Value;
    }

    internal static object UpdateArrayValue(ConfigNode node, JsonElement arrayValue)
    {
        return node.DataType switch {
            "String" => ConvertArrayValue<string>(arrayValue),
            "Boolean" => ConvertArrayValue<bool>(arrayValue),
            "Float" => ConvertArrayValue<float>(arrayValue),
            "UInt32" => ConvertArrayValue<uint>(arrayValue),
            "Int32" => ConvertArrayValue<int>(arrayValue),
            _ => throw new NotImplementedException($"Node type not implemented: {node.DataType}."),
        };
    }

    private static T[] ConvertArrayValue<T>(JsonElement arrayValue)
    {
        return arrayValue.EnumerateArray().Select(element =>
        {
            object value = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out long integer) ? (object)integer : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null when !typeof(T).IsValueType => null,
                _ => throw new JsonException($"Cannot convert {element.ValueKind} to {typeof(T).Name}."),
            };
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }).ToArray();
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
