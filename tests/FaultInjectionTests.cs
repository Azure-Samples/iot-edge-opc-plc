namespace OpcPlc.Tests;

using global::AlarmCondition;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Server;
using OpcPlc.CompanionSpecs.DI;
using OpcPlc.CompanionSpecs.IA;
using OpcPlc.CompanionSpecs.Machinery;
using OpcPlc.CompanionSpecs.Pumps;
using OpcPlc.Configuration;
using OpcPlc.Helpers;
using SimpleEvents;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class FaultInjectionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task RequestValidation_WireRejectionDrainsAndResumesAsync(bool optionalModels)
    {
        var fixture = new PlcSimulatorFixture(optionalModels
            ? ["--str=false", "--sl", "--pumps", "--simpleevents", "--alm"]
            : ["--str=false"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            var managers = fixture.Server.CurrentInstance.NodeManager.AsyncNodeManagers;
            managers.Should().Contain(fixture.Server.PlcNodeManager);
            managers.Select(manager => manager.GetType()).Should().ContainInOrder(
                typeof(DiNodeManager), typeof(PlcNodeManager));
            if (optionalModels)
            {
                managers.Select(manager => manager.GetType()).Should().ContainInOrder(
                    typeof(SimpleEventsNodeManager), typeof(AlarmConditionServerNodeManager), typeof(IaNodeManager),
                    typeof(MachineryNodeManager), typeof(PumpNodeManager));
                managers.Should().Contain(fixture.Server.SimpleEventsNodeManager);
                managers.Should().Contain(fixture.Server.AlarmNodeManager);
            }
            else
            {
                managers.Should().NotContain(manager => manager is IaNodeManager ||
                    manager is MachineryNodeManager || manager is PumpNodeManager ||
                    manager is SimpleEventsNodeManager || manager is AlarmConditionServerNodeManager);
            }
            fixture.Server.PlcNodeManager.Should().BeAssignableTo<AsyncCustomNodeManager>();
            using var session = await fixture.CreateSessionAsync("FaultInjectionWire").ConfigureAwait(false);
            session.NamespaceUris.GetIndex(OpcPlc.Namespaces.DI).Should().Be(2);
            session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcApplications).Should().Be(3);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            (uint Identifier, string NamespaceUri, Type ManagerType)[] modelNodes =
            [
                (Opc.Ua.DI.Objects.DeviceSet, OpcPlc.Namespaces.DI, typeof(DiNodeManager)),
                (1010, OpcPlc.Namespaces.IA, typeof(IaNodeManager)),
                (1004, OpcPlc.Namespaces.Machinery, typeof(MachineryNodeManager)),
                (1052, OpcPlc.Namespaces.Pumps, typeof(PumpNodeManager))
            ];
            foreach (var model in modelNodes.Take(optionalModels ? modelNodes.Length : 1))
            {
                var modelNodeId = NodeId.Create(model.Identifier, model.NamespaceUri, session.NamespaceUris);
                var owner = await fixture.Server.CurrentInstance.NodeManager.GetManagerHandleAsync(
                    modelNodeId, deadline.Token).ConfigureAwait(false);
                owner.handle.Should().NotBeNull();
                owner.nodeManager.Should().BeOfType(model.ManagerType);
            }
            var lifecycle = fixture.Server.NodeManagerLifecycle;
            var registration = await lifecycle.AddAsync(new DrainTargetFactory(), null, deadline.Token)
                .ConfigureAwait(false);
            var node = new NodeId("FastUInt1", session.NamespaceUris.GetIndexOrAppend(OpcPlc.Namespaces.OpcPlcApplications));
            StatusCode.IsGood((await session.ReadValueAsync(node, deadline.Token).ConfigureAwait(false)).StatusCode)
                .Should().BeTrue();
            fixture.Server.InjectErrorResponseRate = 1;
            try
            {
                Func<Task> read = async () => await session.ReadValueAsync(node, deadline.Token).ConfigureAwait(false);
                var failure = await read.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
                StatusCode.IsBad(failure.Which.StatusCode).Should().BeTrue();
                await lifecycle.RemoveAsync(registration, null, deadline.Token).ConfigureAwait(false);
            }
            finally
            {
                fixture.Server.InjectErrorResponseRate = 0;
            }
            StatusCode.IsGood((await session.ReadValueAsync(node, deadline.Token).ConfigureAwait(false)).StatusCode)
                .Should().BeTrue();
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);
        }
        finally
        {
            fixture.Server.InjectErrorResponseRate = 0;
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }

    [Test]
    public async Task AlarmMethods_WireCallsResolveDynamicConditionsAsync()
    {
        var fixture = new PlcSimulatorFixture(["--str=false", "--alm"]);
        await fixture.StartAsync().ConfigureAwait(false);
        try
        {
            using var session = await fixture.CreateSessionAsync("NativeAlarmMethods").ConfigureAwait(false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var manager = fixture.Server.AlarmNodeManager;
            var backend = (UnderlyingSystem)manager.SystemContext.SystemHandle;
            backend.TryGetSource("SouthMotor", out var source).Should().BeTrue();
            source.Refresh();
            var sourceId = ModelUtils.ConstructIdForSource("Metals/SouthMotor", manager.NamespaceIndex);
            var alarmId = new NodeId(sourceId.IdentifierAsString + "?Bronze", manager.NamespaceIndex);
            var enabledId = new NodeId(alarmId.IdentifierAsString + "/EnabledState/Id", manager.NamespaceIndex);

            (await CallAsync(alarmId, "Disable", []).ConfigureAwait(false)).Should().Be(StatusCodes.Good);
            (await session.ReadValueAsync(enabledId, deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetBoolean().Should().BeFalse();
            (await CallAsync(alarmId, "Enable", []).ConfigureAwait(false)).Should().Be(StatusCodes.Good);
            (await session.ReadValueAsync(enabledId, deadline.Token).ConfigureAwait(false))
                .WrappedValue.GetBoolean().Should().BeTrue();

            foreach (string method in new[] { "AddComment", "Acknowledge", "Confirm" })
            {
                (await CallAsync(alarmId, method,
                    [Variant.From((ByteString)new byte[] { 1, 2 }), Variant.From(new LocalizedText("wire comment"))])
                    .ConfigureAwait(false)).Should().Be(StatusCodes.BadEventIdUnknown);
            }

            var dialogId = new NodeId(sourceId.IdentifierAsString + "?OnlineState", manager.NamespaceIndex);
            (await CallAsync(dialogId, "Respond", [Variant.From(1)]).ConfigureAwait(false))
                .Should().Be(StatusCodes.Good);
            source.IsOffline.Should().BeTrue();
            await session.CloseAsync(deadline.Token).ConfigureAwait(false);

            async Task<StatusCode> CallAsync(NodeId objectId, string method, ArrayOf<Variant> arguments)
            {
                var response = await session.CallAsync(null,
                [
                    new CallMethodRequest
                    {
                        ObjectId = objectId,
                        MethodId = new NodeId(objectId.IdentifierAsString + "/" + method, manager.NamespaceIndex),
                        InputArguments = arguments
                    }
                ], deadline.Token).ConfigureAwait(false);
                response.Results.Count.Should().Be(1);
                return response.Results[0].StatusCode;
            }
        }
        finally
        {
            await fixture.StopAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }

    private sealed class DrainTargetFactory : IAsyncNodeManagerFactory
    {
        public ArrayOf<string> NamespacesUris => ["urn:opcplc:request-drain-test"];

        public ValueTask<IAsyncNodeManager> CreateAsync(IServerInternal server,
            ApplicationConfiguration configuration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IAsyncNodeManager>(new DrainTarget(server, configuration));
        }
    }

    private sealed class DrainTarget(IServerInternal server, ApplicationConfiguration configuration)
        : AsyncCustomNodeManager(server, configuration, "urn:opcplc:request-drain-test");

    [Test]
    public async Task RequestValidation_Disabled_DoesNotReject()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var telemetry = new OpcTelemetryContext(loggerFactory, "OpcPlc", "test");
        using var server = new FaultInjectionServer(loggerFactory.CreateLogger<PlcServer>(), telemetry);

        Func<Task> validate = async () => await server.ValidateAsync().ConfigureAwait(false);

        await validate.Should().NotThrowAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task RequestValidation_RateOne_RejectsEveryRequest()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var telemetry = new OpcTelemetryContext(loggerFactory, "OpcPlc", "test");
        using var server = new FaultInjectionServer(loggerFactory.CreateLogger<PlcServer>(), telemetry)
        {
            InjectErrorResponseRate = 1
        };

        for (int requestIndex = 0; requestIndex < 32; requestIndex++)
        {
            Func<Task> validate = async () => await server.ValidateAsync().ConfigureAwait(false);
            var rejection = await validate.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);
            StatusCode.IsBad(rejection.Which.StatusCode).Should().BeTrue();
        }
    }

    [Test]
    public async Task RequestValidation_DisablingInjection_ResumesAdmission()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var telemetry = new OpcTelemetryContext(loggerFactory, "OpcPlc", "test");
        using var server = new FaultInjectionServer(loggerFactory.CreateLogger<PlcServer>(), telemetry)
        {
            InjectErrorResponseRate = 1
        };

        Func<Task> validate = async () => await server.ValidateAsync().ConfigureAwait(false);
        await validate.Should().ThrowAsync<ServiceResultException>().ConfigureAwait(false);

        server.InjectErrorResponseRate = 0;

        await validate.Should().NotThrowAsync().ConfigureAwait(false);
    }

    private sealed class FaultInjectionServer(ILogger logger, ITelemetryContext telemetry)
        : PlcServer(new OpcPlcConfiguration(), new PlcSimulation([]), new TimeService(), [], logger, telemetry)
    {
        public ValueTask ValidateAsync() => OnRequestValidatedAsync(null);
    }
}
