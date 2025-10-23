namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using System.Linq;

/// <summary>
/// Tests for NodeSet2 XML loading.
/// </summary>
[TestFixture]
public class NodeSet2Tests : SimulatorTestsBase
{
    public NodeSet2Tests() : base(["--ns2=/home/runner/work/iot-edge-opc-plc/iot-edge-opc-plc/src/SimpleEvent/SimpleEvents.NodeSet2.xml"])
    {
    }

    [TestCase]
    public void NodeSet2_IsLoaded_WithNamespace()
    {
        // The SimpleEvents namespace should be loaded
        var namespaceUris = Session.NamespaceUris.ToArray();
        namespaceUris.Should().Contain("http://microsoft.com/Opc/OpcPlc/SimpleEvents", 
            "SimpleEvents namespace should be loaded from NodeSet2 file");
    }

    [TestCase]
    public void NodeSet2_HasNamespace_Registered()
    {
        // Verify that the namespace from the NodeSet2 file is registered
        var namespaceIndex = Session.NamespaceUris.GetIndex("http://microsoft.com/Opc/OpcPlc/SimpleEvents");
        
        namespaceIndex.Should().BeGreaterThan(0, 
            "SimpleEvents namespace should be registered with a valid index");
    }


}
