namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc.CompanionSpecs.WotCon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

[TestFixture]
public class WotConMigrationTests
{
    [Test]
    public async Task AddressSpace_ImportsManagementAndOptionalMembersAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);

        fixture.Logger.Errors.Should().BeEmpty();
        fixture.Management.NodeId.Should().Be(fixture.Id(31));
        MethodState create = fixture.Find<MethodState>(fixture.Id(32));
        create.InputArguments.Value.ToArray().Should().ContainSingle().Which.DataType.Should().Be(DataTypeIds.String);
        create.OutputArguments.Value.ToArray().Should().ContainSingle().Which.DataType.Should().Be(DataTypeIds.NodeId);
        fixture.Child<PropertyState<ArrayOf<string>>>(fixture.Management, "SupportedWoTBindings")
            .Value.ToArray().Should().Contain("https://opcfoundation.org/OpcPlc/simulator");
        BaseObjectState configuration = fixture.Child<BaseObjectState>(fixture.Management, "Configuration");
        fixture.Child<PropertyState<string>>(configuration, "License").Value.Should().Be("MIT");
        fixture.Child<MethodState>(fixture.Management, "DiscoverAssets").OutputArguments.Value.Count.Should().Be(1);
    }

    [Test]
    public async Task CreateDelete_PreservesDuplicateAndNotFoundStatusesAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        (BaseObjectState asset, FileState file) = await fixture.CreateAssetAsync("Pump").ConfigureAwait(false);
        file.TypeDefinitionId.Should().Be(fixture.Id(110));
        file.Writable.Value.Should().BeTrue();
        file.UserWritable.Value.Should().BeTrue();
        file.MimeType.Value.Should().Be("application/td+json");
        file.LastModifiedTime.Should().NotBeNull();
        file.MaxByteStringLength.NodeId.NamespaceIndex.Should().Be(fixture.NamespaceIndex);
        fixture.Find<PropertyState<uint>>(file.MaxByteStringLength.NodeId).Should().BeSameAs(file.MaxByteStringLength);
        file.Open.NodeId.NamespaceIndex.Should().Be(fixture.NamespaceIndex);
        var duplicate = await fixture.CallAsync(fixture.Management,
            fixture.Find<MethodState>(fixture.Id(32)), new Variant("Pump")).ConfigureAwait(false);
        duplicate.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadBrowseNameDuplicated);
        var deleted = await fixture.CallAsync(fixture.Management,
            fixture.Find<MethodState>(fixture.Id(35)), new Variant(asset.NodeId)).ConfigureAwait(false);
        ServiceResult.IsGood(deleted.Status).Should().BeTrue("DeleteAsset: {0}; logs: {1}",
            deleted.Status, string.Join("\n", fixture.Logger.Errors));
        fixture.Find<NodeState>(asset.NodeId).Should().BeNull();
        fixture.Find<NodeState>(file.NodeId).Should().BeNull();
        var missing = await fixture.CallAsync(fixture.Management,
            fixture.Find<MethodState>(fixture.Id(35)), new Variant(asset.NodeId)).ConfigureAwait(false);
        missing.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadNotFound);
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task File_RoundTripsBytesPositionAndOpenCountAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, file) = await fixture.CreateAssetAsync("File").ConfigureAwait(false);
        uint handle = await fixture.OpenAsync(file).ConfigureAwait(false);
        file.OpenCount.Value.Should().Be(1);
        ByteString payload = (ByteString)Encoding.UTF8.GetBytes("payload");
        var write = await fixture.CallAsync(file, file.Write, new Variant(handle), new Variant(payload))
            .ConfigureAwait(false);
        ServiceResult.IsGood(write.Status).Should().BeTrue();
        file.Size.Value.Should().Be(7);
        var position = await fixture.CallAsync(file, file.GetPosition, new Variant(handle)).ConfigureAwait(false);
        position.Outputs[0].GetUInt64().Should().Be(7);
        var seek = await fixture.CallAsync(file, file.SetPosition, new Variant(handle), new Variant(0UL))
            .ConfigureAwait(false);
        ServiceResult.IsGood(seek.Status).Should().BeTrue();
        var read = await fixture.CallAsync(file, file.Read, new Variant(handle), new Variant(20)).ConfigureAwait(false);
        ServiceResult.IsGood(read.Status).Should().BeTrue();
        read.Outputs[0].TypeInfo.BuiltInType.Should().Be(BuiltInType.ByteString);
        read.Outputs[0].GetByteString().Should().Be(payload);
        var close = await fixture.CallAsync(file, file.Close, new Variant(handle)).ConfigureAwait(false);
        ServiceResult.IsGood(close.Status).Should().BeTrue();
        file.OpenCount.Value.Should().Be(0);
    }

    [Test]
    public async Task Upload_MaterializesPropertiesActionsAndDeterministicSimulationAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (asset, file) = await fixture.CreateAssetAsync("Pump").ConfigureAwait(false);
        const string description = """
            {"title":"Pump","base":"opc.tcp://pump","properties":{
              "temperature":{"type":"number","unit":"degC","readOnly":true},
              "levels":{"type":"array","items":{"type":"integer"}},
              "quiet":{"type":"integer","observable":false},
              "command":{"type":"string","writeOnly":true}},
             "actions":{"reset":{"input":{"type":"string"},"output":{"type":"boolean"}}}}
            """;
        ServiceResult result = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(description)).ConfigureAwait(false);
        ServiceResult.IsGood(result).Should().BeTrue();
        BaseDataVariableState temperature = fixture.Child<BaseDataVariableState>(asset, "temperature");
        BaseDataVariableState levels = fixture.Child<BaseDataVariableState>(asset, "levels");
        BaseDataVariableState quiet = fixture.Child<BaseDataVariableState>(asset, "quiet");
        BaseDataVariableState command = fixture.Child<BaseDataVariableState>(asset, "command");
        temperature.Value.GetDouble().Should().Be(42);
        temperature.AccessLevel.Should().Be(AccessLevels.CurrentRead);
        fixture.Child<PropertyState<EUInformation>>(temperature, BrowseNames.EngineeringUnits)
            .Value.DisplayName.Text.Should().Be("degC");
        levels.Value.GetInt32Array().ToArray().Should().Equal(100, 101, 102);
        fixture.Tick();
        temperature.Value.GetDouble().Should().Be(42.998);
        temperature.Timestamp.Should().Be((DateTimeUtc)fixture.Now);
        levels.Value.GetInt32Array().ToArray().Should().Equal(101, 102, 103);
        quiet.Value.GetInt32().Should().Be(100);
        command.Value.GetString().Should().Be("idle");
        MethodState action = fixture.Child<MethodState>(asset, "reset");
        var actionResult = await fixture.CallAsync(asset, action, new Variant("reset")).ConfigureAwait(false);
        ServiceResult.IsGood(actionResult.Status).Should().BeTrue();
        actionResult.Outputs.Should().ContainSingle().Which.GetBoolean().Should().BeTrue();
        fixture.Child<PropertyState<string>>(asset, "AssetEndpoint").Value.Should().Be("opc.tcp://pump");
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task CreateAsset_RejectsEmptyNamesAsync(string name)
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var result = await fixture.CallAsync(fixture.Management, fixture.Find<MethodState>(fixture.Id(32)),
            new Variant(name)).ConfigureAwait(false);

        result.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadBrowseNameInvalid);
    }

    [Test]
    public async Task OptionalManagement_DiscoversSortedUniqueEndpointsAndTestsConnectionsAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        MethodState create = fixture.Child<MethodState>(fixture.Management, "CreateAssetForEndpoint");
        foreach (var asset in new[] { ("Z", "opc.tcp://z"), ("A", "opc.tcp://a"), ("A2", "opc.tcp://a") })
        {
            var created = await fixture.CallAsync(fixture.Management, create,
                new Variant(asset.Item1), new Variant(asset.Item2)).ConfigureAwait(false);
            ServiceResult.IsGood(created.Status).Should().BeTrue("{0}", created.Status);
        }

        var discovered = await fixture.CallAsync(fixture.Management,
            fixture.Child<MethodState>(fixture.Management, "DiscoverAssets")).ConfigureAwait(false);
        ServiceResult.IsGood(discovered.Status).Should().BeTrue();
        discovered.Outputs[0].GetStringArray().ToArray().Should().Equal("opc.tcp://a", "opc.tcp://z");
        MethodState connection = fixture.Child<MethodState>(fixture.Management, "ConnectionTest");
        var known = await fixture.CallAsync(fixture.Management, connection, new Variant("opc.tcp://a"))
            .ConfigureAwait(false);
        known.Outputs.Should().Equal(new Variant(true), new Variant("Simulated"));
        var unknown = await fixture.CallAsync(fixture.Management, connection, new Variant("opc.tcp://missing"))
            .ConfigureAwait(false);
        unknown.Outputs.Should().Equal(new Variant(false), new Variant("UnknownEndpoint"));
        var invalid = await fixture.CallAsync(fixture.Management, connection, new Variant(" ")).ConfigureAwait(false);
        invalid.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidArgument);
    }

    [TestCase("")]
    [TestCase("{invalid")]
    [TestCase("{}")]
    public async Task Upload_RejectsInvalidDescriptionAndClosesHandleAsync(string json)
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, file) = await fixture.CreateAssetAsync("Invalid").ConfigureAwait(false);

        ServiceResult result = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(json)).ConfigureAwait(false);

        result.StatusCode.Should().Be((StatusCode)StatusCodes.BadDecodingError);
        file.OpenCount.Value.Should().Be(0);
    }

    [Test]
    public async Task Upload_RejectsMalformedUtf8AndUnsupportedBindingAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, file) = await fixture.CreateAssetAsync("Invalid").ConfigureAwait(false);
        ServiceResult utf8 = await fixture.UploadAsync(file, [0xC3, 0x28]).ConfigureAwait(false);
        utf8.StatusCode.Should().Be((StatusCode)StatusCodes.BadDecodingError);
        ServiceResult binding = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(
            """{"title":"Invalid","@context":"https://www.w3.org/2019/wot/modbus"}""")).ConfigureAwait(false);
        binding.StatusCode.Should().Be((StatusCode)StatusCodes.BadNotSupported);
        file.OpenCount.Value.Should().Be(0);
    }

    [Test]
    public async Task File_RejectsUnknownHandlesAndOversizedWritesAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, file) = await fixture.CreateAssetAsync("Limits").ConfigureAwait(false);
        MethodState closeAndUpdate = fixture.Child<MethodState>(file, "CloseAndUpdate");
        var unknown = await fixture.CallAsync(file, closeAndUpdate, new Variant(9999u)).ConfigureAwait(false);
        unknown.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidState);
        var read = await fixture.CallAsync(file, file.Read, new Variant(9999u), new Variant(10)).ConfigureAwait(false);
        read.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidArgument);
        var position = await fixture.CallAsync(file, file.GetPosition, new Variant(9999u)).ConfigureAwait(false);
        position.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadInvalidArgument);
        file.MaxByteStringLength.Should().NotBeNull();
        file.MaxByteStringLength.Value.Should().Be(65536u);
        uint handle = await fixture.OpenAsync(file).ConfigureAwait(false);
        var tooLarge = await fixture.CallAsync(file, file.Write, new Variant(handle),
            new Variant((ByteString)new byte[65537])).ConfigureAwait(false);
        tooLarge.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadRequestTooLarge);
        file.Size.Value.Should().Be(0);
        var close = await fixture.CallAsync(file, file.Close, new Variant(handle)).ConfigureAwait(false);
        ServiceResult.IsGood(close.Status).Should().BeTrue();
    }

    [Test]
    public async Task Assets_HaveIndependentFileNodesHandlesAndPayloadsAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, first) = await fixture.CreateAssetAsync("First").ConfigureAwait(false);
        var (_, second) = await fixture.CreateAssetAsync("Second").ConfigureAwait(false);
        first.NodeId.Should().NotBe(second.NodeId);
        first.Open.NodeId.Should().NotBe(second.Open.NodeId);
        first.Size.NodeId.Should().NotBe(second.Size.NodeId);
        first.Open.InputArguments.NodeId.Should().NotBe(second.Open.InputArguments.NodeId);
        uint handle = await fixture.OpenAsync(first).ConfigureAwait(false);
        var write = await fixture.CallAsync(first, first.Write, new Variant(handle),
            new Variant((ByteString)new byte[] { 1, 2, 3 })).ConfigureAwait(false);
        ServiceResult.IsGood(write.Status).Should().BeTrue();
        first.Size.Value.Should().Be(3);
        second.Size.Value.Should().Be(0);
        second.OpenCount.Value.Should().Be(0);
        await fixture.CallAsync(first, first.Close, new Variant(handle)).ConfigureAwait(false);
    }

    [Test]
    public async Task Reupload_ReplacesPropertiesActionsAndEndpointThenDeleteRemovesThemAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (asset, file) = await fixture.CreateAssetAsync("Replace").ConfigureAwait(false);
        ServiceResult first = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(
            """{"title":"First","base":"opc.tcp://old","properties":{"old":{"type":"number"}},"actions":{"oldAction":{}}}"""))
            .ConfigureAwait(false);
        ServiceResult.IsGood(first).Should().BeTrue("{0}", first);
        NodeId oldProperty = fixture.Child<BaseDataVariableState>(asset, "old").NodeId;
        NodeId oldAction = fixture.Child<MethodState>(asset, "oldAction").NodeId;
        NodeId oldEndpoint = fixture.Child<PropertyState<string>>(asset, "AssetEndpoint").NodeId;
        ServiceResult second = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(
            """{"title":"Second","properties":{"new":{"type":"boolean"}}}""")).ConfigureAwait(false);
        ServiceResult.IsGood(second).Should().BeTrue("{0}", second);
        fixture.Find<NodeState>(oldProperty).Should().BeNull();
        fixture.Find<NodeState>(oldAction).Should().BeNull();
        fixture.Find<NodeState>(oldEndpoint).Should().BeNull();
        BaseDataVariableState newProperty = fixture.Child<BaseDataVariableState>(asset, "new");
        newProperty.Value.GetBoolean().Should().BeTrue();
        fixture.Tick();
        newProperty.Value.GetBoolean().Should().BeFalse();
        var removed = await fixture.CallAsync(fixture.Management, fixture.Find<MethodState>(fixture.Id(35)),
            new Variant(asset.NodeId)).ConfigureAwait(false);
        ServiceResult.IsGood(removed.Status).Should().BeTrue();
        fixture.Find<NodeState>(newProperty.NodeId).Should().BeNull();
        fixture.Tick();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Dispatch_RemapsManagementTypeMethodToRegisteredInstanceAsync(bool typeMethod)
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var request = new CallMethodRequest
        {
            ObjectId = fixture.Management.NodeId,
            MethodId = fixture.Id(typeMethod ? 26u : 32u),
            InputArguments = [new Variant("Remapped")]
        };
        using var context = new OperationContext(new RequestHeader(), null, RequestType.Call, default,
            new UserIdentity(new AnonymousIdentityTokenHandler(new AnonymousIdentityToken())));
        var results = new List<CallMethodResult> { new() };
        var errors = new List<ServiceResult> { ServiceResult.Good };

        await fixture.Manager.CallAsync(context, [request], results, errors, CancellationToken.None)
            .ConfigureAwait(false);

        request.MethodId.Should().Be(fixture.Id(32));
        ServiceResult.IsGood(errors[0]).Should().BeTrue("{0}", errors[0]);
        results[0].OutputArguments.Count.Should().Be(1);
        fixture.Find<BaseObjectState>(results[0].OutputArguments[0].GetNodeId()).Should().NotBeNull();
    }

    [Test]
    public async Task DispatchAsync_RemapsPerAssetFileMethodsAndHonorsCancellationAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (_, file) = await fixture.CreateAssetAsync("AsyncFile").ConfigureAwait(false);
        var request = new CallMethodRequest
        {
            ObjectId = file.NodeId,
            MethodId = new NodeId(Methods.FileType_Open),
            InputArguments = [new Variant((byte)2)]
        };
        using var context = new OperationContext(new RequestHeader(), null, RequestType.Call, default,
            new UserIdentity(new AnonymousIdentityTokenHandler(new AnonymousIdentityToken())));
        var results = new List<CallMethodResult> { new() };
        var errors = new List<ServiceResult> { ServiceResult.Good };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> cancelledCall = async () => await fixture.Manager.CallAsync(
            context, [request], results, errors, cancellation.Token).ConfigureAwait(false);

        await cancelledCall.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        request.MethodId.Should().Be(new NodeId(Methods.FileType_Open));
        file.OpenCount.Value.Should().Be(0);
        await fixture.Manager.CallAsync(context, [request], results, errors, CancellationToken.None)
            .ConfigureAwait(false);
        request.MethodId.Should().Be(file.Open.NodeId);
        ServiceResult.IsGood(errors[0]).Should().BeTrue("{0}", errors[0]);
        results[0].StatusCode.Should().Be((StatusCode)StatusCodes.Good);
        file.OpenCount.Value.Should().Be(1);
        uint handle = results[0].OutputArguments[0].GetUInt32();
        await fixture.CallAsync(file, file.Close, new Variant(handle)).ConfigureAwait(false);
    }

    [Test]
    public async Task Upload_ActionArrayOutputsAndPropertyTimestampsKeepTypesAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (asset, file) = await fixture.CreateAssetAsync("Types").ConfigureAwait(false);
        const string description = """
            {"title":"Types","properties":{
              "clock":{"type":"string","format":"date-time"},
              "labels":{"type":"array","items":{"type":"string"}},
              "flags":{"type":"array","items":{"type":"boolean"}}},
             "actions":{"sample":{"output":{"type":"object","properties":{
               "levels":{"type":"array","items":{"type":"number"}},
               "flags":{"type":"array","items":{"type":"boolean"}},
               "label":{"type":"string"}}}}}}
            """;
        ServiceResult uploaded = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(description)).ConfigureAwait(false);
        ServiceResult.IsGood(uploaded).Should().BeTrue("{0}", uploaded);
        fixture.Tick();
        BaseDataVariableState clock = fixture.Child<BaseDataVariableState>(asset, "clock");
        clock.Value.GetDateTime().Should().Be((DateTimeUtc)fixture.Now);
        fixture.Child<BaseDataVariableState>(asset, "labels").Value.GetStringArray().ToArray()
            .Should().Equal("running");
        fixture.Child<BaseDataVariableState>(asset, "flags").Value.GetBooleanArray().ToArray()
            .Should().Equal(false, true);
        var result = await fixture.CallAsync(asset, fixture.Child<MethodState>(asset, "sample")).ConfigureAwait(false);
        ServiceResult.IsGood(result.Status).Should().BeTrue();
        result.Outputs.Count.Should().Be(3);
        result.Outputs[0].GetDoubleArray().ToArray().Should().Equal(42.0, 42.998, 43.987);
        result.Outputs[1].GetBooleanArray().ToArray().Should().Equal(true, false);
        result.Outputs[2].GetString().Should().Be("idle");
        var context = ServiceMessageContext.CreateEmpty(null);
        using var encoder = new BinaryEncoder(context);
        encoder.WriteVariantArray("Outputs", result.Outputs.ToArrayOf());
        using var decoder = new BinaryDecoder(encoder.CloseAndReturnBuffer(), context);
        decoder.ReadVariantArray("Outputs").ToArray().Should().Equal(result.Outputs);
    }

    [Test]
    public async Task AddressSpace_PreCanceled_DoesNotRegisterOrStartSimulationAsync()
    {
        var fixture = new WotFixture();
        await using var cleanup = fixture.ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var references = new Dictionary<NodeId, IList<IReference>>();
        Func<Task> create = () => fixture.Manager.CreateAddressSpaceAsync(references, cancellation.Token).AsTask();

        var failure = await create.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);

        failure.Which.CancellationToken.Should().Be(cancellation.Token);
        references.Should().BeEmpty();
        fixture.Management.Should().BeNull();
        fixture.Timer.Verify(timer => timer.Dispose(), Times.Never);
    }

    [Test]
    public async Task CreateAsset_AwaitsRegistrationAndCancelsQueuedMutationAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Manager.BeforeRegistration = node =>
        {
            if (node is BaseObjectState && node.NodeId.IdType == IdType.Guid)
            {
                entered.TrySetResult();
                return release.Task;
            }
            return Task.CompletedTask;
        };
        Task<(BaseObjectState Asset, FileState File)> creation = fixture.CreateAssetAsync("Awaited");
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            creation.IsCompleted.Should().BeFalse();
            using var cancellation = new CancellationTokenSource();
            MethodState create = fixture.Find<MethodState>(fixture.Id(32));
            var outputs = new List<Variant> { Variant.Null };
            Task<ServiceResult> queued = create.OnCallMethod2Async(fixture.Manager.SystemContext,
                create, fixture.Management.NodeId, [new Variant("Canceled")], outputs, cancellation.Token).AsTask();
            queued.IsCompleted.Should().BeFalse();
            cancellation.Cancel();
            Func<Task> wait = () => queued.WaitAsync(TimeSpan.FromSeconds(5));
            await wait.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        }
        finally
        {
            release.TrySetResult();
            await creation.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            fixture.Manager.BeforeRegistration = null;
        }

        var duplicate = await fixture.CallAsync(fixture.Management, fixture.Find<MethodState>(fixture.Id(32)),
            new Variant("Awaited")).ConfigureAwait(false);
        duplicate.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadBrowseNameDuplicated);
        var canceledName = await fixture.CreateAssetAsync("Canceled").ConfigureAwait(false);
        canceledName.Asset.Should().NotBeNull();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAddressSpace_WaitsForMutationAndClosesOpenFilesAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (existing, file) = await fixture.CreateAssetAsync("Existing").ConfigureAwait(false);
        await fixture.OpenAsync(file).ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> canceledDelete = () => fixture.Manager.DeleteAddressSpaceAsync(cancellation.Token).AsTask();
        await canceledDelete.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        fixture.Find<NodeState>(existing.NodeId).Should().NotBeNull();
        file.OpenCount.Value.Should().Be(1);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Manager.BeforeRegistration = node =>
        {
            entered.TrySetResult();
            return release.Task;
        };
        var creation = fixture.CallAsync(fixture.Management, fixture.Find<MethodState>(fixture.Id(32)),
            new Variant("InFlight"));
        Task deletion = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            deletion = fixture.Manager.DeleteAddressSpaceAsync().AsTask();
            deletion.IsCompleted.Should().BeFalse();
            fixture.Find<NodeState>(existing.NodeId).Should().NotBeNull();
        }
        finally
        {
            release.TrySetResult();
            await creation.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await (deletion ?? fixture.Manager.DeleteAddressSpaceAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            fixture.Manager.BeforeRegistration = null;
        }

        fixture.Find<NodeState>(existing.NodeId).Should().BeNull();
        var created = await creation.ConfigureAwait(false);
        ServiceResult.IsGood(created.Status).Should().BeTrue();
        fixture.Find<NodeState>(created.Outputs[0].GetNodeId()).Should().BeNull();
        file.OpenCount.Value.Should().Be(0);
        var staleOpen = await fixture.CallAsync(file, file.Open, new Variant((byte)2)).ConfigureAwait(false);
        staleOpen.Status.StatusCode.Should().Be((StatusCode)StatusCodes.BadObjectDeleted);
        fixture.Tick();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAddressSpace_DrainsSimulationBeforeDeletingNodesAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (asset, file) = await fixture.CreateAssetAsync("Drain").ConfigureAwait(false);
        ServiceResult uploaded = await fixture.UploadAsync(file, Encoding.UTF8.GetBytes(
            """{"title":"Drain","properties":{"level":{"type":"number"},"next":{"type":"number"}}}"""))
            .ConfigureAwait(false);
        ServiceResult.IsGood(uploaded).Should().BeTrue();
        var variable = fixture.Child<BaseDataVariableState>(asset, "level");
        var next = fixture.Child<BaseDataVariableState>(asset, "next");
        Variant original = next.Value;
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        variable.StateChanged += (context, node, changes) =>
        {
            if ((changes & NodeStateChangeMasks.Value) != 0)
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Simulation callback was not released.");
                }
            }
        };
        Task tick = Task.Run(fixture.Tick);
        Task deletion = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            deletion = fixture.Manager.DeleteAddressSpaceAsync().AsTask();
            deletion.Wait(TimeSpan.FromMilliseconds(200)).Should().BeFalse();
            fixture.Find<NodeState>(asset.NodeId).Should().NotBeNull();
        }
        finally
        {
            release.Set();
            await tick.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await (deletion ?? fixture.Manager.DeleteAddressSpaceAsync().AsTask())
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        fixture.Tick();
        next.Value.Should().Be(original);
        fixture.Find<NodeState>(asset.NodeId).Should().BeNull();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsset_WaitsForUploadAndRemovesNewGenerationAsync()
    {
        var fixture = await WotFixture.CreateAsync().ConfigureAwait(false);
        await using var cleanup = fixture.ConfigureAwait(false);
        var (asset, file) = await fixture.CreateAssetAsync("Serialized").ConfigureAwait(false);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NodeId materializedId = NodeId.Null;
        fixture.Manager.BeforeRegistration = node =>
        {
            if (node.BrowseName.Name == "level")
            {
                materializedId = node.NodeId;
                entered.TrySetResult();
                return release.Task;
            }
            return Task.CompletedTask;
        };
        Task<ServiceResult> upload = fixture.UploadAsync(file, Encoding.UTF8.GetBytes(
            """{"title":"Serialized","properties":{"level":{"type":"number"}}}"""));
        Task<(ServiceResult Status, List<Variant> Outputs)> deletion = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            deletion = fixture.CallAsync(fixture.Management, fixture.Find<MethodState>(fixture.Id(35)),
                new Variant(asset.NodeId));
            deletion.IsCompleted.Should().BeFalse();
            fixture.Find<NodeState>(asset.NodeId).Should().NotBeNull();
        }
        finally
        {
            release.TrySetResult();
            await upload.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (deletion is not null)
            {
                await deletion.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            fixture.Manager.BeforeRegistration = null;
        }

        ServiceResult.IsGood(await upload.ConfigureAwait(false)).Should().BeTrue();
        ServiceResult.IsGood((await deletion.ConfigureAwait(false)).Status).Should().BeTrue();
        materializedId.IsNull.Should().BeFalse();
        fixture.Find<NodeState>(materializedId).Should().BeNull();
        fixture.Find<NodeState>(asset.NodeId).Should().BeNull();
        fixture.Find<NodeState>(file.NodeId).Should().BeNull();
        fixture.Tick();
        fixture.Logger.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task NativeManager_WireCreateRoutesToNativeOwnerAsync()
    {
        var fixture = new PlcSimulatorFixture(["--wotcon", "--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var session = await fixture.CreateSessionAsync("NativeWoT").ConfigureAwait(false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var master = fixture.Server.CurrentInstance.NodeManager;
            var manager = master.AsyncNodeManagers.OfType<WotConNodeManager>().Should().ContainSingle().Subject;
            manager.Should().BeAssignableTo<AsyncCustomNodeManager>();
            var managementId = new NodeId(31, manager.NamespaceIndex);
            var owner = await master.GetManagerHandleAsync(managementId, deadline.Token).ConfigureAwait(false);
            owner.nodeManager.Should().BeSameAs(manager);
            var response = await session.CallAsync(null,
            [
                new CallMethodRequest
                {
                    ObjectId = managementId, MethodId = new NodeId(26, manager.NamespaceIndex),
                    InputArguments = [new Variant("NativeWire")]
                }
            ], deadline.Token).ConfigureAwait(false);
            response.Results[0].StatusCode.Should().Be((StatusCode)StatusCodes.Good);
            NodeId assetId = response.Results[0].OutputArguments[0].GetNodeId();
            var assetOwner = await master.GetManagerHandleAsync(assetId, deadline.Token).ConfigureAwait(false);
            assetOwner.nodeManager.Should().BeSameAs(manager);
            assetOwner.handle.Should().NotBeNull();
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }

    private sealed class TestWotManager(IServerInternal server, ApplicationConfiguration configuration,
        TimeService timeService, ILogger logger) : WotConNodeManager(server, configuration, timeService, logger)
    {
        public Func<NodeState, Task> BeforeRegistration { get; set; }

        protected override async ValueTask AddPredefinedNodeAsync(
            ISystemContext context, NodeState node, CancellationToken cancellationToken = default)
        {
            if (BeforeRegistration is not null)
            {
                await BeforeRegistration(node).ConfigureAwait(false);
            }
            await base.AddPredefinedNodeAsync(context, node, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class WotFixture : IAsyncDisposable
    {
        private ElapsedEventHandler _tick;
        private readonly Mock<OpcPlc.ITimer> _timer = new();
        public TestWotManager Manager { get; }
        public Mock<OpcPlc.ITimer> Timer => _timer;
        public CaptureLogger Logger { get; } = new();
        public DateTime Now { get; } = new(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        public ushort NamespaceIndex => Manager.NamespaceIndex;
        public BaseObjectState Management => Find<BaseObjectState>(Id(31));

        public WotFixture()
        {
            ITelemetryContext telemetry = DefaultTelemetry.Create(_ => { });
            var namespaces = new NamespaceTable();
            namespaces.GetIndexOrAppend("urn:opcplc:test:server");
            var typeTable = new TypeTable(namespaces);
            typeTable.AddSubtype(ObjectTypeIds.BaseObjectType, NodeId.Null);
            typeTable.AddSubtype(ObjectTypeIds.BaseInterfaceType, ObjectTypeIds.BaseObjectType);
            typeTable.AddSubtype(ObjectTypeIds.FileType, ObjectTypeIds.BaseObjectType);
            typeTable.AddReferenceSubtype(ReferenceTypeIds.References, NodeId.Null,
                new QualifiedName(BrowseNames.References));
            typeTable.AddReferenceSubtype(ReferenceTypeIds.HierarchicalReferences, ReferenceTypeIds.References,
                new QualifiedName(BrowseNames.HierarchicalReferences));
            typeTable.AddReferenceSubtype(ReferenceTypeIds.HasChild, ReferenceTypeIds.HierarchicalReferences,
                new QualifiedName(BrowseNames.HasChild));
            typeTable.AddReferenceSubtype(ReferenceTypeIds.Aggregates, ReferenceTypeIds.HasChild,
                new QualifiedName(BrowseNames.Aggregates));
            typeTable.AddReferenceSubtype(ReferenceTypeIds.HasComponent, ReferenceTypeIds.Aggregates,
                new QualifiedName(BrowseNames.HasComponent));
            var factory = EncodeableFactory.Create();
            var server = new Mock<IServerInternal>();
            server.SetupGet(instance => instance.Telemetry).Returns(telemetry);
            server.SetupGet(instance => instance.NamespaceUris).Returns(namespaces);
            server.SetupGet(instance => instance.ServerUris).Returns(new StringTable());
            server.SetupGet(instance => instance.TypeTree).Returns(typeTable);
            server.SetupGet(instance => instance.Factory).Returns(factory);
            var master = new Mock<IMasterNodeManager>();
            server.SetupGet(instance => instance.NodeManager).Returns(master.Object);
            server.SetupGet(instance => instance.MessageContext).Returns(new ServiceMessageContext(telemetry, factory));
            server.SetupGet(instance => instance.DefaultSystemContext).Returns(new ServerSystemContext(server.Object));
            var timeService = new Mock<TimeService>();
            timeService.Setup(service => service.UtcNow()).Returns(Now);
            timeService.Setup(service => service.NewTimer(It.IsAny<ElapsedEventHandler>(), 1000u))
                .Callback<ElapsedEventHandler, uint>((callback, _) => _tick = callback).Returns(_timer.Object);
            Manager = new TestWotManager(server.Object,
                new ApplicationConfiguration { ServerConfiguration = new ServerConfiguration() }, timeService.Object, Logger);
            master.SetupGet(instance => instance.AsyncNodeManagers).Returns([Manager]);
        }

        public static async Task<WotFixture> CreateAsync()
        {
            var fixture = new WotFixture();
            try
            {
                await fixture.Manager.CreateAddressSpaceAsync(new Dictionary<NodeId, IList<IReference>>())
                    .ConfigureAwait(false);
                fixture.Logger.Errors.Should().BeEmpty();
                fixture.Management.Should().NotBeNull();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public NodeId Id(uint id) => new(id, NamespaceIndex);
        public T Find<T>(NodeId id) where T : NodeState => Manager.FindPredefinedNode<T>(id);
        public T Child<T>(NodeState parent, string name) where T : BaseInstanceState
        {
            var children = new List<BaseInstanceState>();
            parent.GetChildren(Manager.SystemContext, children);
            return children.OfType<T>().Single(child => child.BrowseName.Name == name);
        }

        public async Task<(ServiceResult Status, List<Variant> Outputs)> CallAsync(
            NodeState parent, MethodState method, params Variant[] inputs)
        {
            var errors = new List<ServiceResult>();
            var outputs = new List<Variant>();
            ServiceResult status = await method.CallAsync(Manager.SystemContext, parent.NodeId,
                inputs.ToArrayOf(), errors, outputs, CancellationToken.None).ConfigureAwait(false);
            foreach (ServiceResult error in errors)
            {
                ServiceResult.IsGood(error).Should().BeTrue("SDK argument validation: {0}", error);
            }
            return (status, outputs);
        }

        public async Task<(BaseObjectState Asset, FileState File)> CreateAssetAsync(string name)
        {
            var result = await CallAsync(Management, Find<MethodState>(Id(32)), new Variant(name)).ConfigureAwait(false);
            ServiceResult.IsGood(result.Status).Should().BeTrue("CreateAsset: {0}", result.Status);
            BaseObjectState asset = Find<BaseObjectState>(result.Outputs[0].GetNodeId());
            return (asset, Child<FileState>(asset, "WoTFile"));
        }

        public async Task<uint> OpenAsync(FileState file)
        {
            var result = await CallAsync(file, file.Open, new Variant((byte)2)).ConfigureAwait(false);
            ServiceResult.IsGood(result.Status).Should().BeTrue();
            return result.Outputs[0].GetUInt32();
        }

        public async Task<ServiceResult> UploadAsync(FileState file, byte[] payload)
        {
            uint handle = await OpenAsync(file).ConfigureAwait(false);
            var write = await CallAsync(file, file.Write, new Variant(handle), new Variant((ByteString)payload))
                .ConfigureAwait(false);
            ServiceResult.IsGood(write.Status).Should().BeTrue();
            var close = await CallAsync(file, Child<MethodState>(file, "CloseAndUpdate"), new Variant(handle))
                .ConfigureAwait(false);
            return close.Status;
        }

        public void Tick() => _tick.Invoke(null, null);
        public async ValueTask DisposeAsync()
        {
            try
            {
                await Manager.DeleteAddressSpaceAsync().ConfigureAwait(false);
            }
            finally
            {
                Manager.Dispose();
            }
        }
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<string> Errors { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Errors.Add(formatter(state, exception) + ": " + exception);
            }
        }
    }
}