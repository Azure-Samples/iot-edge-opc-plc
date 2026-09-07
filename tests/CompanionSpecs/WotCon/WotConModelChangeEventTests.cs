namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Verifies model-change events raised by the WoT asset-management methods.
/// </summary>
[TestFixture]
public class WotConModelChangeEventTests : SubscriptionTestsBase
{
    private const uint WotAssetConnectionManagementObjectId = 31;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint DeleteAssetMethodInstanceId = 35;
    private const uint CreateAssetForEndpointTypeMethodId = 49;

    private NodeId _eventType;

    public WotConModelChangeEventTests() : base(["--wotcon"])
    {
    }

    private ushort WotConNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);

    [SetUp]
    public async Task CreateMonitoredItem()
    {
        _eventType = ObjectTypeIds.GeneralModelChangeEventType;

        SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

        var filter = (EventFilter)MonitoredItem.Filter;
        filter.WhereClause.Push(FilterOperator.OfType, _eventType);
        filter.SelectClauses.Add(new SimpleAttributeOperand
        {
            TypeDefinitionId = _eventType,
            BrowsePath = [new QualifiedName(BrowseNames.Changes)],
            AttributeId = Attributes.Value,
        });

        await AddMonitoredItemAsync().ConfigureAwait(false);
    }

    [TearDown]
    public void RemoveMonitoredItem()
    {
        Session.DefaultSubscription.RemoveItem(MonitoredItem);
    }

    [Test]
    public async Task CreateAsset_ReportsNodeAdded()
    {
        ClearEvents();

        var assetId = await CreateAssetAsync("ModelChangeCreate_" + Guid.NewGuid().ToString("N")[..8])
            .ConfigureAwait(false);

        AssertModelChange(assetId, ModelChangeStructureVerbMask.NodeAdded);
    }

    [Test]
    public async Task CreateAssetForEndpoint_ReportsNodeAdded()
    {
        ClearEvents();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (status, outputs) = await CallAsync(
            WotConNodeId(WotAssetConnectionManagementObjectId),
            WotConNodeId(CreateAssetForEndpointTypeMethodId),
            [
                new Variant("ModelChangeEndpoint_" + suffix),
                new Variant($"opc.tcp://model-change-{suffix}.invalid:4840"),
            ]).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue("CreateAssetForEndpoint should succeed, got {0}", status);
        var assetId = outputs.Should().ContainSingle().Subject.Value as NodeId;
        NodeId.IsNull(assetId).Should().BeFalse();

        AssertModelChange(assetId, ModelChangeStructureVerbMask.NodeAdded);
    }

    [Test]
    public async Task DeleteAsset_ReportsNodeDeleted()
    {
        ClearEvents();

        var assetId = await CreateAssetAsync("ModelChangeDelete_" + Guid.NewGuid().ToString("N")[..8])
            .ConfigureAwait(false);

        var (status, _) = await CallAsync(
            WotConNodeId(WotAssetConnectionManagementObjectId),
            WotConNodeId(DeleteAssetMethodInstanceId),
            [new Variant(assetId)]).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue("DeleteAsset should succeed, got {0}", status);

        AssertModelChange(assetId, ModelChangeStructureVerbMask.NodeDeleted, expectedEventCount: 2);
    }

    private void AssertModelChange(
        NodeId assetId,
        ModelChangeStructureVerbMask expectedVerb,
        int expectedEventCount = 1)
    {
        var receivedEvents = ReceiveAtMostEvents(expectedEventCount)
            .Select(value => (EventFieldList)value.NotificationValue)
            .Select(EventFieldListToDictionary)
            .ToList();
        var eventFields = receivedEvents.Should().ContainSingle(
            fields => GetChanges(fields).Any(change =>
                change.Affected == assetId
                && ((ModelChangeStructureVerbMask)change.Verb).HasFlag(expectedVerb))).Subject;
        eventFields.Should().Contain(new Dictionary<string, object>
        {
            ["/EventType"] = _eventType,
            ["/SourceNode"] = Server,
            ["/SourceName"] = "Server",
        });

        var change = GetChanges(eventFields).Should().ContainSingle().Subject;
        change.Affected.Should().Be(assetId);
        change.AffectedType.Should().Be(ObjectTypeIds.BaseObjectType);
        ((ModelChangeStructureVerbMask)change.Verb).Should().HaveFlag(expectedVerb);
    }

    private static ModelChangeStructureDataType[] GetChanges(Dictionary<string, object> eventFields)
    {
        return eventFields["/Changes"] switch
        {
            ModelChangeStructureDataType[] values => values,
            ExtensionObject[] values => values.Select(value => value.Body)
                .OfType<ModelChangeStructureDataType>()
                .ToArray(),
            object value => throw new AssertionException(
                $"Expected model-change structures but received {value?.GetType().FullName ?? "null"}."),
        };
    }

    private async Task<NodeId> CreateAssetAsync(string assetName)
    {
        var (status, outputs) = await CallAsync(
            WotConNodeId(WotAssetConnectionManagementObjectId),
            WotConNodeId(CreateAssetMethodInstanceId),
            [new Variant(assetName)]).ConfigureAwait(false);

        StatusCode.IsGood(status).Should().BeTrue("CreateAsset should succeed, got {0}", status);
        var assetId = outputs.Should().ContainSingle().Subject.Value as NodeId;
        NodeId.IsNull(assetId).Should().BeFalse();
        return assetId;
    }

    private async Task<(StatusCode Status, VariantCollection Outputs)> CallAsync(
        NodeId objectId,
        NodeId methodId,
        VariantCollection arguments)
    {
        var response = await Session.CallAsync(
            null,
            new CallMethodRequestCollection
            {
                new CallMethodRequest
                {
                    ObjectId = objectId,
                    MethodId = methodId,
                    InputArguments = arguments,
                },
            },
            CancellationToken.None).ConfigureAwait(false);

        var result = response.Results.Should().ContainSingle().Subject;
        return (result.StatusCode, result.OutputArguments ?? []);
    }

    private NodeId WotConNodeId(uint identifier) => new(identifier, WotConNamespaceIndex);
}
