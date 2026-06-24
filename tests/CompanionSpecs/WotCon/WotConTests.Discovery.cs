namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Discovery / metadata tests: namespace registration and the entry-point object
/// (<c>WoTAssetConnectionManagement</c>, i=31) exposing its methods.
/// </summary>
public partial class WotConTests
{
    [Test]
    public void WotConNamespace_IsRegistered()
    {
        WotConNamespaceIndex.Should().BeGreaterThan((ushort)0,
            "the WoT-Con namespace should be registered when --wotcon is set");
    }

    [Test]
    public async Task WotAssetConnectionManagement_HasCreateAssetMethod()
    {
        var browseDescription = new BrowseDescription
        {
            NodeId = WotConNodeId(WotAssetConnectionManagementObjectId),
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HasComponent,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Method,
            ResultMask = (uint)BrowseResultMask.All,
        };

        var results = await Session.BrowseAsync(
            null,
            null,
            0,
            new BrowseDescriptionCollection { browseDescription },
            CancellationToken.None).ConfigureAwait(false);

        results.Results.Should().ContainSingle();
        var references = results.Results[0].References;
        references.Should().Contain(
            r => r.BrowseName.Name == "CreateAsset",
            "CreateAsset method must be a child of WoTAssetConnectionManagement");
    }
}
