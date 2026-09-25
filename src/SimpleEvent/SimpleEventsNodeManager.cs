/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace SimpleEvents;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using OpcPlc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A node manager for a server that exposes several variables.
/// </summary>
public sealed class SimpleEventsNodeManager : AsyncCustomNodeManager
{
    #region Constructors
    /// <summary>
    /// Initializes the node manager.
    /// </summary>
    public SimpleEventsNodeManager(IServerInternal server, ApplicationConfiguration _, ILogger logger)
    :
        base(server, _)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SystemContext.NodeIdFactory = this;

        // set one namespace for the type model and one names for dynamically created nodes.
        string[] namespaceUrls = [OpcPlc.Namespaces.OpcPlcSimpleEvents];
        SetNamespaces(namespaceUrls);
    }

    #endregion
    #region IDisposable Members
    /// <summary>
    /// An overrideable version of the Dispose.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref m_simulationTimer, null)?.Dispose();
        }
        base.Dispose(disposing);
    }
    #endregion

    #region INodeIdFactory Members
    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        return node.NodeId;
    }
    #endregion

    #region Overridden Methods
    /// <summary>
    /// Loads a node set from a file or resource and adds them to the set of predefined nodes.
    /// </summary>
    protected override ValueTask<NodeStateCollection> LoadPredefinedNodesAsync(
        ISystemContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xmlPath = "SimpleEvent/SimpleEvents.NodeSet2.xml";
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // Application running as a snap
            xmlPath = Path.Join(snapLocation, xmlPath);
        }

        var predefinedNodes = new NodeStateCollection();
        using var stream = File.OpenRead(xmlPath);
        var nodeSet = Opc.Ua.Export.UANodeSet.Read(stream);
        nodeSet.Import(context, predefinedNodes);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(predefinedNodes);
    }
    #endregion

    #region INodeManager Members
    /// <summary>
    /// Does any initialization required before the address space can be used.
    /// </summary>
    /// <remarks>
    /// The externalReferences is an out parameter that allows the node manager to link to nodes
    /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
    /// should have a reference to the root folder node(s) exposed by this node manager.
    /// </remarks>
    public override async ValueTask CreateAddressSpaceAsync(
        IDictionary<NodeId, IList<IReference>> externalReferences, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await LoadPredefinedNodesAsync(SystemContext, externalReferences, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        m_simulationTimer = new Timer(DoSimulation, state: null, 3000, 3000);
    }

    /// <summary>
    /// Frees any resources allocated for the address space.
    /// </summary>
    public override async ValueTask DeleteAddressSpaceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Timer timer = Interlocked.Exchange(ref m_simulationTimer, null);
        if (timer is not null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
        }
        await base.DeleteAddressSpaceAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a unique handle for the node.
    /// </summary>
    protected override ValueTask<NodeHandle> GetManagerHandleAsync(
        ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // quickly exclude nodes that are not in the namespace.
        if (!IsNodeIdInNamespace(nodeId))
        {
            return ValueTask.FromResult<NodeHandle>(null);
        }

        // check for predefined nodes.
        if (PredefinedNodes != null && PredefinedNodes.TryGetValue(nodeId, out NodeState node))
        {
            var handle = new NodeHandle {
                NodeId = nodeId,
                Validated = true,
                Node = node,
            };

            return ValueTask.FromResult(handle);
        }

        return ValueTask.FromResult<NodeHandle>(null);
    }

    /// <summary>
    /// Verifies that the specified node exists.
    /// </summary>
    protected override ValueTask<NodeState> ValidateNodeAsync(
        ServerSystemContext context,
        NodeHandle handle,
        IDictionary<NodeId, NodeState> cache,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // not valid if no root.
        if (handle == null)
        {
            return ValueTask.FromResult<NodeState>(null);
        }

        // check if previously validated.
        if (handle.Validated)
        {
            return ValueTask.FromResult(handle.Node);
        }

        // TBD

        return ValueTask.FromResult<NodeState>(null);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Does the simulation.
    /// </summary>
    /// <param name="state">The state.</param>
    private void DoSimulation(object state)
    {
        try
        {
            if (!Server.Factory.TryGetEncodeableType(RuntimeModelIds.SimpleEvents.CycleStepDataType, out var stepType))
            {
                return;
            }

            for (int severity = 1; severity < 3; severity++)
            {
                if (Volatile.Read(ref m_simulationTimer) is null)
                {
                    return;
                }

                var cycleEvent = new SystemEventState(parent: null);
                cycleEvent.Initialize(
                    SystemContext,
                    source: null,
                    (EventSeverity)severity,
                    new LocalizedText($"The system cycle '{++m_cycleId}' has started."));

                cycleEvent.TypeDefinitionId = ExpandedNodeId.ToNodeId(
                    RuntimeModelIds.SimpleEvents.SystemCycleStartedEventType, Server.NamespaceUris);
                cycleEvent.EventType.Value = cycleEvent.TypeDefinitionId;
                cycleEvent.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceName, "System", copy: false);
                cycleEvent.SetChildValue(SystemContext, Opc.Ua.BrowseNames.SourceNode, Opc.Ua.ObjectIds.Server, copy: false);
                var step = (IStructure)stepType.CreateInstance();
                step["Name"] = "Step 1";
                step["Duration"] = 1000.0;
                var encodedStep = new ExtensionObject((IEncodeable)step);
                var stepDataType = ExpandedNodeId.ToNodeId(
                    RuntimeModelIds.SimpleEvents.CycleStepDataType, Server.NamespaceUris);
                AddEventProperty(cycleEvent, "CycleId", Opc.Ua.DataTypeIds.String, m_cycleId.ToString());
                AddEventProperty(cycleEvent, "CurrentStep", stepDataType, new Variant(encodedStep));
                AddEventProperty(cycleEvent, "Steps", stepDataType,
                    new Variant(new[] { encodedStep, encodedStep }.ToArrayOf()), ValueRanks.OneDimension);
                Server.ReportEvent(cycleEvent);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during simulation");
        }
    }

    private void AddEventProperty(BaseEventState cycleEvent, string name, NodeId dataType, Variant value,
        int valueRank = ValueRanks.Scalar)
    {
        cycleEvent.AddChild(new PropertyState(cycleEvent)
        {
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = new LocalizedText(name),
            DataType = dataType,
            ValueRank = valueRank,
            Value = value
        });
    }
    #endregion

    #region Private Fields
    private Timer m_simulationTimer;
    private int m_cycleId;
    private readonly ILogger _logger;
    #endregion
}
