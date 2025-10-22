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

namespace AlarmCondition;

using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Test;
using OpcPlc.AlarmCondition;
using System;
using System.Collections.Generic;
using System.Threading;


/// <summary>
/// A node manager for a simple server that exposes several Areas, Sources and Conditions.
/// </summary>
/// <remarks>
/// This node manager presumes that the information model consists of a hierarchy of predefined
/// Areas with a number of Sources contained within them. Each individual Source is
/// identified by a fully qualified path. The underlying system knows how to access the source
/// configuration when it is provided the fully qualified path.
/// </remarks>
public class AlarmConditionServerNodeManager : CustomNodeManager2
{
    #region Constructors
    /// <summary>
    /// Initializes the node manager.
    /// </summary>
    public AlarmConditionServerNodeManager(IServerInternal server, ApplicationConfiguration _)
    :
        base(server, _, OpcPlc.Namespaces.OpcPlcAlarmsInstance)
    {
        SystemContext.SystemHandle = m_system = new UnderlyingSystem();
        SystemContext.NodeIdFactory = this;

        // create the table to store the available areas.
        m_areas = new Dictionary<string, AreaState>();

        // create the table to store the available sources.
        m_sources = new Dictionary<string, SourceState>();
    }
    #endregion

    #region IDisposable Members
    /// <summary>
    /// An overridable version of the Dispose.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_system?.Dispose();
        }

        base.Dispose(disposing);
    }
    #endregion

    #region INodeIdFactory Members
    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="node">The node.</param>
    /// <returns>The new NodeId.</returns>
    /// <remarks>
    /// This method is called by the NodeState.Create() method which initializes a Node from
    /// the type model. During initialization a number of child nodes are created and need to
    /// have NodeIds assigned to them. This implementation constructs NodeIds by constructing
    /// strings. Other implementations could assign unique integers or Guids and save the new
    /// Node in a dictionary for later lookup.
    /// </remarks>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        return ModelUtils.ConstructIdForComponent(node, NamespaceIndex);
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
    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        var alarmConfig = CreateAreaConfigurationCollection();
        lock (Lock)
        {
            if (alarmConfig != null)
            {
                // Top level areas need a reference from the Server object.
                // These references are added to a list that is returned to the caller.
                // The caller will update the Objects folder node.
                if (!externalReferences.TryGetValue(ObjectIds.Server, out IList<IReference> references))
                {
                    externalReferences[ObjectIds.Server] = references = new List<IReference>();
                }

                for (int ii = 0; ii < alarmConfig.Count; ii++)
                {
                    // recursively process each area.
                    AreaState area = CreateAndIndexAreas(null, alarmConfig[ii]);
                    AddRootNotifier(area);

                    // add an organizes reference from the ObjectsFolder to the area.
                    references.Add(new NodeStateReference(ReferenceTypeIds.HasNotifier, false, area.NodeId));
                }
            }

            StartDeterministicEvents();

            // Existing generic system/audit events every second.
            m_simulationTimer = new Timer(OnRaiseSystemEvents, state: null, dueTime: 1000, period: 1000);

            // Alarm state simulation runs in parallel.
            m_system.StartSimulation();
        }
    }

    private void StartDeterministicEvents()
    {
        // Deterministic round-robin event emission (one per second) over a fixed ordered list of sources.
        // Order: WestTank, EastTank, NorthMotor, SouthMotor.
        string[] deterministicSourcePaths =
        [
                "Metals/WestTank",
                "Colours/EastTank",
                "Colours/NorthMotor",
                "Metals/SouthMotor"
        ];

        var ordered = new List<SourceState>(deterministicSourcePaths.Length);
        foreach (var sp in deterministicSourcePaths)
        {
            if (m_sources.TryGetValue(sp, out var src))
            {
                ordered.Add(src);
            }
        }
        _deterministicEventSources = ordered.ToArray();
        _deterministicEventIndex = 0;

        // Start deterministic event timer (1 second period).
        _deterministicEventTimer = new Timer(OnDeterministicEvent, state: null, dueTime: 1000, period: 1000);
    }

    private AreaConfigurationCollection CreateAreaConfigurationCollection()
    {
        return new AreaConfigurationCollection
        {
            new AreaConfiguration
            {
                Name = "Green",
                SubAreas = new AreaConfigurationCollection
                {
                    new AreaConfiguration
                    {
                        Name = "East",
                        SubAreas = new AreaConfigurationCollection
                        {
                            new AreaConfiguration
                            {
                                Name = "Red",
                                SourcePaths =  new StringCollection { "Metals/SouthMotor", "Colours/NorthMotor" }
                            },
                            new AreaConfiguration
                            {
                                Name = "Blue",
                                SourcePaths =  new StringCollection { "Metals/WestTank", "Metals/SouthMotor" }
                            }
                        }
                    }
                }
            },
            new AreaConfiguration
            {
                Name = "Yellow",
                SubAreas = new AreaConfigurationCollection
                {
                   new AreaConfiguration
                   {
                       Name = "West",
                       SubAreas = new AreaConfigurationCollection
                       {
                            new AreaConfiguration
                            {
                                Name = "Red",
                                SourcePaths =  new StringCollection { "Metals/SouthMotor", "Colours/NorthMotor" }
                            },
                           new AreaConfiguration
                            {
                                Name = "Blue",
                                SourcePaths =  new StringCollection { "Colours/EastTank", "Metals/WestTank" }
                            }

                       }
                   }
                }
            }
        };
    }
    private void OnRaiseSystemEvents(object state)
    {
        try
        {
            SystemEventState e = new(null);

            e.Initialize(
                SystemContext,
                source: null,
                EventSeverity.Medium,
                new LocalizedText("Raising Events"));

            e.SetChildValue(SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);
            e.SetChildValue(SystemContext, BrowseNames.SourceName, "Internal", false);

            Server.ReportEvent(e);

            var aes = new AuditEventState(null);

            aes.Initialize(
                SystemContext,
                source: null,
                EventSeverity.Medium,
                new LocalizedText("Events Raised"),
                status: true,
                DateTime.UtcNow);

            aes.SetChildValue(SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);
            aes.SetChildValue(SystemContext, BrowseNames.SourceName, "Internal", false);

            Server.ReportEvent(aes);
        }
        catch (Exception e)
        {
            Utils.Trace(e, "Unexpected error in OnRaiseSystemEvents");
        }
    }

    private void OnDeterministicEvent(object state)
    {
        try
        {
            SourceState source;
            lock (Lock)
            {
                if (_deterministicEventSources == null || _deterministicEventSources.Length == 0)
                {
                    return;
                }
                source = _deterministicEventSources[_deterministicEventIndex];
                _deterministicEventIndex = (_deterministicEventIndex + 1) % _deterministicEventSources.Length;
            }

            // Access underlying system source to emit a deterministic alarm snapshot.
            if (source.SymbolicName != null &&
                ((UnderlyingSystem)SystemContext.SystemHandle) is UnderlyingSystem sys &&
                sys.TryGetSource(source.SymbolicName, out var underlying))
            {
                underlying.TriggerSnapshot($"Deterministic heartbeat for {source.SymbolicName}");
            }
        }
        catch (Exception ex)
        {
            Utils.Trace(ex, "Unexpected error in OnDeterministicEvent");
        }
    }

    #region CreateAddressSpace Support Functions
    /// <summary>
    /// Creates and indexes an area defined for the server.
    /// </summary>
    private AreaState CreateAndIndexAreas(AreaState parent, AreaConfiguration configuration)
    {
        // create a unique path to the area.
        string areaPath = Utils.Format("{0}/{1}", (parent != null) ? parent.SymbolicName : String.Empty, configuration.Name);
        NodeId areaId = ModelUtils.ConstructIdForArea(areaPath, NamespaceIndex);

        // create the object that will be used to access the area and any variables contained within it.
        AreaState area = new(SystemContext, parent, areaId, configuration);
        m_areas[areaPath] = area;

        parent?.AddChild(area);

        // create an index any sub-areas defined for the area.
        if (configuration.SubAreas != null)
        {
            for (int ii = 0; ii < configuration.SubAreas.Count; ii++)
            {
                CreateAndIndexAreas(area, configuration.SubAreas[ii]);
            }
        }

        // add references to sources.
        if (configuration.SourcePaths != null)
        {
            for (int ii = 0; ii < configuration.SourcePaths.Count; ii++)
            {
                string sourcePath = configuration.SourcePaths[ii];

                // check if the source already exists because it is referenced by another area.

                if (!m_sources.TryGetValue(sourcePath, out SourceState source))
                {
                    NodeId sourceId = ModelUtils.ConstructIdForSource(sourcePath, NamespaceIndex);
                    ResetRandomGenerator(ii);
                    m_sources[sourcePath] = source = new SourceState(this, sourceId, sourcePath, m_generator);
                }

                // HasEventSource and HasNotifier control the propagation of event notifications so
                // they are not like other references. These calls set up a link between the source
                // and area that will cause events produced by the source to be automatically
                // propagated to the area.
                source.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, true, area);
                area.AddNotifier(SystemContext, ReferenceTypeIds.HasEventSource, false, source);
            }
        }

        return area;
    }
    #endregion

    /// <summary>
    /// Frees any resources allocated for the address space.
    /// </summary>
    public override void DeleteAddressSpace()
    {
        lock (Lock)
        {
            m_system.StopSimulation();
            m_areas.Clear();
            m_sources.Clear();
        }
    }

    /// <summary>
    /// Returns a unique handle for the node.
    /// </summary>
    protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
    {
        lock (Lock)
        {
            // quickly exclude nodes that are not in the namespace.
            if (!IsNodeIdInNamespace(nodeId))
            {
                return null;
            }

            // check for check for nodes that are being currently monitored.

            if (MonitoredNodes.TryGetValue(nodeId, out var monitoredNode))
            {
                NodeHandle handle = new() {
                    NodeId = nodeId,
                    Validated = true,
                    Node = monitoredNode.Node,
                };

                return handle;
            }

            // parse the identifier.
            ParsedNodeId parsedNodeId = ParsedNodeId.Parse(nodeId);

            if (parsedNodeId != null)
            {
                var handle = new NodeHandle {
                    NodeId = nodeId,
                    Validated = false,
                    Node = null,
                    ParsedNodeId = parsedNodeId
                };

                return handle;
            }

            return null;
        }
    }

    /// <summary>
    /// Verifies that the specified node exists.
    /// </summary>
    protected override NodeState ValidateNode(
        ServerSystemContext context,
        NodeHandle handle,
        IDictionary<NodeId, NodeState> cache)
    {
        // not valid if no root.
        if (handle == null)
        {
            return null;
        }

        // check if previously validated.
        if (handle.Validated)
        {
            return handle.Node;
        }

        NodeState target = null;

        // check if already in the cache.
        if (cache != null)
        {
            if (cache.TryGetValue(handle.NodeId, out target))
            {
                // nulls mean a NodeId which was previously found to be invalid has been referenced again.
                if (target == null)
                {
                    return null;
                }

                handle.Node = target;
                handle.Validated = true;
                return handle.Node;
            }

            target = null;
        }

        try
        {
            // check if the node id has been parsed.
            if (handle.ParsedNodeId is not ParsedNodeId parsedNodeId)
            {
                return null;
            }

            NodeState root = null;

            // validate area.
            if (parsedNodeId.RootType == ModelUtils.Area)
            {

                if (!m_areas.TryGetValue(parsedNodeId.RootId, out AreaState area))
                {
                    return null;
                }

                root = area;
            }

            // validate source.
            else if (parsedNodeId.RootType == ModelUtils.Source)
            {

                if (!m_sources.TryGetValue(parsedNodeId.RootId, out SourceState source))
                {
                    return null;
                }

                root = source;
            }

            // unknown root type.
            else
            {
                return null;
            }

            // all done if no components to validate.
            if (String.IsNullOrEmpty(parsedNodeId.ComponentPath))
            {
                handle.Validated = true;
                handle.Node = target = root;
                return handle.Node;
            }

            // validate component.
            NodeState component = root.FindChildBySymbolicName(context, parsedNodeId.ComponentPath);

            // component does not exist.
            if (component == null)
            {
                return null;
            }

            // found a valid component.
            handle.Validated = true;
            handle.Node = target = component;
            return handle.Node;
        }
        finally
        {
            // store the node in the cache to optimize subsequent lookups.
            cache?.Add(handle.NodeId, target);
        }
    }

    private void ResetRandomGenerator(int seed, int boundaryValueFrequency = 0)
    {
        m_generator = new DataGenerator(new RoundRobinSource(seed));
        m_generator.BoundaryValueFrequency = boundaryValueFrequency;
    }

    #endregion

    #region Private Fields
    private readonly UnderlyingSystem m_system;
    private readonly Dictionary<string, AreaState> m_areas;
    private readonly Dictionary<string, SourceState> m_sources;
    private Timer m_simulationTimer;
    private DataGenerator m_generator;

    private Timer _deterministicEventTimer;
    private SourceState[] _deterministicEventSources;
    private int _deterministicEventIndex;
    #endregion
}
