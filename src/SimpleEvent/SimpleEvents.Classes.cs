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

namespace OpcPlc.SimpleEvent
{
    using Opc.Ua;
    using SimpleEvents;
    using System.Collections.Generic;

    #region SystemCycleStatusEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleStatusEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleStatusEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleStatusEventState : SystemEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStatusEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStatusEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
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
           "AQAAACwAAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL1NpbXBsZUV2ZW50c/////8EYIAC" +
           "AQAAAAEAIgAAAFN5c3RlbUN5Y2xlU3RhdHVzRXZlbnRUeXBlSW5zdGFuY2UBAQIAAQECAAIAAAD/////" +
           "CgAAABVgiQoCAAAAAAAHAAAARXZlbnRJZAEBAwAALgBEAwAAAAAP/////wEB/////wAAAAAVYIkKAgAA" +
           "AAAACQAAAEV2ZW50VHlwZQEBBAAALgBEBAAAAAAR/////wEB/////wAAAAAVYIkKAgAAAAAACgAAAFNv" +
           "dXJjZU5vZGUBAQUAAC4ARAUAAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABTb3VyY2VOYW1l" +
           "AQEGAAAuAEQGAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAAAEAAAAVGltZQEBBwAALgBEBwAAAAEA" +
           "JgH/////AQH/////AAAAABVgiQoCAAAAAAALAAAAUmVjZWl2ZVRpbWUBAQgAAC4ARAgAAAABACYB////" +
           "/wEB/////wAAAAAVYIkKAgAAAAAABwAAAE1lc3NhZ2UBAQoAAC4ARAoAAAAAFf////8BAf////8AAAAA" +
           "FWCJCgIAAAAAAAgAAABTZXZlcml0eQEBCwAALgBECwAAAAAF/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "BwAAAEN5Y2xlSWQBAQwAAC4ARAwAAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABDdXJyZW50" +
           "U3RlcAEBDQAALgBEDQAAAAEBAQD/////AQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<string> CycleId
        {
            get
            {
                return m_cycleId;
            }

            set
            {
                if (!ReferenceEquals(m_cycleId, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_cycleId = value;
            }
        }

        /// <remarks />
        public PropertyState<CycleStepDataType> CurrentStep
        {
            get
            {
                return m_currentStep;
            }

            set
            {
                if (!ReferenceEquals(m_currentStep, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_currentStep = value;
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
            if (m_cycleId != null)
            {
                children.Add(m_cycleId);
            }

            if (m_currentStep != null)
            {
                children.Add(m_currentStep);
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
                case SimpleEvents.BrowseNames.CycleId:
                    {
                        if (createOrReplace)
                        {
                            if (CycleId == null)
                            {
                                if (replacement == null)
                                {
                                    CycleId = new PropertyState<string>(this);
                                }
                                else
                                {
                                    CycleId = (PropertyState<string>)replacement;
                                }
                            }
                        }

                        instance = CycleId;
                        break;
                    }

                case SimpleEvents.BrowseNames.CurrentStep:
                    {
                        if (createOrReplace)
                        {
                            if (CurrentStep == null)
                            {
                                if (replacement == null)
                                {
                                    CurrentStep = new PropertyState<CycleStepDataType>(this);
                                }
                                else
                                {
                                    CurrentStep = (PropertyState<CycleStepDataType>)replacement;
                                }
                            }
                        }

                        instance = CurrentStep;
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
        private PropertyState<string> m_cycleId;
        private PropertyState<CycleStepDataType> m_currentStep;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleStartedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleStartedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleStartedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleStartedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleStartedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStartedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
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
           "AQAAACwAAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL1NpbXBsZUV2ZW50c/////8EYIAC" +
           "AQAAAAEAIwAAAFN5c3RlbUN5Y2xlU3RhcnRlZEV2ZW50VHlwZUluc3RhbmNlAQEOAAEBDgAOAAAA////" +
           "/wsAAAAVYIkKAgAAAAAABwAAAEV2ZW50SWQBAQ8AAC4ARA8AAAAAD/////8BAf////8AAAAAFWCJCgIA" +
           "AAAAAAkAAABFdmVudFR5cGUBARAAAC4ARBAAAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABT" +
           "b3VyY2VOb2RlAQERAAAuAEQRAAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAKAAAAU291cmNlTmFt" +
           "ZQEBEgAALgBEEgAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAAABAAAAFRpbWUBARMAAC4ARBMAAAAB" +
           "ACYB/////wEB/////wAAAAAVYIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQEUAAAuAEQUAAAAAQAmAf//" +
           "//8BAf////8AAAAAFWCJCgIAAAAAAAcAAABNZXNzYWdlAQEWAAAuAEQWAAAAABX/////AQH/////AAAA" +
           "ABVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBARcAAC4ARBcAAAAABf////8BAf////8AAAAAFWCJCgIAAAAB" +
           "AAcAAABDeWNsZUlkAQEYAAAuAEQYAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQALAAAAQ3VycmVu" +
           "dFN0ZXABARkAAC4ARBkAAAABAQEA/////wEB/////wAAAAAXYIkKAgAAAAEABQAAAFN0ZXBzAQEaAAAu" +
           "AEQaAAAAAQEBAAEAAAABAAAAAAAAAAEB/////wAAAAA=";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<CycleStepDataType[]> Steps
        {
            get
            {
                return m_steps;
            }

            set
            {
                if (!ReferenceEquals(m_steps, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_steps = value;
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
            if (m_steps != null)
            {
                children.Add(m_steps);
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
                case SimpleEvents.BrowseNames.Steps:
                    {
                        if (createOrReplace)
                        {
                            if (Steps == null)
                            {
                                if (replacement == null)
                                {
                                    Steps = new PropertyState<CycleStepDataType[]>(this);
                                }
                                else
                                {
                                    Steps = (PropertyState<CycleStepDataType[]>)replacement;
                                }
                            }
                        }

                        instance = Steps;
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
        private PropertyState<CycleStepDataType[]> m_steps;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleAbortedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleAbortedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleAbortedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleAbortedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleAbortedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleAbortedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
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
           "AQAAACwAAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL1NpbXBsZUV2ZW50c/////8EYIAC" +
           "AQAAAAEAIwAAAFN5c3RlbUN5Y2xlQWJvcnRlZEV2ZW50VHlwZUluc3RhbmNlAQEbAAEBGwAbAAAA////" +
           "/wsAAAAVYIkKAgAAAAAABwAAAEV2ZW50SWQBARwAAC4ARBwAAAAAD/////8BAf////8AAAAAFWCJCgIA" +
           "AAAAAAkAAABFdmVudFR5cGUBAR0AAC4ARB0AAAAAEf////8BAf////8AAAAAFWCJCgIAAAAAAAoAAABT" +
           "b3VyY2VOb2RlAQEeAAAuAEQeAAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAKAAAAU291cmNlTmFt" +
           "ZQEBHwAALgBEHwAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAAABAAAAFRpbWUBASAAAC4ARCAAAAAB" +
           "ACYB/////wEB/////wAAAAAVYIkKAgAAAAAACwAAAFJlY2VpdmVUaW1lAQEhAAAuAEQhAAAAAQAmAf//" +
           "//8BAf////8AAAAAFWCJCgIAAAAAAAcAAABNZXNzYWdlAQEjAAAuAEQjAAAAABX/////AQH/////AAAA" +
           "ABVgiQoCAAAAAAAIAAAAU2V2ZXJpdHkBASQAAC4ARCQAAAAABf////8BAf////8AAAAAFWCJCgIAAAAB" +
           "AAcAAABDeWNsZUlkAQElAAAuAEQlAAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQALAAAAQ3VycmVu" +
           "dFN0ZXABASYAAC4ARCYAAAABAQEA/////wEB/////wAAAAAVYIkKAgAAAAEABQAAAEVycm9yAQEnAAAu" +
           "AEQnAAAAABP/////AQH/////AAAAAA==";
        #endregion
#endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<StatusCode> Error
        {
            get
            {
                return m_error;
            }

            set
            {
                if (!ReferenceEquals(m_error, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_error = value;
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
            if (m_error != null)
            {
                children.Add(m_error);
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
                case SimpleEvents.BrowseNames.Error:
                    {
                        if (createOrReplace)
                        {
                            if (Error == null)
                            {
                                if (replacement == null)
                                {
                                    Error = new PropertyState<StatusCode>(this);
                                }
                                else
                                {
                                    Error = (PropertyState<StatusCode>)replacement;
                                }
                            }
                        }

                        instance = Error;
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
        private PropertyState<StatusCode> m_error;
        #endregion
    }
#endif
    #endregion

    #region SystemCycleFinishedEventState Class
#if (!OPCUA_EXCLUDE_SystemCycleFinishedEventState)
    /// <summary>
    /// Stores an instance of the SystemCycleFinishedEventType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCode("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SystemCycleFinishedEventState : SystemCycleStatusEventState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public SystemCycleFinishedEventState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleFinishedEventType, SimpleEvents.Namespaces.SimpleEvents, namespaceUris);
        }

#if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
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
           "AQAAACwAAABodHRwOi8vbWljcm9zb2Z0LmNvbS9PcGMvT3BjUGxjL1NpbXBsZUV2ZW50c/////8EYIAC" +
           "AQAAAAEAJAAAAFN5c3RlbUN5Y2xlRmluaXNoZWRFdmVudFR5cGVJbnN0YW5jZQEBKAABASgAKAAAAP//" +
           "//8KAAAAFWCJCgIAAAAAAAcAAABFdmVudElkAQEpAAAuAEQpAAAAAA//////AQH/////AAAAABVgiQoC" +
           "AAAAAAAJAAAARXZlbnRUeXBlAQEqAAAuAEQqAAAAABH/////AQH/////AAAAABVgiQoCAAAAAAAKAAAA" +
           "U291cmNlTm9kZQEBKwAALgBEKwAAAAAR/////wEB/////wAAAAAVYIkKAgAAAAAACgAAAFNvdXJjZU5h" +
           "bWUBASwAAC4ARCwAAAAADP////8BAf////8AAAAAFWCJCgIAAAAAAAQAAABUaW1lAQEtAAAuAEQtAAAA" +
           "AQAmAf////8BAf////8AAAAAFWCJCgIAAAAAAAsAAABSZWNlaXZlVGltZQEBLgAALgBELgAAAAEAJgH/" +
           "////AQH/////AAAAABVgiQoCAAAAAAAHAAAATWVzc2FnZQEBMAAALgBEMAAAAAAV/////wEB/////wAA" +
           "AAAVYIkKAgAAAAAACAAAAFNldmVyaXR5AQExAAAuAEQxAAAAAAX/////AQH/////AAAAABVgiQoCAAAA" +
           "AQAHAAAAQ3ljbGVJZAEBMgAALgBEMgAAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAEN1cnJl" +
           "bnRTdGVwAQEzAAAuAEQzAAAAAQEBAP////8BAf////8AAAAA";
        #endregion
#endif
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        #endregion

        #region Private Fields
        #endregion
    }
#endif
    #endregion
}