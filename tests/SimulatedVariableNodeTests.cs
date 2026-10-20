// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Tests;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Timers;

[TestFixture]
public class SimulatedVariableNodeTests
{
    private static IEnumerable<TestCaseData> ValueCases()
    {
        yield return new TestCaseData(false, true, new Variant(false), new Variant(true));
        yield return new TestCaseData(-1, int.MinValue, new Variant(-1), new Variant(int.MinValue));
        yield return new TestCaseData(1u, uint.MaxValue, new Variant(1u), new Variant(uint.MaxValue));
        yield return new TestCaseData(1.25, -3.5, new Variant(1.25), new Variant(-3.5));
        yield return new TestCaseData("initial", "updated", new Variant("initial"), new Variant("updated"));

        ByteString initialBytes = (ByteString)new byte[] { 1, 2 };
        ByteString updatedBytes = (ByteString)new byte[] { 3, 4, 5 };
        yield return new TestCaseData(initialBytes, updatedBytes, new Variant(initialBytes), new Variant(updatedBytes));

        ArrayOf<byte> initialArray = new byte[] { 1, 2 }.ToArrayOf();
        ArrayOf<byte> updatedArray = new byte[] { 3, 4, 5 }.ToArrayOf();
        yield return new TestCaseData(initialArray, updatedArray, new Variant(initialArray), new Variant(updatedArray));
        yield return new TestCaseData(initialBytes, ByteString.Empty, new Variant(initialBytes), new Variant(ByteString.Empty));
        ArrayOf<byte> emptyArray = Array.Empty<byte>().ToArrayOf();
        yield return new TestCaseData(initialArray, emptyArray, new Variant(initialArray), new Variant(emptyArray));
    }

    [TestCaseSource(nameof(ValueCases))]
    public void Value_RoundTripsWithoutChangingType<T>(T initial, T updated, Variant initialValue, Variant expectedValue)
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = initialValue };
        using var node = new SimulatedVariableNode<T>(context, variable, new TimeService());

        node.Value.Should().Be(initial);

        node.Value = updated;

        node.Value.Should().Be(updated);
        variable.Value.Should().Be(expectedValue);
        variable.Value.TypeInfo.Should().Be(expectedValue.TypeInfo);
    }

    [Test]
    public void Value_UpdatesTimestampAndNotifiesValueChange()
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = new Variant(1u) };
        variable.ClearChangeMasks(context, false);
        NodeStateChangeMasks observedChanges = NodeStateChangeMasks.None;
        variable.OnStateChanged = (_, _, changes) => observedChanges |= changes;
        var now = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        var timeService = new Mock<TimeService>();
        timeService.Setup(service => service.Now()).Returns(now);
        using var node = new SimulatedVariableNode<uint>(context, variable, timeService.Object);

        node.Value = 2u;

        variable.Timestamp.Should().Be((DateTimeUtc)now);
        observedChanges.Should().HaveFlag(NodeStateChangeMasks.Value);
        variable.ChangeMasks.Should().Be(NodeStateChangeMasks.None);
    }

    [Test]
    public void Start_UsesCurrentValueAndConfiguredPeriod()
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = new Variant(5u) };
        ElapsedEventHandler callback = null;
        var timer = new Mock<ITimer>();
        var timeService = new Mock<TimeService>();
        timeService.Setup(service => service.NewTimer(It.IsAny<ElapsedEventHandler>(), 1000u))
            .Callback<ElapsedEventHandler, uint>((handler, _) => callback = handler)
            .Returns(timer.Object);
        using var node = new SimulatedVariableNode<uint>(context, variable, timeService.Object);
        node.Start(value => value + 1, 1000);

        variable.Value = new Variant(20u);
        callback.Should().NotBeNull();
        callback.Invoke(null, null);

        variable.Value.GetUInt32().Should().Be(21u);
        timeService.Verify(service => service.NewTimer(It.IsAny<ElapsedEventHandler>(), 1000u), Times.Once);

        node.Stop();
        timer.VerifySet(instance => instance.Enabled = false, Times.Once);
    }

    [Test]
    public void Value_RejectsMismatchedType()
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = new Variant("not a uint") };
        using var node = new SimulatedVariableNode<uint>(context, variable, new TimeService());

        Action read = () => _ = node.Value;

        read.Should().Throw<ServiceResultException>();
    }

    [Test]
    public void Value_RejectsByteStringAsByteArray()
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = new Variant((ByteString)new byte[] { 1, 2 }) };
        using var node = new SimulatedVariableNode<ArrayOf<byte>>(context, variable, new TimeService());

        Action read = () => _ = node.Value;

        read.Should().Throw<ServiceResultException>()
            .Where(exception => exception.StatusCode == StatusCodes.BadTypeMismatch);
    }

    [Test]
    public void Value_PreservesNullString()
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null) { Value = Variant.Null };
        using var node = new SimulatedVariableNode<string>(context, variable, new TimeService());

        node.Value.Should().BeNull();
        node.Value = "value";
        node.Value = null;

        node.Value.Should().BeNull();
        variable.Value.IsNull.Should().BeTrue();
    }

    [TestCase(100 * 1024, true)]
    [TestCase(200 * 1024, false)]
    public void BinaryValue_EncodingPreservesShapeAndPayload(int length, bool scalar)
    {
        var context = new SystemContext(null);
        var variable = new BaseDataVariableState(null);
        byte[] payload = new byte[length];
        Array.Fill(payload, (byte)'A');
        if (scalar)
        {
            using var node = new SimulatedVariableNode<ByteString>(context, variable, new TimeService());
            node.Value = (ByteString)payload;
        }
        else
        {
            using var node = new SimulatedVariableNode<ArrayOf<byte>>(context, variable, new TimeService());
            node.Value = payload.ToArrayOf();
        }

        var messageContext = ServiceMessageContext.CreateEmpty(null);
        messageContext.MaxArrayLength = 200 * 1024;
        using var encoder = new BinaryEncoder(messageContext);
        encoder.WriteVariant("Value", variable.Value);
        byte[] encoded = encoder.CloseAndReturnBuffer();
        using var decoder = new BinaryDecoder(encoded, messageContext);
        Variant decoded = decoder.ReadVariant("Value");

        decoded.TypeInfo.BuiltInType.Should().Be(scalar ? BuiltInType.ByteString : BuiltInType.Byte);
        decoded.TypeInfo.ValueRank.Should().Be(scalar ? ValueRanks.Scalar : ValueRanks.OneDimension);
        byte[] decodedPayload = scalar ? decoded.GetByteString().ToArray() : decoded.GetByteArray().ToArray();
        decodedPayload.Should().Equal(payload);
    }
}