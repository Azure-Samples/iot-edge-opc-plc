/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
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
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace AggregateStateModel
{
    #region AggregateStateState Class
    #if (!OPCUA_EXCLUDE_AggregateStateState)
    /// <summary>
    /// Stores an instance of the AggregateStateType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class AggregateStateState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public AggregateStateState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(AggregateStateModel.ObjectTypes.AggregateStateType, AggregateStateModel.Namespaces.AggregateState, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the instance with a node.
        /// </summary>
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAC4AAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL0FnZ3JlZ2F0ZVN0YXRl/////4Rg" +
           "gAABAAAAAQAaAAAAQWdncmVnYXRlU3RhdGVUeXBlSW5zdGFuY2UBAZo6AQGaOgH/////AQAAABVgqQoC" +
           "AAAAAQAOAAAAQWdncmVnYXRlU3RhdGUBAZs6AC8AP5s6AAAWAQGmOgKdAAAAPEFnZ3JlZ2F0ZVN0YXRl" +
           "RGF0YVR5cGUgeG1sbnM9Imh0dHA6Ly9taWNyb3NvZnQuY29tL09wYy9PcGNQbGMvQWdncmVnYXRlU3Rh" +
           "dGUiPjxUZW1wZXJhdHVyZT4tNTwvVGVtcGVyYXR1cmU+PFByZXNzdXJlPjEuMDwvUHJlc3N1cmU+PC9B" +
           "Z2dyZWdhdGVTdGF0ZURhdGFUeXBlPgEBmTr/////AQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the AggregateState Variable.
        /// </summary>
        public BaseDataVariableState<AggregateStateDataType> AggregateState
        {
            get
            {
                return m_aggregateState;
            }

            set
            {
                if (!Object.ReferenceEquals(m_aggregateState, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_aggregateState = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_aggregateState != null)
            {
                children.Add(m_aggregateState);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case AggregateStateModel.BrowseNames.AggregateState:
                {
                    if (createOrReplace)
                    {
                        if (AggregateState == null)
                        {
                            if (replacement == null)
                            {
                                AggregateState = new BaseDataVariableState<AggregateStateDataType>(this);
                            }
                            else
                            {
                                AggregateState = (BaseDataVariableState<AggregateStateDataType>)replacement;
                            }
                        }
                    }

                    instance = AggregateState;
                    break;
                }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private BaseDataVariableState<AggregateStateDataType> m_aggregateState;
        #endregion
    }
    #endif
    #endregion
}