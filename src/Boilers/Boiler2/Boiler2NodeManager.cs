/* ========================================================================
 * Copyright (c) 2005-2023 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Opc.Ua;
using Opc.Ua.Server;

namespace OpcPlc.Boiler2
{
    /// <summary>
    /// The node manager factory for test data.
    /// </summary>
    public class Boiler2ManagerFactory : INodeManagerFactory
    {
        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new Boiler2NodeManager(server, configuration, NamespacesUris.ToArray());
        }

        /// <inheritdoc/>
        public StringCollection NamespacesUris
        {
            get
            {
                var nameSpaces = new StringCollection {
                    Namespaces.Boiler2
                };
                return nameSpaces;
            }
        }
    }

    /// <summary>
    /// A node manager for a variety of test data.
    /// </summary>
    public class Boiler2NodeManager : CustomNodeManager2
    {
        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public Boiler2NodeManager(Opc.Ua.Server.IServerInternal server, ApplicationConfiguration configuration, string[] namespaceUris)
        :
            base(server, configuration)
        {
            // update the namespaces.
            NamespaceUris = namespaceUris;

            Server.Factory.AddEncodeableTypes(typeof(Boiler2NodeManager).Assembly.GetExportedTypes()
                .Where(t => t.FullName.StartsWith(typeof(Boiler2NodeManager).Namespace, StringComparison.Ordinal)));
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
            lock (Lock)
            {
                // ensure the namespace used by the node manager is in the server's namespace table.
                m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(Namespaces.Boiler2);

                base.CreateAddressSpace(externalReferences);

                ConfigurationNodeManager configurationNodeManager = Server.NodeManager.ConfigurationNodeManager;
                if (configurationNodeManager != null)
                {
                    var nameSpaceMetaDataState = configurationNodeManager.CreateNamespaceMetadataState(Server.NamespaceUris.GetString(m_namespaceIndex));
                }
            }
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            predefinedNodes.LoadFromBinaryResource(context, "opcplc.Boilers.Boiler2.OpcPlc.Boiler2.PredefinedNodes.uanodes",
                this.GetType().GetTypeInfo().Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            var variableNode = predefinedNode as BaseVariableState;
            if (variableNode != null)
            {
                if (variableNode.Value == null)
                {
                    var dataTypeId = variableNode.DataType;
                    if (IsNodeIdInNamespace(dataTypeId))
                    {
                        if (variableNode.Parent is BaseVariableTypeState)
                        {
                            return predefinedNode;
                        }

                        switch ((uint)dataTypeId.Identifier)
                        {
                            default: break;
                        }
                    }
                }
            }

            var passiveNode = predefinedNode as BaseObjectState;

            if (passiveNode == null)
            {
                return predefinedNode;
            }

            NodeId typeId = passiveNode.TypeDefinitionId;

            if (!IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
            {
                return predefinedNode;
            }

            return predefinedNode;
        }
        #endregion

        #region Private Fields
        private object m_lock = new object();
        private ushort m_namespaceIndex;
        #endregion
    }

    #region PropertyTypeValue Class
    /// <remarks />
    /// <exclude />
    public class PropertyTypeValue<T> : BaseVariableValue
    {
        #region Constructors
        /// <remarks />
        public PropertyTypeValue(BaseDataVariableState<T> variable, List<BaseVariableState> variableStates, T value, object dataLock) : base(dataLock)
        {
            m_value = value;

            if (m_value == null)
            {
                //m_value = new T();
            }

            Initialize(variable, variableStates);
        }
        #endregion

        #region Public Members
        /// <remarks />
        public BaseDataVariableState<T> Variable
        {
            get { return m_variable; }
        }

        /// <remarks />
        public T Value
        {
            get { return m_value; }
            set { m_value = value; }
        }
        #endregion

        #region Private Methods
        private void Initialize(BaseDataVariableState<T> variable, List<BaseVariableState> variableStates)
        {
            lock (Lock)
            {
                m_variable = variable;
                m_variableStates = variableStates;

                variable.Value = m_value;

                variable.OnReadValue = OnReadValue;
                variable.OnSimpleWriteValue = OnWriteValue;

                List<BaseInstanceState> updateList = new List<BaseInstanceState>();
                updateList.Add(variable);

                foreach (var instance in m_variableStates)
                {
                    instance.OnReadValue = OnRead_Field;
                    instance.OnSimpleWriteValue = OnWrite_Field;
                    updateList.Add(instance);
                }

                SetUpdateList(updateList);
            }
        }

        /// <remarks />
        protected ServiceResult OnReadValue(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            lock (Lock)
            {
                DoBeforeReadProcessing(context, node);

                if (m_value != null)
                {
                    value = m_value;
                }

                return Read(context, node, indexRange, dataEncoding, ref value, ref statusCode, ref timestamp);
            }
        }

        private ServiceResult OnWriteValue(ISystemContext context, NodeState node, ref object value)
        {
            lock (Lock)
            {
                m_value = (T)Write(value);
            }

            return ServiceResult.Good;
        }

        #region Field Access Methods
        /// <remarks />
        private ServiceResult OnRead_Field(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            lock (Lock)
            {
                DoBeforeReadProcessing(context, node);

                if (m_value != null)
                {
                    var property = typeof(T).GetProperty(node.DisplayName.Text);
                    if (property != null)
                    {
                        value = property.GetValue(m_value);
                    }
                }

                return Read(context, node, indexRange, dataEncoding, ref value, ref statusCode, ref timestamp);
            }
        }

        /// <remarks />
        private ServiceResult OnWrite_Field(ISystemContext context, NodeState node, ref object value)
        {
            lock (Lock)
            {
                var property = typeof(T).GetProperty(node.DisplayName.Text);
                if (property != null)
                {
                    property.SetValue(m_value, Write(value));
                }
            }

            return ServiceResult.Good;
        }
        #endregion
        #endregion

        #region Private Fields
        private T m_value;
        private BaseDataVariableState<T> m_variable;
        private List<BaseVariableState> m_variableStates;
        #endregion
    }
    #endregion
}
