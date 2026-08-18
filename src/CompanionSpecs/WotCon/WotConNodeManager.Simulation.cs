// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

/// <summary>
/// Per-tick value simulation for materialized WoT-Con property Variables, so OPC UA
/// subscriptions on a WoT asset observe value changes rather than the single seed written at
/// materialization time.
/// </summary>
public partial class WotConNodeManager
{
    /// <summary>
    /// Simulation period. Matches the pump / stacklight simulations, and is the rate the
    /// materialized Variables advertise through <c>MinimumSamplingInterval</c>.
    /// </summary>
    internal const uint SimulationIntervalMilliseconds = 1000;

    private ITimer _simulationTimer;
    private long _simulationTick;

    /// <summary>
    /// Starts the value simulation. Safe to call when no asset exists yet: the tick walks the
    /// (initially empty) asset registry, so assets created later are picked up automatically.
    /// </summary>
    private void StartValueSimulation()
    {
        _simulationTimer ??= _timeService.NewTimer(
            (_, _) => AdvanceMaterializedValues(),
            SimulationIntervalMilliseconds);
    }

    /// <summary>
    /// Advances every materialized property Variable of every live asset by one tick.
    /// </summary>
    /// <remarks>
    /// Each asset is advanced under its <see cref="WotAsset.LifecycleLock"/> — the same lock
    /// <c>CloseAndUpdate</c> materialization and <c>DeleteAsset</c> teardown hold — so the tick
    /// can never write into a node generation that is being replaced or removed. Assets are
    /// snapshotted first because <c>_assets</c> can be mutated concurrently.
    /// </remarks>
    private void AdvanceMaterializedValues()
    {
        long tick = ++_simulationTick;
        DateTime utcNow = _timeService.UtcNow();

        try
        {
            foreach (var asset in _assets.Values)
            {
                lock (asset.LifecycleLock)
                {
                    if (asset.IsDeleted)
                    {
                        continue;
                    }

                    foreach (var nodeId in asset.MaterializedPropertyNodeIds.Values)
                    {
                        AdvanceVariable(nodeId, tick, utcNow);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // A throwing timer callback would tear down the simulation for every asset.
            _logger?.LogError(ex, "[WotCon] Value simulation tick {Tick} failed", tick);
        }
    }

    private void AdvanceVariable(NodeId nodeId, long tick, DateTime utcNow)
    {
        var node = FindPredefinedNode<BaseDataVariableState>(nodeId);
        if (node == null)
        {
            return;
        }

        // observable=false materializes MinimumSamplingInterval=-1 to tell clients the Variable
        // does not support sampling; drifting it would contradict the Thing Description.
        // Write-only properties carry no readable value to observe.
        if (node.MinimumSamplingInterval < 0 || (node.AccessLevel & AccessLevels.CurrentRead) == 0)
        {
            return;
        }

        node.Value = WotMockValueGenerator.Advance(node.DataType, node.ValueRank, tick, utcNow);
        node.Timestamp = utcNow;
        node.ClearChangeMasks(SystemContext, includeChildren: false);
    }
}
