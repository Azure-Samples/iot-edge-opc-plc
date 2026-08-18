namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.TimeSpan;

/// <summary>
/// Verifies that the value simulation reaches OPC UA clients: a subscription on a materialized
/// WoT-Con property Variable receives a data-change notification when the simulation ticks.
/// </summary>
/// <remarks>
/// Lives in its own fixture because <see cref="SubscriptionTestsBase"/> creates a subscription
/// per test, which the rest of the WoT-Con suite does not need — the same split
/// <c>PumpTests</c> / <c>PumpEventTests</c> uses.
/// </remarks>
[TestFixture]
public class WotConValueSubscriptionTests : SubscriptionTestsBase
{
    private const uint WotAssetConnectionManagementObjectId = 31;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint FileCloseAndUpdateTypeMethodId = 111;

    public WotConValueSubscriptionTests()
        : base(["--wotcon"])
    {
    }

    private ushort WotConNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);

    [Test]
    public async Task Subscription_OnMaterializedProperty_ReceivesValueChange()
    {
        var (assetId, fileId) = await CreateAssetAndResolveFileAsync("SubAsset_" + Guid.NewGuid().ToString("N")[..8]).ConfigureAwait(false);

        const string td = """
            {
              "@context": "https://www.w3.org/2022/wot/td/v1.1",
              "title": "SubscribedThing",
              "properties": {
                "counter": { "type": "integer" }
              }
            }
            """;
        await UploadAndFinalizeTdAsync(fileId, td).ConfigureAwait(false);

        NodeId counterId = await ResolvePropertyAsync(assetId, "counter").ConfigureAwait(false);

        SetUpMonitoredItem(counterId, NodeClass.Variable, Attributes.Value);
        await AddMonitoredItemAsync().ConfigureAwait(false);

        // Drop the initial-value notification the server sends on subscribe.
        ReceiveAtMostEvents(1);
        ClearEvents();

        FireTimersWithPeriod(FromSeconds(1), numberOfTimes: 1);

        var notifications = ReceiveEvents(1).ToList();
        var notification = notifications[0].NotificationValue.Should().BeOfType<MonitoredItemNotification>().Subject;
        notification.Value.Value.Should().BeOfType<int>();
        StatusCode.IsGood(notification.Value.StatusCode).Should().BeTrue();
    }

    private NodeId WotConNodeId(uint identifier) => new(identifier, WotConNamespaceIndex);

    /// <summary>
    /// Creates an asset and resolves its per-asset WoTFile child.
    /// </summary>
    private async Task<(NodeId AssetId, NodeId FileId)> CreateAssetAndResolveFileAsync(string assetName)
    {
        var (createStatus, outputs) = await CallAsync(
            objectId: WotConNodeId(WotAssetConnectionManagementObjectId),
            methodId: WotConNodeId(CreateAssetMethodInstanceId),
            arguments: new VariantCollection { new Variant(assetName) }).ConfigureAwait(false);
        StatusCode.IsGood(createStatus).Should().BeTrue("CreateAsset should succeed, got {0}", createStatus);

        var assetId = outputs[0].Value as NodeId;
        assetId.Should().NotBeNull();

        NodeId fileId = await TranslateChildAsync(assetId, new QualifiedName("WoTFile", WotConNamespaceIndex)).ConfigureAwait(false);
        return (assetId, fileId);
    }

    /// <summary>
    /// Opens the asset's WoTFile, writes the Thing Description, and finalizes it with
    /// CloseAndUpdate so the properties materialize.
    /// </summary>
    private async Task UploadAndFinalizeTdAsync(NodeId fileId, string td)
    {
        var (openStatus, openOutputs) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Open, 0),
            arguments: new VariantCollection { new Variant((byte)6) }).ConfigureAwait(false);
        StatusCode.IsGood(openStatus).Should().BeTrue("Open should succeed, got {0}", openStatus);
        uint handle = Convert.ToUInt32(openOutputs[0].Value);

        var (writeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: new NodeId(Methods.FileType_Write, 0),
            arguments: new VariantCollection
            {
                new Variant(handle),
                new Variant(System.Text.Encoding.UTF8.GetBytes(td)),
            }).ConfigureAwait(false);
        StatusCode.IsGood(writeStatus).Should().BeTrue("Write should succeed, got {0}", writeStatus);

        var (closeStatus, _) = await CallAsync(
            objectId: fileId,
            methodId: WotConNodeId(FileCloseAndUpdateTypeMethodId),
            arguments: new VariantCollection { new Variant(handle) }).ConfigureAwait(false);
        StatusCode.IsGood(closeStatus).Should().BeTrue("CloseAndUpdate should succeed, got {0}", closeStatus);
    }

    private Task<NodeId> ResolvePropertyAsync(NodeId assetId, string propertyName)
        => TranslateChildAsync(assetId, new QualifiedName(propertyName, WotConNamespaceIndex));

    private async Task<NodeId> TranslateChildAsync(NodeId startingNode, QualifiedName targetName)
    {
        var browsePath = new BrowsePath
        {
            StartingNode = startingNode,
            RelativePath = new RelativePath
            {
                Elements =
                {
                    new RelativePathElement
                    {
                        ReferenceTypeId = ReferenceTypeIds.HasComponent,
                        IsInverse = false,
                        IncludeSubtypes = true,
                        TargetName = targetName,
                    },
                },
            },
        };

        var response = await Session.TranslateBrowsePathsToNodeIdsAsync(
            null,
            new BrowsePathCollection { browsePath },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        var result = response.Results[0];
        StatusCode.IsGood(result.StatusCode).Should().BeTrue("resolving '{0}' should succeed, got {1}", targetName.Name, result.StatusCode);
        result.Targets.Should().ContainSingle();
        return ToNodeId(result.Targets[0].TargetId);
    }

    private async Task<(StatusCode Status, VariantCollection Outputs)> CallAsync(
        NodeId objectId,
        NodeId methodId,
        VariantCollection arguments)
    {
        var request = new CallMethodRequest
        {
            ObjectId = objectId,
            MethodId = methodId,
            InputArguments = arguments,
        };

        var response = await Session.CallAsync(
            null,
            new CallMethodRequestCollection { request },
            CancellationToken.None).ConfigureAwait(false);

        response.Results.Should().ContainSingle();
        var result = response.Results[0];
        return (result.StatusCode, result.OutputArguments ?? new VariantCollection());
    }
}
