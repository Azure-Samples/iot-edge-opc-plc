namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Tests for the per-tick value simulation of materialized WoT-Con property Variables: values
/// drift so OPC UA subscriptions observe changes, while Variables the Thing Description marks
/// non-observable or write-only stay untouched.
/// </summary>
public partial class WotConTests
{
    private const string SimulationTd = """
        {
          "@context": "https://www.w3.org/2022/wot/td/v1.1",
          "title": "SimulatedThing",
          "properties": {
            "temperature": { "type": "number" },
            "counter":     { "type": "integer" },
            "running":     { "type": "boolean" },
            "state":       { "type": "string" },
            "samples":     { "type": "array", "items": { "type": "integer" } },
            "static":      { "type": "number", "observable": false }
          }
        }
        """;

    [Test]
    public async Task SimulationTick_AdvancesMaterializedPropertyValues()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("SimAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        await UploadAndFinalizeTdAsync(fileId, SimulationTd).ConfigureAwait(false);

        var nodeIds = await ResolveMaterializedPropertiesAsync(assetId).ConfigureAwait(false);

        // Compare two adjacent ticks rather than seed-vs-tick: the seed is the tick 0 value, and
        // e.g. the boolean toggle matches it again on every even tick this fixture happens to be at.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var before = await ReadPropertyValuesAsync(nodeIds, "temperature", "counter", "running", "state", "samples").ConfigureAwait(false);

        // A single tick is enough: every generator moves on each tick.
        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var after = await ReadPropertyValuesAsync(nodeIds, "temperature", "counter", "running", "state", "samples").ConfigureAwait(false);

        after["temperature"].Should().BeOfType<double>().And.NotBe(before["temperature"]);
        after["counter"].Should().BeOfType<int>().And.NotBe(before["counter"]);
        after["running"].Should().BeOfType<bool>().And.NotBe(before["running"]);
        after["state"].Should().BeOfType<string>().And.NotBe(before["state"]);
        after["samples"].Should().BeOfType<int[]>().And.NotBeEquivalentTo((int[])before["samples"]);
    }

    [Test]
    public async Task SimulationTick_LeavesNonObservablePropertyUnchanged()
    {
        // observable:false materializes MinimumSamplingInterval = -1 to tell clients sampling is
        // unsupported, so drifting the value would contradict the Thing Description.
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("SimStaticAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        await UploadAndFinalizeTdAsync(fileId, SimulationTd).ConfigureAwait(false);

        var nodeIds = await ResolveMaterializedPropertiesAsync(assetId).ConfigureAwait(false);
        var before = await ReadPropertyValuesAsync(nodeIds, "static").ConfigureAwait(false);

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 5);

        var after = await ReadPropertyValuesAsync(nodeIds, "static").ConfigureAwait(false);
        after["static"].Should().Be(before["static"]);
    }

    [Test]
    public async Task SimulationTick_AfterTdReupload_AdvancesTheNewVariableGeneration()
    {
        // CloseAndUpdate deletes and recreates the property nodes; the tick walks the asset's
        // current NodeId table, so the replacement generation has to keep drifting.
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("SimReuploadAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);
        await UploadAndFinalizeTdAsync(fileId, SimulationTd).ConfigureAwait(false);

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 2);

        await UploadAndFinalizeTdAsync(fileId, SimulationTd).ConfigureAwait(false);

        var nodeIds = await ResolveMaterializedPropertiesAsync(assetId).ConfigureAwait(false);
        var before = await ReadPropertyValuesAsync(nodeIds, "counter").ConfigureAwait(false);

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var after = await ReadPropertyValuesAsync(nodeIds, "counter").ConfigureAwait(false);
        after["counter"].Should().NotBe(before["counter"]);
    }

    /// <summary>
    /// Browses the asset's materialized property Variables into a name → NodeId map.
    /// </summary>
    private async Task<System.Collections.Generic.Dictionary<string, NodeId>> ResolveMaterializedPropertiesAsync(NodeId assetId)
    {
        var refs = await BrowseAssetVariableChildrenAsync(assetId).ConfigureAwait(false);
        return refs.ToDictionary(r => r.BrowseName.Name, r => ToNodeId(r.NodeId), StringComparer.Ordinal);
    }

    private async Task<System.Collections.Generic.Dictionary<string, object>> ReadPropertyValuesAsync(
        System.Collections.Generic.Dictionary<string, NodeId> nodeIds,
        params string[] names)
    {
        var values = new System.Collections.Generic.Dictionary<string, object>(StringComparer.Ordinal);

        foreach (string name in names)
        {
            nodeIds.Should().ContainKey(name);
            var dataValue = await ReadDataValueAsync(nodeIds[name]).ConfigureAwait(false);
            StatusCode.IsGood(dataValue.StatusCode).Should().BeTrue("reading '{0}' should succeed", name);
            values[name] = dataValue.Value;
        }

        return values;
    }
}
