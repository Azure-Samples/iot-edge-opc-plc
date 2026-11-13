namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Verifies Server/Namespaces describes each namespace once, using the metadata of the imported NodeSets.
/// </summary>
public class NamespaceMetadataTests() : SimulatorTestsBase(["--sl", "--pumps", "--wotcon"])
{
    [Test]
    public async Task ServerNamespaces_HasOneMetadataObjectPerNamespace()
    {
        string[] namespaces = (await ReadDataValueAsync(VariableIds.Server_NamespaceArray).ConfigureAwait(false))
            .WrappedValue.GetStringArray().ToArray();

        List<(NodeId NodeId, string Uri)> metadata = await ReadMetadataObjectsAsync().ConfigureAwait(false);

        metadata.Select(m => m.Uri).Should().BeEquivalentTo(
            namespaces, "every namespace should be described by exactly one metadata object");
    }

    [TestCase("http://opcfoundation.org/UA/DI/", "1.03.1", "2021-09-07")]
    [TestCase("http://opcfoundation.org/UA/IA/", "1.01.4", "2025-05-23")]
    [TestCase("http://opcfoundation.org/UA/Machinery/", "1.04.1", "2026-01-01")]
    [TestCase("http://opcfoundation.org/UA/Pumps/", "1.0.0", "2021-04-19")]
    [TestCase("http://opcfoundation.org/UA/WoT-Con/", "1.02.0", "2025-12-05")]
    [TestCase("http://microsoft.com/Opc/OpcPlc/Boiler", "1.00", "2023-05-12")]
    public async Task NodeSetNamespace_PublishesImportedMetadata(string namespaceUri, string version, string date)
    {
        NodeId metadata = (await ReadMetadataObjectsAsync().ConfigureAwait(false))
            .Where(m => m.Uri == namespaceUri)
            .Should().ContainSingle().Subject.NodeId;

        (await ReadPropertyAsync<string>(metadata, BrowseNames.NamespaceVersion).ConfigureAwait(false))
            .Should().Be(version);
        (await ReadPropertyAsync<DateTimeUtc>(metadata, BrowseNames.NamespacePublicationDate).ConfigureAwait(false))
            .Should().Be(DateTimeUtc.From(DateTime.Parse(
                date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal)));
        (await BrowseAsync(metadata, ReferenceTypeIds.HasProperty).ConfigureAwait(false))
            .Should().OnlyHaveUniqueItems();
    }

    private async Task<List<(NodeId NodeId, string Uri)>> ReadMetadataObjectsAsync()
    {
        List<(NodeId NodeId, string Uri)> metadata = [];
        foreach (NodeId nodeId in await BrowseAsync(ObjectIds.Server_Namespaces, ReferenceTypeIds.HierarchicalReferences)
            .ConfigureAwait(false))
        {
            metadata.Add((nodeId, await ReadPropertyAsync<string>(nodeId, BrowseNames.NamespaceUri).ConfigureAwait(false)));
        }

        return metadata;
    }

    private async Task<T> ReadPropertyAsync<T>(NodeId metadata, string browseName)
    {
        NodeId property = await FindNodeAsync(metadata, Opc.Ua.Namespaces.OpcUa, browseName).ConfigureAwait(false);
        return await ReadValueAsync<T>(property).ConfigureAwait(false);
    }

    private async Task<List<NodeId>> BrowseAsync(NodeId nodeId, NodeId referenceTypeId)
    {
        BrowseResponse response = await Session.BrowseAsync(null, null, 0,
            [
                new BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = referenceTypeId,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All,
                }
            ], CancellationToken.None).ConfigureAwait(false);

        return [.. response.Results.ToArray().Single().References.ToArray().Select(r => ToNodeId(r.NodeId))];
    }
}
