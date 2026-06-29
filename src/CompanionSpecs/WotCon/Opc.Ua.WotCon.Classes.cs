/* ========================================================================
 * Copyright (c) 2005-2024 The OPC Foundation, Inc. All rights reserved.
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
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1515 // Consider making public types internal
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1028 // Enum Storage should be Int32

namespace Opc.Ua.WotCon
{
    #region WoTAssetConnectionManagementState Class
    #if (!OPCUA_EXCLUDE_WoTAssetConnectionManagementState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class WoTAssetConnectionManagementState : BaseObjectState
    {
        #region Constructors
        public WoTAssetConnectionManagementState(NodeState parent) : base(parent)
        {
        }

        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.WotCon.ObjectTypes.WoTAssetConnectionManagementType, Opc.Ua.WotCon.Namespaces.WotCon, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);

            if (SupportedWoTBindings != null)
            {
                SupportedWoTBindings.Initialize(context, SupportedWoTBindings_InitializationString);
            }

            if (DiscoverAssets != null)
            {
                DiscoverAssets.Initialize(context, DiscoverAssets_InitializationString);
            }

            if (CreateAssetForEndpoint != null)
            {
                CreateAssetForEndpoint.Initialize(context, CreateAssetForEndpoint_InitializationString);
            }

            if (ConnectionTest != null)
            {
                ConnectionTest.Initialize(context, ConnectionTest_InitializationString);
            }

            if (Configuration != null)
            {
                Configuration.Initialize(context, Configuration_InitializationString);
            }
        }

        #region Initialization String
        private const string SupportedWoTBindings_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////F2CJCgIAAAABABQA" +
           "AABTdXBwb3J0ZWRXb1RCaW5kaW5ncwEBKAAALgBEKAAAAAEAx1wBAAAAAQAAAAAAAAABAf////8AAAAA";

        private const string DiscoverAssets_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABAA4A" +
           "AABEaXNjb3ZlckFzc2V0cwEBKQAALwEBKQApAAAAAQH/////AQAAABdgqQoCAAAAAAAPAAAAT3V0cHV0" +
           "QXJndW1lbnRzAQEwAAAuAEQwAAAAlgEAAAABACoBASEAAAAOAAAAQXNzZXRFbmRwb2ludHMADAEAAAAB" +
           "AAAAAAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";

        private const string CreateAssetForEndpoint_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABYA" +
           "AABDcmVhdGVBc3NldEZvckVuZHBvaW50AQExAAAvAQExADEAAAABAf////8CAAAAF2CpCgIAAAAAAA4A" +
           "AABJbnB1dEFyZ3VtZW50cwEBMgAALgBEMgAAAJYCAAAAAQAqAQEYAAAACQAAAEFzc2V0TmFtZQAM////" +
           "/wAAAAAAAQAqAQEcAAAADQAAAEFzc2V0RW5kcG9pbnQADP////8AAAAAAAEAKAEBAAAAAQAAAAIAAAAB" +
           "Af////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAaoAAC4ARKoAAACWAQAAAAEAKgEB" +
           "FgAAAAcAAABBc3NldElkABH/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAA==";

        private const string ConnectionTest_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABAA4A" +
           "AABDb25uZWN0aW9uVGVzdAEBSwAALwEBSwBLAAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRB" +
           "cmd1bWVudHMBAUwAAC4AREwAAACWAQAAAAEAKgEBHAAAAA0AAABBc3NldEVuZHBvaW50AAz/////AAAA" +
           "AAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQFN" +
           "AAAuAERNAAAAlgIAAAABACoBARYAAAAHAAAAU3VjY2VzcwAB/////wAAAAAAAQAqAQEVAAAABgAAAFN0" +
           "YXR1cwAM/////wAAAAAAAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAA=";

        private const string Configuration_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCACgEAAAABAA0A" +
           "AABDb25maWd1cmF0aW9uAQFOAAAvAQFpAE4AAAD/////AAAAAA==";

        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABACgA" +
           "AABXb1RBc3NldENvbm5lY3Rpb25NYW5hZ2VtZW50VHlwZUluc3RhbmNlAQEBAAEBAQABAAAA/////wgA" +
           "AAAEYMAKAQAAABgAAABXb1RBc3NldE5hbWVfUGxhY2Vob2xkZXIBAA4AAAA8V29UQXNzZXROYW1lPgEB" +
           "AgAAIwA6AgAAAAEAAAABAMNEAAEBKgACAAAABGCACgEAAAABAAcAAABXb1RGaWxlAQGQAAAvAQFuAJAA" +
           "AAD/////CwAAABVgiQoCAAAAAAAEAAAAU2l6ZQEBkQAALgBEkQAAAAAJ/////wEB/////wAAAAAVYIkK" +
           "AgAAAAAACAAAAFdyaXRhYmxlAQGSAAAuAESSAAAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAA" +
           "VXNlcldyaXRhYmxlAQGTAAAuAESTAAAAAAH/////AQH/////AAAAABVgiQoCAAAAAAAJAAAAT3BlbkNv" +
           "dW50AQGUAAAuAESUAAAAAAX/////AQH/////AAAAAARhggoEAAAAAAAEAAAAT3BlbgEBmAAALwEAPC2Y" +
           "AAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAZkAAC4ARJkAAACWAQAAAAEA" +
           "KgEBEwAAAAQAAABNb2RlAAP/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAA" +
           "AAAPAAAAT3V0cHV0QXJndW1lbnRzAQGaAAAuAESaAAAAlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRs" +
           "ZQAH/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAEYYIKBAAAAAAABQAAAENsb3NlAQGb" +
           "AAAvAQA/LZsAAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBnAAALgBEnAAA" +
           "AJYBAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf//" +
           "//8AAAAABGGCCgQAAAAAAAQAAABSZWFkAQGdAAAvAQBBLZ0AAAABAf////8CAAAAF2CpCgIAAAAAAA4A" +
           "AABJbnB1dEFyZ3VtZW50cwEBngAALgBEngAAAJYCAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB///" +
           "//8AAAAAAAEAKgEBFQAAAAYAAABMZW5ndGgABv////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8A" +
           "AAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZ8AAC4ARJ8AAACWAQAAAAEAKgEBEwAAAAQA" +
           "AABEYXRhAA//////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhggoEAAAAAAAFAAAAV3Jp" +
           "dGUBAaAAAC8BAEQtoAAAAAEB/////wEAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGhAAAu" +
           "AEShAAAAlgIAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAqAQETAAAABAAAAERh" +
           "dGEAD/////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAABGGCCgQAAAAAAAsAAABHZXRQb3Np" +
           "dGlvbgEBogAALwEARi2iAAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAaMA" +
           "AC4ARKMAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgBAQAAAAEAAAAB" +
           "AAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGkAAAuAESkAAAAlgEAAAAB" +
           "ACoBARcAAAAIAAAAUG9zaXRpb24ACf////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGC" +
           "CgQAAAAAAAsAAABTZXRQb3NpdGlvbgEBpQAALwEASS2lAAAAAQH/////AQAAABdgqQoCAAAAAAAOAAAA" +
           "SW5wdXRBcmd1bWVudHMBAaYAAC4ARKYAAACWAgAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////" +
           "AAAAAAABACoBARcAAAAIAAAAUG9zaXRpb24ACf////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8A" +
           "AAAABGGCCgQAAAABAA4AAABDbG9zZUFuZFVwZGF0ZQEBpwAALwEBbwCnAAAAAQH/////AQAAABdgqQoC" +
           "AAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAagAAC4ARKgAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFu" +
           "ZGxlAAf/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABVgiQoCAAAAAQANAAAAQXNzZXRF" +
           "bmRwb2ludAEBqQAALgBEqQAAAAAM/////wEB/////wAAAAAXYIkKAgAAAAEAFAAAAFN1cHBvcnRlZFdv" +
           "VEJpbmRpbmdzAQEoAAAuAEQoAAAAAQDHXAEAAAABAAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAEACwAA" +
           "AENyZWF0ZUFzc2V0AQEaAAAvAQEaABoAAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3Vt" +
           "ZW50cwEBGwAALgBEGwAAAJYBAAAAAQAqAQEYAAAACQAAAEFzc2V0TmFtZQAM/////wAAAAAAAQAoAQEA" +
           "AAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBHAAALgBEHAAA" +
           "AJYBAAAAAQAqAQEWAAAABwAAAEFzc2V0SWQAEf////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8A" +
           "AAAABGGCCgQAAAABAAsAAABEZWxldGVBc3NldAEBHQAALwEBHQAdAAAAAQH/////AQAAABdgqQoCAAAA" +
           "AAAOAAAASW5wdXRBcmd1bWVudHMBAR4AAC4ARB4AAACWAQAAAAEAKgEBFgAAAAcAAABBc3NldElkABH/" +
           "////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhggoEAAAAAQAOAAAARGlzY292ZXJBc3Nl" +
           "dHMBASkAAC8BASkAKQAAAAEB/////wEAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBMAAA" +
           "LgBEMAAAAJYBAAAAAQAqAQEhAAAADgAAAEFzc2V0RW5kcG9pbnRzAAwBAAAAAQAAAAAAAAAAAQAoAQEA" +
           "AAABAAAAAQAAAAEB/////wAAAAAEYYIKBAAAAAEAFgAAAENyZWF0ZUFzc2V0Rm9yRW5kcG9pbnQBATEA" +
           "AC8BATEAMQAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEyAAAuAEQyAAAA" +
           "lgIAAAABACoBARgAAAAJAAAAQXNzZXROYW1lAAz/////AAAAAAABACoBARwAAAANAAAAQXNzZXRFbmRw" +
           "b2ludAAM/////wAAAAAAAQAoAQEAAAABAAAAAgAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1" +
           "dEFyZ3VtZW50cwEBqgAALgBEqgAAAJYBAAAAAQAqAQEWAAAABwAAAEFzc2V0SWQAEf////8AAAAAAAEA" +
           "KAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAABAA4AAABDb25uZWN0aW9uVGVzdAEBSwAALwEB" +
           "SwBLAAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAUwAAC4AREwAAACWAQAA" +
           "AAEAKgEBHAAAAA0AAABBc3NldEVuZHBvaW50AAz/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////" +
           "AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQFNAAAuAERNAAAAlgIAAAABACoBARYAAAAH" +
           "AAAAU3VjY2VzcwAB/////wAAAAAAAQAqAQEVAAAABgAAAFN0YXR1cwAM/////wAAAAAAAQAoAQEAAAAB" +
           "AAAAAgAAAAEB/////wAAAAAEYIAKAQAAAAEADQAAAENvbmZpZ3VyYXRpb24BAU4AAC8BAWkATgAAAP//" +
           "//8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        public PropertyState<string[]> SupportedWoTBindings
        {
            get => m_supportedWoTBindings;

            set
            {
                if (!Object.ReferenceEquals(m_supportedWoTBindings, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_supportedWoTBindings = value;
            }
        }

        public CreateAssetMethodState CreateAsset
        {
            get => m_createAssetMethod;

            set
            {
                if (!Object.ReferenceEquals(m_createAssetMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_createAssetMethod = value;
            }
        }

        public DeleteAssetMethodState DeleteAsset
        {
            get => m_deleteAssetMethod;

            set
            {
                if (!Object.ReferenceEquals(m_deleteAssetMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deleteAssetMethod = value;
            }
        }

        public DiscoverAssetsMethodState DiscoverAssets
        {
            get => m_discoverAssetsMethod;

            set
            {
                if (!Object.ReferenceEquals(m_discoverAssetsMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_discoverAssetsMethod = value;
            }
        }

        public CreateAssetForEndpointMethodState CreateAssetForEndpoint
        {
            get => m_createAssetForEndpointMethod;

            set
            {
                if (!Object.ReferenceEquals(m_createAssetForEndpointMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_createAssetForEndpointMethod = value;
            }
        }

        public ConnectionTestMethodState ConnectionTest
        {
            get => m_connectionTestMethod;

            set
            {
                if (!Object.ReferenceEquals(m_connectionTestMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_connectionTestMethod = value;
            }
        }

        public WoTAssetConfigurationState Configuration
        {
            get => m_configuration;

            set
            {
                if (!Object.ReferenceEquals(m_configuration, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_configuration = value;
            }
        }
        #endregion

        #region Overridden Methods
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_supportedWoTBindings != null)
            {
                children.Add(m_supportedWoTBindings);
            }

            if (m_createAssetMethod != null)
            {
                children.Add(m_createAssetMethod);
            }

            if (m_deleteAssetMethod != null)
            {
                children.Add(m_deleteAssetMethod);
            }

            if (m_discoverAssetsMethod != null)
            {
                children.Add(m_discoverAssetsMethod);
            }

            if (m_createAssetForEndpointMethod != null)
            {
                children.Add(m_createAssetForEndpointMethod);
            }

            if (m_connectionTestMethod != null)
            {
                children.Add(m_connectionTestMethod);
            }

            if (m_configuration != null)
            {
                children.Add(m_configuration);
            }

            base.GetChildren(context, children);
        }
            
        protected override void RemoveExplicitlyDefinedChild(BaseInstanceState child)
        {
            if (Object.ReferenceEquals(m_supportedWoTBindings, child))
            {
                m_supportedWoTBindings = null;
                return;
            }

            if (Object.ReferenceEquals(m_createAssetMethod, child))
            {
                m_createAssetMethod = null;
                return;
            }

            if (Object.ReferenceEquals(m_deleteAssetMethod, child))
            {
                m_deleteAssetMethod = null;
                return;
            }

            if (Object.ReferenceEquals(m_discoverAssetsMethod, child))
            {
                m_discoverAssetsMethod = null;
                return;
            }

            if (Object.ReferenceEquals(m_createAssetForEndpointMethod, child))
            {
                m_createAssetForEndpointMethod = null;
                return;
            }

            if (Object.ReferenceEquals(m_connectionTestMethod, child))
            {
                m_connectionTestMethod = null;
                return;
            }

            if (Object.ReferenceEquals(m_configuration, child))
            {
                m_configuration = null;
                return;
            }

            base.RemoveExplicitlyDefinedChild(child);
        }

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
                case Opc.Ua.WotCon.BrowseNames.SupportedWoTBindings:
                {
                    if (createOrReplace)
                    {
                        if (SupportedWoTBindings == null)
                        {
                            if (replacement == null)
                            {
                                SupportedWoTBindings = new PropertyState<string[]>(this);
                            }
                            else
                            {
                                SupportedWoTBindings = (PropertyState<string[]>)replacement;
                            }
                        }
                    }

                    instance = SupportedWoTBindings;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.CreateAsset:
                {
                    if (createOrReplace)
                    {
                        if (CreateAsset == null)
                        {
                            if (replacement == null)
                            {
                                CreateAsset = new CreateAssetMethodState(this);
                            }
                            else
                            {
                                CreateAsset = (CreateAssetMethodState)replacement;
                            }
                        }
                    }

                    instance = CreateAsset;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.DeleteAsset:
                {
                    if (createOrReplace)
                    {
                        if (DeleteAsset == null)
                        {
                            if (replacement == null)
                            {
                                DeleteAsset = new DeleteAssetMethodState(this);
                            }
                            else
                            {
                                DeleteAsset = (DeleteAssetMethodState)replacement;
                            }
                        }
                    }

                    instance = DeleteAsset;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.DiscoverAssets:
                {
                    if (createOrReplace)
                    {
                        if (DiscoverAssets == null)
                        {
                            if (replacement == null)
                            {
                                DiscoverAssets = new DiscoverAssetsMethodState(this);
                            }
                            else
                            {
                                DiscoverAssets = (DiscoverAssetsMethodState)replacement;
                            }
                        }
                    }

                    instance = DiscoverAssets;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.CreateAssetForEndpoint:
                {
                    if (createOrReplace)
                    {
                        if (CreateAssetForEndpoint == null)
                        {
                            if (replacement == null)
                            {
                                CreateAssetForEndpoint = new CreateAssetForEndpointMethodState(this);
                            }
                            else
                            {
                                CreateAssetForEndpoint = (CreateAssetForEndpointMethodState)replacement;
                            }
                        }
                    }

                    instance = CreateAssetForEndpoint;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.ConnectionTest:
                {
                    if (createOrReplace)
                    {
                        if (ConnectionTest == null)
                        {
                            if (replacement == null)
                            {
                                ConnectionTest = new ConnectionTestMethodState(this);
                            }
                            else
                            {
                                ConnectionTest = (ConnectionTestMethodState)replacement;
                            }
                        }
                    }

                    instance = ConnectionTest;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.Configuration:
                {
                    if (createOrReplace)
                    {
                        if (Configuration == null)
                        {
                            if (replacement == null)
                            {
                                Configuration = new WoTAssetConfigurationState(this);
                            }
                            else
                            {
                                Configuration = (WoTAssetConfigurationState)replacement;
                            }
                        }
                    }

                    instance = Configuration;
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
        private PropertyState<string[]> m_supportedWoTBindings;
        private CreateAssetMethodState m_createAssetMethod;
        private DeleteAssetMethodState m_deleteAssetMethod;
        private DiscoverAssetsMethodState m_discoverAssetsMethod;
        private CreateAssetForEndpointMethodState m_createAssetForEndpointMethod;
        private ConnectionTestMethodState m_connectionTestMethod;
        private WoTAssetConfigurationState m_configuration;
        #endregion
    }
    #endif
    #endregion

    #region CreateAssetMethodState Class
    #if (!OPCUA_EXCLUDE_CreateAssetMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class CreateAssetMethodState : MethodState
    {
        #region Constructors
        public CreateAssetMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new CreateAssetMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABUA" +
           "AABDcmVhdGVBc3NldE1ldGhvZFR5cGUBAVoAAC8BAVoAWgAAAAEB/////wIAAAAXYKkKAgAAAAAADgAA" +
           "AElucHV0QXJndW1lbnRzAQFbAAAuAERbAAAAlgEAAAABACoBARgAAAAJAAAAQXNzZXROYW1lAAz/////" +
           "AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRz" +
           "AQFcAAAuAERcAAAAlgEAAAABACoBARYAAAAHAAAAQXNzZXRJZAAR/////wAAAAAAAQAoAQEAAAABAAAA" +
           "AQAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public CreateAssetMethodStateMethodCallHandler OnCall;

        public CreateAssetMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            string assetName = (string)_inputArguments[0];

            NodeId assetId = (NodeId)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetName,
                    ref assetId);
            }

            _outputArguments[0] = assetId;

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            CreateAssetMethodStateResult _result = null;

            string assetName = (string)_inputArguments[0];

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    assetName,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            _outputArguments[0] = _result.AssetId;

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult CreateAssetMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetName,
        ref NodeId assetId);

    /// <exclude />
    public partial class CreateAssetMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
        public NodeId AssetId { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<CreateAssetMethodStateResult> CreateAssetMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetName,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region DeleteAssetMethodState Class
    #if (!OPCUA_EXCLUDE_DeleteAssetMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class DeleteAssetMethodState : MethodState
    {
        #region Constructors
        public DeleteAssetMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new DeleteAssetMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABUA" +
           "AABEZWxldGVBc3NldE1ldGhvZFR5cGUBAV0AAC8BAV0AXQAAAAEB/////wEAAAAXYKkKAgAAAAAADgAA" +
           "AElucHV0QXJndW1lbnRzAQFeAAAuAEReAAAAlgEAAAABACoBARYAAAAHAAAAQXNzZXRJZAAR/////wAA" +
           "AAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public DeleteAssetMethodStateMethodCallHandler OnCall;

        public DeleteAssetMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            NodeId assetId = (NodeId)_inputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetId);
            }

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            DeleteAssetMethodStateResult _result = null;

            NodeId assetId = (NodeId)_inputArguments[0];

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    assetId,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult DeleteAssetMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        NodeId assetId);

    /// <exclude />
    public partial class DeleteAssetMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<DeleteAssetMethodStateResult> DeleteAssetMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        NodeId assetId,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region DiscoverAssetsMethodState Class
    #if (!OPCUA_EXCLUDE_DiscoverAssetsMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class DiscoverAssetsMethodState : MethodState
    {
        #region Constructors
        public DiscoverAssetsMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new DiscoverAssetsMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABgA" +
           "AABEaXNjb3ZlckFzc2V0c01ldGhvZFR5cGUBAV8AAC8BAV8AXwAAAAEB/////wEAAAAXYKkKAgAAAAAA" +
           "DwAAAE91dHB1dEFyZ3VtZW50cwEBYAAALgBEYAAAAJYBAAAAAQAqAQEhAAAADgAAAEFzc2V0RW5kcG9p" +
           "bnRzAAwBAAAAAQAAAAAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public DiscoverAssetsMethodStateMethodCallHandler OnCall;

        public DiscoverAssetsMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            string[] assetEndpoints = (string[])_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref assetEndpoints);
            }

            _outputArguments[0] = assetEndpoints;

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            DiscoverAssetsMethodStateResult _result = null;

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            _outputArguments[0] = _result.AssetEndpoints;

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult DiscoverAssetsMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref string[] assetEndpoints);

    /// <exclude />
    public partial class DiscoverAssetsMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
        public string[] AssetEndpoints { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<DiscoverAssetsMethodStateResult> DiscoverAssetsMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region CreateAssetForEndpointMethodState Class
    #if (!OPCUA_EXCLUDE_CreateAssetForEndpointMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class CreateAssetForEndpointMethodState : MethodState
    {
        #region Constructors
        public CreateAssetForEndpointMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new CreateAssetForEndpointMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABACAA" +
           "AABDcmVhdGVBc3NldEZvckVuZHBvaW50TWV0aG9kVHlwZQEBYQAALwEBYQBhAAAAAQH/////AgAAABdg" +
           "qQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAWIAAC4ARGIAAACWAgAAAAEAKgEBGAAAAAkAAABBc3Nl" +
           "dE5hbWUADP////8AAAAAAAEAKgEBHAAAAA0AAABBc3NldEVuZHBvaW50AAz/////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGsAAAuAESsAAAA" +
           "lgEAAAABACoBARYAAAAHAAAAQXNzZXRJZAAR/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAA" +
           "AAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public CreateAssetForEndpointMethodStateMethodCallHandler OnCall;

        public CreateAssetForEndpointMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            string assetName = (string)_inputArguments[0];
            string assetEndpoint = (string)_inputArguments[1];

            NodeId assetId = (NodeId)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetName,
                    assetEndpoint,
                    ref assetId);
            }

            _outputArguments[0] = assetId;

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            CreateAssetForEndpointMethodStateResult _result = null;

            string assetName = (string)_inputArguments[0];
            string assetEndpoint = (string)_inputArguments[1];

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    assetName,
                    assetEndpoint,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            _outputArguments[0] = _result.AssetId;

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult CreateAssetForEndpointMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetName,
        string assetEndpoint,
        ref NodeId assetId);

    /// <exclude />
    public partial class CreateAssetForEndpointMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
        public NodeId AssetId { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<CreateAssetForEndpointMethodStateResult> CreateAssetForEndpointMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetName,
        string assetEndpoint,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region ConnectionTestMethodState Class
    #if (!OPCUA_EXCLUDE_ConnectionTestMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class ConnectionTestMethodState : MethodState
    {
        #region Constructors
        public ConnectionTestMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new ConnectionTestMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABgA" +
           "AABDb25uZWN0aW9uVGVzdE1ldGhvZFR5cGUBAWYAAC8BAWYAZgAAAAEB/////wIAAAAXYKkKAgAAAAAA" +
           "DgAAAElucHV0QXJndW1lbnRzAQFnAAAuAERnAAAAlgEAAAABACoBARwAAAANAAAAQXNzZXRFbmRwb2lu" +
           "dAAM/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFy" +
           "Z3VtZW50cwEBaAAALgBEaAAAAJYCAAAAAQAqAQEWAAAABwAAAFN1Y2Nlc3MAAf////8AAAAAAAEAKgEB" +
           "FQAAAAYAAABTdGF0dXMADP////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public ConnectionTestMethodStateMethodCallHandler OnCall;

        public ConnectionTestMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            string assetEndpoint = (string)_inputArguments[0];

            bool success = (bool)_outputArguments[0];
            string status = (string)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    assetEndpoint,
                    ref success,
                    ref status);
            }

            _outputArguments[0] = success;
            _outputArguments[1] = status;

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            ConnectionTestMethodStateResult _result = null;

            string assetEndpoint = (string)_inputArguments[0];

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    assetEndpoint,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            _outputArguments[0] = _result.Success;
            _outputArguments[1] = _result.Status;

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult ConnectionTestMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetEndpoint,
        ref bool success,
        ref string status);

    /// <exclude />
    public partial class ConnectionTestMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
        public bool Success { get; set; }
        public string Status { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<ConnectionTestMethodStateResult> ConnectionTestMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string assetEndpoint,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region WoTAssetConfigurationState Class
    #if (!OPCUA_EXCLUDE_WoTAssetConfigurationState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class WoTAssetConfigurationState : BaseInterfaceState
    {
        #region Constructors
        public WoTAssetConfigurationState(NodeState parent) : base(parent)
        {
        }

        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.WotCon.ObjectTypes.WoTAssetConfigurationType, Opc.Ua.WotCon.Namespaces.WotCon, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);

            if (License != null)
            {
                License.Initialize(context, License_InitializationString);
            }
        }

        #region Initialization String
        private const string License_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////FWCJCgIAAAABAAcA" +
           "AABMaWNlbnNlAQFtAAAuAERtAAAAAAz/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABACEA" +
           "AABXb1RBc3NldENvbmZpZ3VyYXRpb25UeXBlSW5zdGFuY2UBAWkAAQFpAGkAAAD/////AgAAABVgyQoC" +
           "AAAAKQAAAFdvVENvbmZpZ3VyYXRpb25QYXJhbWV0ZXJOYW1lX1BsYWNlaG9sZGVyAQAfAAAAPFdvVENv" +
           "bmZpZ3VyYXRpb25QYXJhbWV0ZXJOYW1lPgEBbAAALgBEbAAAAAAY/////wEB/////wAAAAAVYIkKAgAA" +
           "AAEABwAAAExpY2Vuc2UBAW0AAC4ARG0AAAAADP////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        public PropertyState<string> License
        {
            get => m_license;

            set
            {
                if (!Object.ReferenceEquals(m_license, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_license = value;
            }
        }
        #endregion

        #region Overridden Methods
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_license != null)
            {
                children.Add(m_license);
            }

            base.GetChildren(context, children);
        }
            
        protected override void RemoveExplicitlyDefinedChild(BaseInstanceState child)
        {
            if (Object.ReferenceEquals(m_license, child))
            {
                m_license = null;
                return;
            }

            base.RemoveExplicitlyDefinedChild(child);
        }

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
                case Opc.Ua.WotCon.BrowseNames.License:
                {
                    if (createOrReplace)
                    {
                        if (License == null)
                        {
                            if (replacement == null)
                            {
                                License = new PropertyState<string>(this);
                            }
                            else
                            {
                                License = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = License;
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
        private PropertyState<string> m_license;
        #endregion
    }
    #endif
    #endregion

    #region IWoTAssetState Class
    #if (!OPCUA_EXCLUDE_IWoTAssetState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class IWoTAssetState : BaseInterfaceState
    {
        #region Constructors
        public IWoTAssetState(NodeState parent) : base(parent)
        {
        }

        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.WotCon.ObjectTypes.IWoTAssetType, Opc.Ua.WotCon.Namespaces.WotCon, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);

            if (AssetEndpoint != null)
            {
                AssetEndpoint.Initialize(context, AssetEndpoint_InitializationString);
            }
        }

        #region Initialization String
        private const string AssetEndpoint_InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////FWCJCgIAAAABAA0A" +
           "AABBc3NldEVuZHBvaW50AQF6AAAuAER6AAAAAAz/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABABUA" +
           "AABJV29UQXNzZXRUeXBlSW5zdGFuY2UBASoAAQEqACoAAAD/////AwAAAARggAoBAAAAAQAHAAAAV29U" +
           "RmlsZQEBKwAALwEBbgArAAAA/////wsAAAAVYIkKAgAAAAAABAAAAFNpemUBASwAAC4ARCwAAAAACf//" +
           "//8BAf////8AAAAAFWCJCgIAAAAAAAgAAABXcml0YWJsZQEBLQAALgBELQAAAAAB/////wEB/////wAA" +
           "AAAVYIkKAgAAAAAADAAAAFVzZXJXcml0YWJsZQEBLgAALgBELgAAAAAB/////wEB/////wAAAAAVYIkK" +
           "AgAAAAAACQAAAE9wZW5Db3VudAEBLwAALgBELwAAAAAF/////wEB/////wAAAAAEYYIKBAAAAAAABAAA" +
           "AE9wZW4BATMAAC8BADwtMwAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQE0" +
           "AAAuAEQ0AAAAlgEAAAABACoBARMAAAAEAAAATW9kZQAD/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB" +
           "/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBNQAALgBENQAAAJYBAAAAAQAqAQEZ" +
           "AAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQA" +
           "AAAAAAUAAABDbG9zZQEBNgAALwEAPy02AAAAAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1" +
           "bWVudHMBATcAAC4ARDcAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgB" +
           "AQAAAAEAAAABAAAAAQH/////AAAAAARhggoEAAAAAAAEAAAAUmVhZAEBOAAALwEAQS04AAAAAQH/////" +
           "AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBATkAAC4ARDkAAACWAgAAAAEAKgEBGQAAAAoA" +
           "AABGaWxlSGFuZGxlAAf/////AAAAAAABACoBARUAAAAGAAAATGVuZ3RoAAb/////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQE6AAAuAEQ6AAAA" +
           "lgEAAAABACoBARMAAAAEAAAARGF0YQAP/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAE" +
           "YYIKBAAAAAAABQAAAFdyaXRlAQE7AAAvAQBELTsAAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1" +
           "dEFyZ3VtZW50cwEBPAAALgBEPAAAAJYCAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAA" +
           "AAEAKgEBEwAAAAQAAABEYXRhAA//////AAAAAAABACgBAQAAAAEAAAACAAAAAQH/////AAAAAARhggoE" +
           "AAAAAAALAAAAR2V0UG9zaXRpb24BAT0AAC8BAEYtPQAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElu" +
           "cHV0QXJndW1lbnRzAQE+AAAuAEQ+AAAAlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAA" +
           "AAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEB" +
           "PwAALgBEPwAAAJYBAAAAAQAqAQEXAAAACAAAAFBvc2l0aW9uAAn/////AAAAAAABACgBAQAAAAEAAAAB" +
           "AAAAAQH/////AAAAAARhggoEAAAAAAALAAAAU2V0UG9zaXRpb24BAUAAAC8BAEktQAAAAAEB/////wEA" +
           "AAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQFBAAAuAERBAAAAlgIAAAABACoBARkAAAAKAAAA" +
           "RmlsZUhhbmRsZQAH/////wAAAAAAAQAqAQEXAAAACAAAAFBvc2l0aW9uAAn/////AAAAAAABACgBAQAA" +
           "AAEAAAACAAAAAQH/////AAAAAARhggoEAAAAAQAOAAAAQ2xvc2VBbmRVcGRhdGUBAWoAAC8BAW8AagAA" +
           "AAEB/////wEAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQFrAAAuAERrAAAAlgEAAAABACoB" +
           "ARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAVYIkK" +
           "AgAAAAEADQAAAEFzc2V0RW5kcG9pbnQBAXoAAC4ARHoAAAAADP////8BAf////8AAAAAFWDJCgIAAAAb" +
           "AAAAV29UUHJvcGVydHlOYW1lX1BsYWNlaG9sZGVyAQARAAAAPFdvVFByb3BlcnR5TmFtZT4BAUIAAQGO" +
           "AAA/QgAAAAAY/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        public WoTAssetFileState WoTFile
        {
            get => m_woTFile;

            set
            {
                if (!Object.ReferenceEquals(m_woTFile, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_woTFile = value;
            }
        }

        public PropertyState<string> AssetEndpoint
        {
            get => m_assetEndpoint;

            set
            {
                if (!Object.ReferenceEquals(m_assetEndpoint, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_assetEndpoint = value;
            }
        }
        #endregion

        #region Overridden Methods
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_woTFile != null)
            {
                children.Add(m_woTFile);
            }

            if (m_assetEndpoint != null)
            {
                children.Add(m_assetEndpoint);
            }

            base.GetChildren(context, children);
        }
            
        protected override void RemoveExplicitlyDefinedChild(BaseInstanceState child)
        {
            if (Object.ReferenceEquals(m_woTFile, child))
            {
                m_woTFile = null;
                return;
            }

            if (Object.ReferenceEquals(m_assetEndpoint, child))
            {
                m_assetEndpoint = null;
                return;
            }

            base.RemoveExplicitlyDefinedChild(child);
        }

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
                case Opc.Ua.WotCon.BrowseNames.WoTFile:
                {
                    if (createOrReplace)
                    {
                        if (WoTFile == null)
                        {
                            if (replacement == null)
                            {
                                WoTFile = new WoTAssetFileState(this);
                            }
                            else
                            {
                                WoTFile = (WoTAssetFileState)replacement;
                            }
                        }
                    }

                    instance = WoTFile;
                    break;
                }

                case Opc.Ua.WotCon.BrowseNames.AssetEndpoint:
                {
                    if (createOrReplace)
                    {
                        if (AssetEndpoint == null)
                        {
                            if (replacement == null)
                            {
                                AssetEndpoint = new PropertyState<string>(this);
                            }
                            else
                            {
                                AssetEndpoint = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = AssetEndpoint;
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
        private WoTAssetFileState m_woTFile;
        private PropertyState<string> m_assetEndpoint;
        #endregion
    }
    #endif
    #endregion

    #region CloseAndUpdateMethodState Class
    #if (!OPCUA_EXCLUDE_CloseAndUpdateMethodState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class CloseAndUpdateMethodState : MethodState
    {
        #region Constructors
        public CloseAndUpdateMethodState(NodeState parent) : base(parent)
        {
        }

        public new static NodeState Construct(NodeState parent)
        {
            return new CloseAndUpdateMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGGCCgQAAAABABgA" +
           "AABDbG9zZUFuZFVwZGF0ZU1ldGhvZFR5cGUBAXsAAC8BAXsAewAAAAEB/////wEAAAAXYKkKAgAAAAAA" +
           "DgAAAElucHV0QXJndW1lbnRzAQGPAAAuAESPAAAAlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH" +
           "/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        public CloseAndUpdateMethodStateMethodCallHandler OnCall;

        public CloseAndUpdateMethodStateMethodAsyncCallHandler OnCallAsync;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        protected override ServiceResult Call(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            ServiceResult _result = null;

            uint fileHandle = (uint)_inputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    fileHandle);
            }

            return _result;
        }

        #if (OPCUA_INCLUDE_ASYNC)
        protected override async ValueTask<ServiceResult> CallAsync(
            ISystemContext _context,
            NodeId _objectId,
            IList<object> _inputArguments,
            IList<object> _outputArguments,
            CancellationToken cancellationToken = default)
        {
            if (OnCall == null && OnCallAsync == null)
            {
                return await base.CallAsync(_context, _objectId, _inputArguments, _outputArguments, cancellationToken).ConfigureAwait(false);
            }

            CloseAndUpdateMethodStateResult _result = null;

            uint fileHandle = (uint)_inputArguments[0];

            if (OnCallAsync != null)
            {
                _result = await OnCallAsync(
                    _context,
                    this,
                    _objectId,
                    fileHandle,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (OnCall != null)
            {
                return Call(_context, _objectId, _inputArguments, _outputArguments);
            }

            return _result.ServiceResult;
        }
        #endif

        #endregion

        #region Private Fields
        #endregion
    }

    /// <exclude />
    public delegate ServiceResult CloseAndUpdateMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        uint fileHandle);

    /// <exclude />
    public partial class CloseAndUpdateMethodStateResult
    {
        public ServiceResult ServiceResult { get; set; }
    }

    /// <exclude />
    public delegate ValueTask<CloseAndUpdateMethodStateResult> CloseAndUpdateMethodStateMethodAsyncCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        uint fileHandle,
        CancellationToken cancellationToken);
    #endif
    #endregion

    #region WoTAssetFileState Class
    #if (!OPCUA_EXCLUDE_WoTAssetFileState)
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public partial class WoTAssetFileState : FileState
    {
        #region Constructors
        public WoTAssetFileState(NodeState parent) : base(parent)
        {
        }

        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.WotCon.ObjectTypes.WoTAssetFileType, Opc.Ua.WotCon.Namespaces.WotCon, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvV29ULUNvbi//////BGCAAgEAAAABABgA" +
           "AABXb1RBc3NldEZpbGVUeXBlSW5zdGFuY2UBAW4AAQFuAG4AAAD/////CwAAABVgiQoCAAAAAAAEAAAA" +
           "U2l6ZQIBAFxCDwAALgBEXEIPAAAJ/////wEB/////wAAAAAVYIkKAgAAAAAACAAAAFdyaXRhYmxlAgEA" +
           "XUIPAAAuAERdQg8AAAH/////AQH/////AAAAABVgiQoCAAAAAAAMAAAAVXNlcldyaXRhYmxlAgEAXkIP" +
           "AAAuAEReQg8AAAH/////AQH/////AAAAABVgiQoCAAAAAAAJAAAAT3BlbkNvdW50AgEAX0IPAAAuAERf" +
           "Qg8AAAX/////AQH/////AAAAAARhggoEAAAAAAAEAAAAT3BlbgIBAGNCDwAALwEAPC1jQg8AAQH/////" +
           "AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMCAQBkQg8AAC4ARGRCDwCWAQAAAAEAKgEBEwAA" +
           "AAQAAABNb2RlAAP/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAA" +
           "T3V0cHV0QXJndW1lbnRzAgEAZUIPAAAuAERlQg8AlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH" +
           "/////wAAAAAAAQAoAQEAAAABAAAAAQAAAAEB/////wAAAAAEYYIKBAAAAAAABQAAAENsb3NlAgEAZkIP" +
           "AAAvAQA/LWZCDwABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwIBAGdCDwAALgBE" +
           "Z0IPAJYBAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAEAAAAB" +
           "Af////8AAAAABGGCCgQAAAAAAAQAAABSZWFkAgEAaEIPAAAvAQBBLWhCDwABAf////8CAAAAF2CpCgIA" +
           "AAAAAA4AAABJbnB1dEFyZ3VtZW50cwIBAGlCDwAALgBEaUIPAJYCAAAAAQAqAQEZAAAACgAAAEZpbGVI" +
           "YW5kbGUAB/////8AAAAAAAEAKgEBFQAAAAYAAABMZW5ndGgABv////8AAAAAAAEAKAEBAAAAAQAAAAIA" +
           "AAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMCAQBqQg8AAC4ARGpCDwCWAQAA" +
           "AAEAKgEBEwAAAAQAAABEYXRhAA//////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAAARhggoE" +
           "AAAAAAAFAAAAV3JpdGUCAQBrQg8AAC8BAEQta0IPAAEB/////wEAAAAXYKkKAgAAAAAADgAAAElucHV0" +
           "QXJndW1lbnRzAgEAbEIPAAAuAERsQg8AlgIAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAA" +
           "AAAAAQAqAQETAAAABAAAAERhdGEAD/////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAABGGC" +
           "CgQAAAAAAAsAAABHZXRQb3NpdGlvbgIBAG1CDwAALwEARi1tQg8AAQH/////AgAAABdgqQoCAAAAAAAO" +
           "AAAASW5wdXRBcmd1bWVudHMCAQBuQg8AAC4ARG5CDwCWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxl" +
           "AAf/////AAAAAAABACgBAQAAAAEAAAABAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJn" +
           "dW1lbnRzAgEAb0IPAAAuAERvQg8AlgEAAAABACoBARcAAAAIAAAAUG9zaXRpb24ACf////8AAAAAAAEA" +
           "KAEBAAAAAQAAAAEAAAABAf////8AAAAABGGCCgQAAAAAAAsAAABTZXRQb3NpdGlvbgIBAHBCDwAALwEA" +
           "SS1wQg8AAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMCAQBxQg8AAC4ARHFCDwCW" +
           "AgAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACoBARcAAAAIAAAAUG9zaXRpb24A" +
           "Cf////8AAAAAAAEAKAEBAAAAAQAAAAIAAAABAf////8AAAAABGGCCgQAAAABAA4AAABDbG9zZUFuZFVw" +
           "ZGF0ZQEBbwAALwEBbwBvAAAAAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAXAA" +
           "AC4ARHAAAACWAQAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgBAQAAAAEAAAAB" +
           "AAAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        public CloseAndUpdateMethodState CloseAndUpdate
        {
            get => m_closeAndUpdateMethod;

            set
            {
                if (!Object.ReferenceEquals(m_closeAndUpdateMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_closeAndUpdateMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_closeAndUpdateMethod != null)
            {
                children.Add(m_closeAndUpdateMethod);
            }

            base.GetChildren(context, children);
        }
            
        protected override void RemoveExplicitlyDefinedChild(BaseInstanceState child)
        {
            if (Object.ReferenceEquals(m_closeAndUpdateMethod, child))
            {
                m_closeAndUpdateMethod = null;
                return;
            }

            base.RemoveExplicitlyDefinedChild(child);
        }

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
                case Opc.Ua.WotCon.BrowseNames.CloseAndUpdate:
                {
                    if (createOrReplace)
                    {
                        if (CloseAndUpdate == null)
                        {
                            if (replacement == null)
                            {
                                CloseAndUpdate = new CloseAndUpdateMethodState(this);
                            }
                            else
                            {
                                CloseAndUpdate = (CloseAndUpdateMethodState)replacement;
                            }
                        }
                    }

                    instance = CloseAndUpdate;
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
        private CloseAndUpdateMethodState m_closeAndUpdateMethod;
        #endregion
    }
    #endif
    #endregion
}