namespace OpcPlc.PluginNodes;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Helpers;
using OpcPlc.PluginNodes.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Nodes with 1 kB (ByteString) values.
/// The first byte cycles from 0 to 255 in a configurable rate in ms.
/// The values are deterministic but scrambled to ensure that they are not efficiently compressed.
/// </summary>
public class VeryFast1KBPluginNodes(TimeService timeService, ILogger logger) : PluginNodeBase(timeService, logger), IPluginNodes
{
    private uint NodeCount { get; set; } = 1;
    private uint NodeRate { get; set; } = 1000; // ms.

    private readonly DeterministicGuid _deterministicGuid = new();
    private PlcNodeManager _plcNodeManager;
    private BaseDataVariableState[] _veryFast1KBNodes;
    private ITimer _nodeGenerator;

    public void AddOptions(Mono.Options.OptionSet optionSet)
    {
        optionSet.Add(
            "vf1k|veryfast1knodes=",
            $"number of very fast 1 kB nodes (Deprecated: Use veryfastbsnodes).\nDefault: {NodeCount}",
            (uint i) => NodeCount = i);

        optionSet.Add(
            "vf1kr|veryfast1krate=",
            $"rate in ms to change very fast 1 kB nodes (Deprecated: Use veryfastbsrate).\nDefault: {NodeRate}",
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
            path: "VeryFast1kB",
            name: "VeryFast1kB",
            NamespaceType.OpcPlcApplications);

        AddNodes(folder);
    }

    public void StartSimulation()
    {
        // Only use the fast timers when we need to go really fast,
        // since they consume more resources and create an own thread.
        _nodeGenerator = NodeRate >= 50 || !Stopwatch.IsHighResolution
            ? _timeService.NewTimer((s, e) => UpdateNodes(), intervalInMilliseconds: NodeRate)
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
        _veryFast1KBNodes = new BaseDataVariableState[NodeCount];

        for (int i = 0; i < NodeCount; i++)
        {
            string oneKbGuid = GetLongDeterministicGuid(maxLength: 1024);
            var initialByteArray = Encoding.UTF8.GetBytes(oneKbGuid);

            string name = $"VeryFast1kB{(i + 1)}";

            _veryFast1KBNodes[i] = _plcNodeManager.CreateBaseVariable(
                folder,
                path: name,
                name: name,
                new NodeId((uint)BuiltInType.ByteString),
                ValueRanks.Scalar,
                AccessLevels.CurrentReadOrWrite,
                "Very fast changing 1 kB node",
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
        for (int i = 0; i < _veryFast1KBNodes.Length; i++)
        {
            byte[] arrayValue = (byte[])_veryFast1KBNodes[i].Value;

            // Update first byte in the range 0 to 255.
            arrayValue[0] = arrayValue[0] == 255
                ? (byte)0
                : (byte)(arrayValue[0] + 1);

            SetValue(_veryFast1KBNodes[i], arrayValue);
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
