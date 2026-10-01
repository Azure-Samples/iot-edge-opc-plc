namespace UnitTests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class OpcUaUnitTests : OpcPlcBase
{
    public OpcUaUnitTests() : base(["--gn=2", "--fn=1", "--str=false"])
    {
    }

    [Test]
    public void Package_CopiesRuntimeAssets()
    {
        string[] assets = [
            "Boilers/Boiler1/BoilerModel1.NodeSet2.xml", "Boilers/Boiler2/BoilerModel2.NodeSet2.xml",
            "CompanionSpecs/DI/Opc.Ua.DI.NodeSet2.xml", "CompanionSpecs/IA/Opc.Ua.IA.NodeSet2.xml",
            "CompanionSpecs/Machinery/Opc.Ua.Machinery.NodeSet2.xml", "CompanionSpecs/Pumps/Opc.Ua.Pumps.NodeSet2.xml",
            "CompanionSpecs/WotCon/Opc.Ua.WotCon.NodeSet2.xml", "SimpleEvent/SimpleEvents.NodeSet2.xml",
            "wwwroot/stacklight.html", "nodesfile.json"];
        foreach (string asset in assets)
        {
            var file = new FileInfo(Path.Combine(AppContext.BaseDirectory, asset));
            file.Exists.Should().BeTrue($"the PLC package must supply {asset}");
            file.Length.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public async Task PackagedServer_SecureReadWriteAndRuntimeTypesAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var session = await GetConnectedClientAsync(deadline.Token).ConfigureAwait(false);
        await using var cleanup = session.ConfigureAwait(false);
        session.Connected.Should().BeTrue();
        session.Endpoint.SecurityMode.Should().Be(MessageSecurityMode.SignAndEncrypt);

        var methods = NodeId.Create("Methods", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
        var stop = NodeId.Create("StopUpdateFastNodes", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
        await session.CallAsync(methods, stop, deadline.Token).ConfigureAwait(false);
        var fastNode = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
        var written = await session.WriteAsync(null,
            [new WriteValue { NodeId = fastNode, AttributeId = Attributes.Value, Value = new DataValue(42_000u) }],
            deadline.Token).ConfigureAwait(false);
        written.Results[0].Should().Be(StatusCodes.Good);
        (await session.ReadValueAsync(fastNode, deadline.Token).ConfigureAwait(false)).WrappedValue.GetUInt32()
            .Should().Be(42_000u);

        using var typeSystem = new DefaultComplexTypeSystemFactory(session.MessageContext.Telemetry).Create(session);
        (await typeSystem.LoadNamespaceAsync(OpcPlc.Namespaces.OpcPlcBoiler, true, deadline.Token)
            .ConfigureAwait(false)).Should().BeTrue();
        var boilerId = NodeId.Create(15013u, OpcPlc.Namespaces.OpcPlcBoiler, session.NamespaceUris);
        var boiler = await session.ReadValueAsync(boilerId, deadline.Token).ConfigureAwait(false);
        boiler.WrappedValue.TryGetStructure<IEncodeable>(out IEncodeable? structure).Should().BeTrue();
        structure.Should().BeAssignableTo<IStructure>();
        await session.CloseAsync(deadline.Token).ConfigureAwait(false);
    }

    [Test]
    public async Task PackagedServer_RestartsAndAcceptsNewSessionAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await RestartOpcPlcServerAsync().WaitAsync(deadline.Token).ConfigureAwait(false);
        var session = await GetConnectedClientAsync(deadline.Token).ConfigureAwait(false);
        await using var cleanup = session.ConfigureAwait(false);
        var fastNode = NodeId.Create("FastUInt1", OpcPlc.Namespaces.OpcPlcApplications, session.NamespaceUris);
        (await session.ReadValueAsync(fastNode, deadline.Token).ConfigureAwait(false)).StatusCode
            .Should().Be(StatusCodes.Good);
        await session.CloseAsync(deadline.Token).ConfigureAwait(false);
    }
}
