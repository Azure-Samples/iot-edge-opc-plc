namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Nodes with configurable kB (ByteString) values.
/// The first byte cycles from 0 to 255 in a configurable rate in ms.
/// The values are deterministic but scrambled to ensure that they are not efficiently compressed.
/// </summary>
public class VeryFastByteStringPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private uint NodeCount { get; set; } = 1;
    private uint NodeSize { get; set; } = 1024; // Bytes.
    private uint NodeRate { get; set; } = 1000; // ms.

    private readonly DeterministicGuid _deterministicGuid = new();
    private PlcNodeManager _plcNodeManager;
    private BaseDataVariableState[] _veryFastByteStringNodes;
    private ITimer _nodeGenerator;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "vfbs|veryfastbsnodes=",
            $"number of very fast ByteString nodes.\nDefault: {NodeCount}",
            (uint i) => NodeCount = i);

        optionSet.Add(
            "vfbss|veryfastbssize=",
            $"size in bytes to change very fast ByteString nodes (min. 1).\nDefault: {NodeSize}",
            (uint i) => NodeSize = i);

        optionSet.Add(
            "vfbsr|veryfastbsrate=",
            $"rate in ms to change very fast ByteString nodes.\nDefault: {NodeRate}",
            (uint i) => NodeRate = i);
    }

    public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
    {
        _plcNodeManager = plcNodeManager;

        if (NodeCount == 0)
        {
            return;
        }

        FolderState folder = _plcNodeManager.CreateFolder(
            telemetryFolder,
            path: "VeryFastByteString",
            name: "VeryFastByteString",
            NamespaceType.OpcPlcApplications);

        AddNodes(folder);
    }

    public void StartSimulation()
    {
        // Only use the fast timers when we need to go really fast,
        // since they consume more resources and create an own thread.
        _nodeGenerator = NodeRate >= 50 || !Stopwatch.IsHighResolution
            ? _timeService.NewTimer((s, e) => UpdateNodes(), NodeRate)
            : _timeService.NewFastTimer((s, e) => UpdateNodes(), intervalInMilliseconds: NodeRate);
    }

    public void StopSimulation()
    {
        if (_nodeGenerator != null)
        {
            _nodeGenerator.Enabled = false;
        }
    }

    private void AddNodes(FolderState folder)
    {
        var nodes = new List<NodeWithIntervals>();
        _veryFastByteStringNodes = new BaseDataVariableState[NodeCount];

        int nodeSize = NodeSize < 1
            ? 1
            : (int)NodeSize;

        for (int i = 0; i < NodeCount; i++)
        {
            string nodeSizeGuid = GetLongDeterministicGuid(nodeSize);
            var initialByteArray = Encoding.UTF8.GetBytes(nodeSizeGuid);

            string name = $"VeryFastByteString{(i + 1)}";

            _veryFastByteStringNodes[i] = _plcNodeManager.CreateBaseVariable(
                folder,
                path: name,
                name: name,
                new NodeId((uint)BuiltInType.ByteString),
                ValueRanks.Scalar,
                AccessLevels.CurrentReadOrWrite,
                "Very fast changing ByteString node",
                NamespaceType.OpcPlcApplications,
                initialByteArray);

            // Update pn.json output.
            nodes.Add(new NodeWithIntervals {
                NodeId = name,
                Namespace = OpcPlc.Namespaces.OpcPlcApplications,
                PublishingInterval = NodeRate,
            });

            Nodes = nodes;
        }
    }

    private void UpdateNodes()
    {
        for (int i = 0; i < _veryFastByteStringNodes.Length; i++)
        {
            byte[] arrayValue = (byte[])_veryFastByteStringNodes[i].Value;

            // Update first byte in the range 0 to 255.
            arrayValue[0] = arrayValue[0] == 255
                ? (byte)0
                : (byte)(arrayValue[0] + 1);

            SetValue(_veryFastByteStringNodes[i], arrayValue);
        }
    }

    private void SetValue<T>(BaseVariableState variable, T value)
    {
        variable.Value = value;
        variable.Timestamp = _timeService.Now();
        variable.ClearChangeMasks(_plcNodeManager.SystemContext, includeChildren: false);
    }

    private string GetLongDeterministicGuid(int maxLength)
    {
        var sb = new StringBuilder();

        while (sb.Length < maxLength)
        {
            sb.Append(_deterministicGuid.NewGuid().ToString());
        }

        return sb.ToString()[..maxLength];
    }
}
