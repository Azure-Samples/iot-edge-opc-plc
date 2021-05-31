using System;

namespace OpcPlc.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// Abstract base class for simulator integration tests.
    /// </summary>
    [TestFixture]
    public abstract class SimulatorTestsBase
    {
        /// <summary>The identifier for the Server Object.</summary>
        protected static readonly NodeId Server = Opc.Ua.ObjectIds.Server;

        /// <summary>The identifier for the ObjectsFolder Object.</summary>
        protected static readonly NodeId ObjectsFolder = Opc.Ua.ObjectIds.ObjectsFolder;

        /// <summary>A Bogus data generator.</summary>
        protected static readonly Faker Fake = new Faker();

        private PlcSimulatorFixture _simulator;

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
            await _simulator.Start();
            Session = await _simulator.CreateSessionAsync(GetType().Name);
        }

        /// <summary>Closes the OPC-UA session and stops the simulator.</summary>
        [OneTimeTearDown]
        public async Task TearDown()
        {
            Session.Close();
            await _simulator.Stop();
        }

        /// <summary>
        /// Retrieve a node from the OPC-PLC namespace given its identifier.
        /// </summary>
        /// <param name="identifier">Node string identifier to retrieve.</param>
        /// <returns>The node identifier.</returns>
        protected NodeId GetOpcPlcNodeId(string identifier)
            => NodeId.Create(identifier, OpcPlc.Namespaces.OpcPlcApplications, Session.NamespaceUris);

        /// <summary>
        /// Find a node given a starting node and the OPC-UA Browse Names of one or more nodes.
        /// </summary>
        /// <param name="startingNode">The node identifier at which to start the search.</param>
        /// <param name="namespaceUri">The namespace URI of all the path parts.</param>
        /// <param name="pathParts">The browse names of the path parts up to the node to retrieve.</param>
        /// <returns>The node reached by traversing the path parts from the starting node.</returns>
        protected NodeId FindNode(NodeId startingNode, string namespaceUri, params string[] pathParts)
            => FindNode(
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
            => _simulator.FireTimersWithPeriod((uint) periodInMilliseconds.TotalMilliseconds, numberOfTimes);

        private NodeId FindNode(NodeId startingNode, string relativePath)
        {
            var browsePaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = startingNode,
                    RelativePath = Opc.Ua.RelativePath.Parse(relativePath, Session.TypeTree)
                }
            };

            Session.TranslateBrowsePathsToNodeIds(
                null,
                browsePaths,
                out var results,
                out _);

            var nodeId = results
                .Should().ContainSingle("search should retain a result")
                .Subject.Targets
                .Should().ContainSingle("search for {0} should retain a result target", relativePath)
                .Subject.TargetId;
            return ToNodeId(nodeId);
        }

        protected T ReadValue<T>(NodeId nodeId)
        {
            return (T)Session.ReadValue(nodeId, typeof(T));
        }

        protected StatusCodeCollection WriteValue(NodeId nodeId, object newValue)
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
                    }
                }
            };

            // write value.
            Session.Write(
                default,
                valuesToWrite,
                out var results,
                out _);

            return results;
        }
    }
}