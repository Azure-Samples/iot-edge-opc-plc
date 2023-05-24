/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace BoilerModel1
{
    #region Boiler1State Class
    #if (!OPCUA_EXCLUDE_Boiler1State)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class Boiler1State : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public Boiler1State(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(BoilerModel1.ObjectTypes.Boiler1Type, BoilerModel1.Namespaces.Boiler, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <remarks />
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACYAAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL0JvaWxlcv////+EYIACAQAAAAEA" +
           "EwAAAEJvaWxlcjFUeXBlSW5zdGFuY2UBAQMAAQEDAAMAAAAB/////wEAAAAVYKkKAgAAAAEADAAAAEJv" +
           "aWxlclN0YXR1cwEBBAAALwA/BAAAABYBAew6AsMAAAA8Qm9pbGVyRGF0YVR5cGUgeG1sbnM9Imh0dHA6" +
           "Ly9taWNyb3NvZnQuY29tL09wYy9PcGNQbGMvQm9pbGVyIj48VGVtcGVyYXR1cmU+PFRvcD4yMDwvVG9w" +
           "PjxCb3R0b20+MjA8L0JvdHRvbT48L1RlbXBlcmF0dXJlPjxQcmVzc3VyZT4xMDAwMjA8L1ByZXNzdXJl" +
           "PjxIZWF0ZXJTdGF0ZT5PbjwvSGVhdGVyU3RhdGU+PC9Cb2lsZXJEYXRhVHlwZT4BAbg6/////wEB////" +
           "/wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<BoilerDataType> BoilerStatus
        {
            get
            {
                return m_boilerStatus;
            }

            set
            {
                if (!Object.ReferenceEquals(m_boilerStatus, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_boilerStatus = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_boilerStatus != null)
            {
                children.Add(m_boilerStatus);
            }

            base.GetChildren(context, children);
        }
            
        /// <remarks />
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
                case BoilerModel1.BrowseNames.BoilerStatus:
                {
                    if (createOrReplace)
                    {
                        if (BoilerStatus == null)
                        {
                            if (replacement == null)
                            {
                                BoilerStatus = new BaseDataVariableState<BoilerDataType>(this);
                            }
                            else
                            {
                                BoilerStatus = (BaseDataVariableState<BoilerDataType>)replacement;
                            }
                        }
                    }

                    instance = BoilerStatus;
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
        private BaseDataVariableState<BoilerDataType> m_boilerStatus;
        #endregion
    }
    #endif
    #endregion
}