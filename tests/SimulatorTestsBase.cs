namespace OpcPlc.Tests;

using Bogus;
using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstract base class for simulator integration tests.
/// </summary>
[TestFixture]
public abstract class SimulatorTestsBase
{
    /// <summary>The identifier for the Server Object.</summary>
    protected static readonly NodeId Server = ObjectIds.Server;

    /// <summary>The identifier for the ObjectsFolder Object.</summary>
    protected static readonly NodeId ObjectsFolder = ObjectIds.ObjectsFolder;

    /// <summary>A Bogus data generator.</summary>
    protected static readonly Faker Fake = new();

    private readonly PlcSimulatorFixture _simulator;

    protected SimulatorTestsBase(string[] args = default)
    {
        _simulator = new PlcSimulatorFixture(args);
    }

    /// <summary>The current OPC-UA Session.</summary>
    protected Session Session { get; private set; }

    /// <summary>Starts the simulator and creates a new OPC-UA session, shared by all test methods in a class.</summary>
    [OneTimeSetUp]
    public async Task Setup()
    {
        await _simulator.StartAsync().ConfigureAwait(false);
        Session = await _simulator.CreateSessionAsync(GetType().Name).ConfigureAwait(false);
    }

    /// <summary>Closes the OPC-UA session and stops the simulator.</summary>
    [OneTimeTearDown]
    public async Task TearDown()
    {
        await Session.CloseAsync().ConfigureAwait(false);
        await _simulator.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Get a <see cref="NodeId"/> from an identifier.
    /// </summary>
    protected NodeId GetOpcPlcNodeId(string identifier)
        => NodeId.Create(identifier, OpcPlc.Namespaces.OpcPlcApplications, Session.NamespaceUris);

    /// <summary>
    /// Find a node given a starting node and the OPC-UA Browse Names of one or more nodes.
    /// </summary>
    /// <param name="startingNode">The node identifier at which to start the search.</param>
    /// <param name="namespaceUri">The namespace URI of all the path parts.</param>
    /// <param name="pathParts">The browse names of the path parts up to the node to retrieve.</param>
    /// <returns>The node reached by traversing the path parts from the starting node.</returns>
    protected Task<NodeId> FindNodeAsync(NodeId startingNode, string namespaceUri, params string[] pathParts)
        => FindNodeAsync(
            startingNode,
            string.Join(
                '/',
                pathParts.Select(s => $"{Session.NamespaceUris.GetIndex(namespaceUri)}:{s}")));

    /// <summary>
    /// Transform an <see cref="ExpandedNodeId"/> into a <see cref="NodeId"/>.
    /// </summary>
    /// <param name="nodeId">Node identifier to transform.</param>
    /// <returns>The transformed node identifier.</returns>
    protected NodeId ToNodeId(ExpandedNodeId nodeId)
    {
        var e = ExpandedNodeId.ToNodeId(nodeId, Session.NamespaceUris);
        e.Should().NotBeNull();
        return e;
    }

    /// <summary>
    /// Cause a subset of the mocked timers to fire a number of times,
    /// and the current mocked time to advance accordingly.
    /// </summary>
    /// <param name="periodInMilliseconds">Defines the timers to fire: only timers with this interval are fired.</param>
    /// <param name="numberOfTimes">Number of times the timer should be fired.</param>
    protected void FireTimersWithPeriod(TimeSpan periodInMilliseconds, int numberOfTimes)
        => _simulator.FireTimersWithPeriod((uint)periodInMilliseconds.TotalMilliseconds, numberOfTimes);

    private async Task<NodeId> FindNodeAsync(NodeId startingNode, string relativePath)
    {
        var browsePaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = startingNode,
                    RelativePath = RelativePath.Parse(relativePath, Session.TypeTree)
                }
            };

        var results = await Session.TranslateBrowsePathsToNodeIdsAsync(
            requestHeader: null,
            browsePaths,
            CancellationToken.None).ConfigureAwait(false);

        var nodeId = results.Results
            .Should().ContainSingle("search should contain a result")
            .Subject.Targets
            .Should().ContainSingle("search for {0} should contain a result target (Results: {1})", relativePath, JsonSerializer.Serialize(results))
            .Subject.TargetId;

        return ToNodeId(nodeId);
    }

    protected async Task<T> ReadValueAsync<T>(NodeId nodeId)
    {
        return (T)(await Session.ReadValueAsync(nodeId).ConfigureAwait(false)).Value;
    }

    protected async Task<StatusCode> WriteValueAsync(NodeId nodeId, object newValue)
    {
        var valuesToWrite = new WriteValueCollection
            {
                new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value =
                    {
                        Value = newValue,
                    },
                }
            };

        // write value.
        var results = await Session.WriteAsync(
            default,
            valuesToWrite,
            CancellationToken.None).ConfigureAwait(false);

        return results.Results.FirstOrDefault();
    }

    /// <summary>
    /// Calls OPC UA method over active session
    /// </summary>
    protected Task<IList<object>> CallMethodAsync(string methodName, string objectName = "Methods", params object[] args)
    {
        return Session.CallAsync(GetOpcPlcNodeId(objectName), GetOpcPlcNodeId(methodName), args: args);
    }
}
