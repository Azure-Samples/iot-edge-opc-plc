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

namespace Opc.Ua.DI
{
    #region TopologyElementTypeState Class
    #if (!OPCUA_EXCLUDE_TopologyElementTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TopologyElementTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public TopologyElementTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.TopologyElementType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (ParameterSet != null)
            {
                ParameterSet.Initialize(context, ParameterSet_InitializationString);
            }

            if (MethodSet != null)
            {
                MethodSet.Initialize(context, MethodSet_InitializationString);
            }

            if (Identification != null)
            {
                Identification.Initialize(context, Identification_InitializationString);
            }

            if (Lock != null)
            {
                Lock.Initialize(context, Lock_InitializationString);
            }
        }

        #region Initialization String
        private const string ParameterSet_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAMAAAAUGFy" +
           "YW1ldGVyU2V0AQGKEwMAAAAAFwAAAEZsYXQgbGlzdCBvZiBQYXJhbWV0ZXJzAC8AOooTAAD/////AQAA" +
           "ADVgyQoCAAAAEwAAAFBhcmFtZXRlcklkZW50aWZpZXIBABUAAAA8UGFyYW1ldGVySWRlbnRpZmllcj4B" +
           "AYEXAwAAAAAyAAAAQSBwYXJhbWV0ZXIgd2hpY2ggYmVsb25ncyB0byB0aGUgdG9wb2xvZ3kgZWxlbWVu" +
           "dC4ALwA/gRcAAAAY/////wEB/////wAAAAA=";

        private const string MethodSet_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAJAAAATWV0" +
           "aG9kU2V0AQGLEwMAAAAAFAAAAEZsYXQgbGlzdCBvZiBNZXRob2RzAC8AOosTAAD/////AAAAAA==";

        private const string Identification_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAOAAAASWRl" +
           "bnRpZmljYXRpb24BAX4XAwAAAABGAAAAVXNlZCB0byBvcmdhbml6ZSBwYXJhbWV0ZXJzIGZvciBpZGVu" +
           "dGlmaWNhdGlvbiBvZiB0aGlzIFRvcG9sb2d5RWxlbWVudAAvAQHtA34XAAD/////AAAAAA==";

        private const string Lock_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAEAAAATG9j" +
           "awEBERgDAAAAACIAAABVc2VkIHRvIGxvY2sgdGhlIHRvcG9sb2d5IGVsZW1lbnQuAC8BAfQYERgAAP//" +
           "//8IAAAAFWCJCgIAAAABAAYAAABMb2NrZWQBAUQZAC4AREQZAAAAAf////8BAf////8AAAAAFWCJCgIA" +
           "AAABAA0AAABMb2NraW5nQ2xpZW50AQETGAAuAEQTGAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAL" +
           "AAAATG9ja2luZ1VzZXIBARQYAC4ARBQYAAAADP////8BAf////8AAAAAFWCJCgIAAAABABEAAABSZW1h" +
           "aW5pbmdMb2NrVGltZQEBFRgALgBEFRgAAAEAIgH/////AQH/////AAAAAARhggoEAAAAAQAIAAAASW5p" +
           "dExvY2sBARYYAC8BAfkYFhgAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEX" +
           "GAAuAEQXGAAAlgEAAAABACoBARYAAAAHAAAAQ29udGV4dAAM/////wAAAAAAAQAoAQEAAAABAAAAAAAA" +
           "AAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBGBgALgBEGBgAAJYBAAAAAQAq" +
           "AQEdAAAADgAAAEluaXRMb2NrU3RhdHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAA" +
           "AARhggoEAAAAAQAJAAAAUmVuZXdMb2NrAQEZGAAvAQH8GBkYAAABAf////8BAAAAF2CpCgIAAAAAAA8A" +
           "AABPdXRwdXRBcmd1bWVudHMBARoYAC4ARBoYAACWAQAAAAEAKgEBHgAAAA8AAABSZW5ld0xvY2tTdGF0" +
           "dXMABv////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAABGGCCgQAAAABAAgAAABFeGl0TG9j" +
           "awEBGxgALwEB/hgbGAAAAQH/////AQAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEcGAAu" +
           "AEQcGAAAlgEAAAABACoBAR0AAAAOAAAARXhpdExvY2tTdGF0dXMABv////8AAAAAAAEAKAEBAAAAAQAA" +
           "AAAAAAABAf////8AAAAABGGCCgQAAAABAAkAAABCcmVha0xvY2sBAR0YAC8BAQAZHRgAAAEB/////wEA" +
           "AAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBHhgALgBEHhgAAJYBAAAAAQAqAQEeAAAADwAA" +
           "AEJyZWFrTG9ja1N0YXR1cwAG/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAAVG9w" +
           "b2xvZ3lFbGVtZW50VHlwZUluc3RhbmNlAQHpAwEB6QPpAwAA/////wUAAAAkYIAKAQAAAAEADAAAAFBh" +
           "cmFtZXRlclNldAEBihMDAAAAABcAAABGbGF0IGxpc3Qgb2YgUGFyYW1ldGVycwAvADqKEwAA/////wEA" +
           "AAA1YMkKAgAAABMAAABQYXJhbWV0ZXJJZGVudGlmaWVyAQAVAAAAPFBhcmFtZXRlcklkZW50aWZpZXI+" +
           "AQGBFwMAAAAAMgAAAEEgcGFyYW1ldGVyIHdoaWNoIGJlbG9uZ3MgdG8gdGhlIHRvcG9sb2d5IGVsZW1l" +
           "bnQuAC8AP4EXAAAAGP////8BAf////8AAAAAJGCACgEAAAABAAkAAABNZXRob2RTZXQBAYsTAwAAAAAU" +
           "AAAARmxhdCBsaXN0IG9mIE1ldGhvZHMALwA6ixMAAP////8AAAAAJGDACgEAAAAPAAAAR3JvdXBJZGVu" +
           "dGlmaWVyAQARAAAAPEdyb3VwSWRlbnRpZmllcj4BAacZAwAAAABRAAAAQW4gYXBwbGljYXRpb24gc3Bl" +
           "Y2lmaWMgZnVuY3Rpb25hbCBncm91cCB1c2VkIHRvIG9yZ2FuaXplIHBhcmFtZXRlcnMgYW5kIG1ldGhv" +
           "ZHMuAC8BAe0DpxkAAP////8AAAAAJGCACgEAAAABAA4AAABJZGVudGlmaWNhdGlvbgEBfhcDAAAAAEYA" +
           "AABVc2VkIHRvIG9yZ2FuaXplIHBhcmFtZXRlcnMgZm9yIGlkZW50aWZpY2F0aW9uIG9mIHRoaXMgVG9w" +
           "b2xvZ3lFbGVtZW50AC8BAe0DfhcAAP////8AAAAAJGCACgEAAAABAAQAAABMb2NrAQERGAMAAAAAIgAA" +
           "AFVzZWQgdG8gbG9jayB0aGUgdG9wb2xvZ3kgZWxlbWVudC4ALwEB9BgRGAAA/////wgAAAAVYIkKAgAA" +
           "AAEABgAAAExvY2tlZAEBRBkALgBERBkAAAAB/////wEB/////wAAAAAVYIkKAgAAAAEADQAAAExvY2tp" +
           "bmdDbGllbnQBARMYAC4ARBMYAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABMb2NraW5nVXNl" +
           "cgEBFBgALgBEFBgAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEQAAAFJlbWFpbmluZ0xvY2tUaW1l" +
           "AQEVGAAuAEQVGAAAAQAiAf////8BAf////8AAAAABGGCCgQAAAABAAgAAABJbml0TG9jawEBFhgALwEB" +
           "+RgWGAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBARcYAC4ARBcYAACWAQAA" +
           "AAEAKgEBFgAAAAcAAABDb250ZXh0AAz/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABdg" +
           "qQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEYGAAuAEQYGAAAlgEAAAABACoBAR0AAAAOAAAASW5p" +
           "dExvY2tTdGF0dXMABv////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAABGGCCgQAAAABAAkA" +
           "AABSZW5ld0xvY2sBARkYAC8BAfwYGRgAAAEB/////wEAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3Vt" +
           "ZW50cwEBGhgALgBEGhgAAJYBAAAAAQAqAQEeAAAADwAAAFJlbmV3TG9ja1N0YXR1cwAG/////wAAAAAA" +
           "AQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAEACAAAAEV4aXRMb2NrAQEbGAAvAQH+GBsY" +
           "AAABAf////8BAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBARwYAC4ARBwYAACWAQAAAAEA" +
           "KgEBHQAAAA4AAABFeGl0TG9ja1N0YXR1cwAG/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAA" +
           "AAAEYYIKBAAAAAEACQAAAEJyZWFrTG9jawEBHRgALwEBABkdGAAAAQH/////AQAAABdgqQoCAAAAAAAP" +
           "AAAAT3V0cHV0QXJndW1lbnRzAQEeGAAuAEQeGAAAlgEAAAABACoBAR4AAAAPAAAAQnJlYWtMb2NrU3Rh" +
           "dHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseObjectState ParameterSet
        {
            get
            {
                return m_parameterSet;
            }

            set
            {
                if (!Object.ReferenceEquals(m_parameterSet, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_parameterSet = value;
            }
        }

        /// <remarks />
        public BaseObjectState MethodSet
        {
            get
            {
                return m_methodSet;
            }

            set
            {
                if (!Object.ReferenceEquals(m_methodSet, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_methodSet = value;
            }
        }

        /// <remarks />
        public FunctionalGroupTypeState Identification
        {
            get
            {
                return m_identification;
            }

            set
            {
                if (!Object.ReferenceEquals(m_identification, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_identification = value;
            }
        }

        /// <remarks />
        public LockingServicesTypeState Lock
        {
            get
            {
                return m_lock;
            }

            set
            {
                if (!Object.ReferenceEquals(m_lock, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_lock = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_parameterSet != null)
            {
                children.Add(m_parameterSet);
            }

            if (m_methodSet != null)
            {
                children.Add(m_methodSet);
            }

            if (m_identification != null)
            {
                children.Add(m_identification);
            }

            if (m_lock != null)
            {
                children.Add(m_lock);
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
                case Opc.Ua.DI.BrowseNames.ParameterSet:
                {
                    if (createOrReplace)
                    {
                        if (ParameterSet == null)
                        {
                            if (replacement == null)
                            {
                                ParameterSet = new BaseObjectState(this);
                            }
                            else
                            {
                                ParameterSet = (BaseObjectState)replacement;
                            }
                        }
                    }

                    instance = ParameterSet;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.MethodSet:
                {
                    if (createOrReplace)
                    {
                        if (MethodSet == null)
                        {
                            if (replacement == null)
                            {
                                MethodSet = new BaseObjectState(this);
                            }
                            else
                            {
                                MethodSet = (BaseObjectState)replacement;
                            }
                        }
                    }

                    instance = MethodSet;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Identification:
                {
                    if (createOrReplace)
                    {
                        if (Identification == null)
                        {
                            if (replacement == null)
                            {
                                Identification = new FunctionalGroupTypeState(this);
                            }
                            else
                            {
                                Identification = (FunctionalGroupTypeState)replacement;
                            }
                        }
                    }

                    instance = Identification;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Lock:
                {
                    if (createOrReplace)
                    {
                        if (Lock == null)
                        {
                            if (replacement == null)
                            {
                                Lock = new LockingServicesTypeState(this);
                            }
                            else
                            {
                                Lock = (LockingServicesTypeState)replacement;
                            }
                        }
                    }

                    instance = Lock;
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
        private BaseObjectState m_parameterSet;
        private BaseObjectState m_methodSet;
        private FunctionalGroupTypeState m_identification;
        private LockingServicesTypeState m_lock;
        #endregion
    }
    #endif
    #endregion

    #region IVendorNameplateTypeState Class
    #if (!OPCUA_EXCLUDE_IVendorNameplateTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class IVendorNameplateTypeState : BaseInterfaceState
    {
        #region Constructors
        /// <remarks />
        public IVendorNameplateTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.IVendorNameplateType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (Manufacturer != null)
            {
                Manufacturer.Initialize(context, Manufacturer_InitializationString);
            }

            if (ManufacturerUri != null)
            {
                ManufacturerUri.Initialize(context, ManufacturerUri_InitializationString);
            }

            if (Model != null)
            {
                Model.Initialize(context, Model_InitializationString);
            }

            if (HardwareRevision != null)
            {
                HardwareRevision.Initialize(context, HardwareRevision_InitializationString);
            }

            if (SoftwareRevision != null)
            {
                SoftwareRevision.Initialize(context, SoftwareRevision_InitializationString);
            }

            if (DeviceRevision != null)
            {
                DeviceRevision.Initialize(context, DeviceRevision_InitializationString);
            }

            if (ProductCode != null)
            {
                ProductCode.Initialize(context, ProductCode_InitializationString);
            }

            if (DeviceManual != null)
            {
                DeviceManual.Initialize(context, DeviceManual_InitializationString);
            }

            if (DeviceClass != null)
            {
                DeviceClass.Initialize(context, DeviceClass_InitializationString);
            }

            if (SerialNumber != null)
            {
                SerialNumber.Initialize(context, SerialNumber_InitializationString);
            }

            if (ProductInstanceUri != null)
            {
                ProductInstanceUri.Initialize(context, ProductInstanceUri_InitializationString);
            }

            if (RevisionCounter != null)
            {
                RevisionCounter.Initialize(context, RevisionCounter_InitializationString);
            }

            if (SoftwareReleaseDate != null)
            {
                SoftwareReleaseDate.Initialize(context, SoftwareReleaseDate_InitializationString);
            }

            if (PatchIdentifiers != null)
            {
                PatchIdentifiers.Initialize(context, PatchIdentifiers_InitializationString);
            }
        }

        #region Initialization String
        private const string Manufacturer_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAATWFu" +
           "dWZhY3R1cmVyAQG8OgAuAES8OgAAABX/////AQH/////AAAAAA==";

        private const string ManufacturerUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAATWFu" +
           "dWZhY3R1cmVyVXJpAQG9OgAuAES9OgAAAAz/////AQH/////AAAAAA==";

        private const string Model_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAFAAAATW9k" +
           "ZWwBAb46AC4ARL46AAAAFf////8BAf////8AAAAA";

        private const string HardwareRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAQAAAASGFy" +
           "ZHdhcmVSZXZpc2lvbgEBvzoALgBEvzoAAAAM/////wEB/////wAAAAA=";

        private const string SoftwareRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAQAAAAU29m" +
           "dHdhcmVSZXZpc2lvbgEBwDoALgBEwDoAAAAM/////wEB/////wAAAAA=";

        private const string DeviceRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAOAAAARGV2" +
           "aWNlUmV2aXNpb24BAcE6AC4ARME6AAAADP////8BAf////8AAAAA";

        private const string ProductCode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAAUHJv" +
           "ZHVjdENvZGUBAcI6AC4ARMI6AAAADP////8BAf////8AAAAA";

        private const string DeviceManual_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAARGV2" +
           "aWNlTWFudWFsAQHDOgAuAETDOgAAAAz/////AQH/////AAAAAA==";

        private const string DeviceClass_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAARGV2" +
           "aWNlQ2xhc3MBAcQ6AC4ARMQ6AAAADP////8BAf////8AAAAA";

        private const string SerialNumber_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAAU2Vy" +
           "aWFsTnVtYmVyAQHFOgAuAETFOgAAAAz/////AQH/////AAAAAA==";

        private const string ProductInstanceUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQASAAAAUHJv" +
           "ZHVjdEluc3RhbmNlVXJpAQHGOgAuAETGOgAAAAz/////AQH/////AAAAAA==";

        private const string RevisionCounter_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAAUmV2" +
           "aXNpb25Db3VudGVyAQHHOgAuAETHOgAAAAb/////AQH/////AAAAAA==";

        private const string SoftwareReleaseDate_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQATAAAAU29m" +
           "dHdhcmVSZWxlYXNlRGF0ZQEBFwAALgBEFwAAAAAN/////wEB/////wAAAAA=";

        private const string PatchIdentifiers_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xdgiQoCAAAAAQAQAAAAUGF0" +
           "Y2hJZGVudGlmaWVycwEBGAAALgBEGAAAAAAMAQAAAAEAAAAAAAAAAQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAcAAAASVZl" +
           "bmRvck5hbWVwbGF0ZVR5cGVJbnN0YW5jZQEBuzoBAbs6uzoAAP////8OAAAAFWCJCgIAAAABAAwAAABN" +
           "YW51ZmFjdHVyZXIBAbw6AC4ARLw6AAAAFf////8BAf////8AAAAAFWCJCgIAAAABAA8AAABNYW51ZmFj" +
           "dHVyZXJVcmkBAb06AC4ARL06AAAADP////8BAf////8AAAAAFWCJCgIAAAABAAUAAABNb2RlbAEBvjoA" +
           "LgBEvjoAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEAEAAAAEhhcmR3YXJlUmV2aXNpb24BAb86AC4A" +
           "RL86AAAADP////8BAf////8AAAAAFWCJCgIAAAABABAAAABTb2Z0d2FyZVJldmlzaW9uAQHAOgAuAETA" +
           "OgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAOAAAARGV2aWNlUmV2aXNpb24BAcE6AC4ARME6AAAA" +
           "DP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABQcm9kdWN0Q29kZQEBwjoALgBEwjoAAAAM/////wEB" +
           "/////wAAAAAVYIkKAgAAAAEADAAAAERldmljZU1hbnVhbAEBwzoALgBEwzoAAAAM/////wEB/////wAA" +
           "AAAVYIkKAgAAAAEACwAAAERldmljZUNsYXNzAQHEOgAuAETEOgAAAAz/////AQH/////AAAAABVgiQoC" +
           "AAAAAQAMAAAAU2VyaWFsTnVtYmVyAQHFOgAuAETFOgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAS" +
           "AAAAUHJvZHVjdEluc3RhbmNlVXJpAQHGOgAuAETGOgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAP" +
           "AAAAUmV2aXNpb25Db3VudGVyAQHHOgAuAETHOgAAAAb/////AQH/////AAAAABVgiQoCAAAAAQATAAAA" +
           "U29mdHdhcmVSZWxlYXNlRGF0ZQEBFwAALgBEFwAAAAAN/////wEB/////wAAAAAXYIkKAgAAAAEAEAAA" +
           "AFBhdGNoSWRlbnRpZmllcnMBARgAAC4ARBgAAAAADAEAAAABAAAAAAAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<LocalizedText> Manufacturer
        {
            get
            {
                return m_manufacturer;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturer, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturer = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ManufacturerUri
        {
            get
            {
                return m_manufacturerUri;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturerUri, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturerUri = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText> Model
        {
            get
            {
                return m_model;
            }

            set
            {
                if (!Object.ReferenceEquals(m_model, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_model = value;
            }
        }

        /// <remarks />
        public PropertyState<string> HardwareRevision
        {
            get
            {
                return m_hardwareRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_hardwareRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_hardwareRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> SoftwareRevision
        {
            get
            {
                return m_softwareRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_softwareRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_softwareRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceRevision
        {
            get
            {
                return m_deviceRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ProductCode
        {
            get
            {
                return m_productCode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_productCode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_productCode = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceManual
        {
            get
            {
                return m_deviceManual;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceManual, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceManual = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceClass
        {
            get
            {
                return m_deviceClass;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceClass, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceClass = value;
            }
        }

        /// <remarks />
        public PropertyState<string> SerialNumber
        {
            get
            {
                return m_serialNumber;
            }

            set
            {
                if (!Object.ReferenceEquals(m_serialNumber, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_serialNumber = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ProductInstanceUri
        {
            get
            {
                return m_productInstanceUri;
            }

            set
            {
                if (!Object.ReferenceEquals(m_productInstanceUri, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_productInstanceUri = value;
            }
        }

        /// <remarks />
        public PropertyState<int> RevisionCounter
        {
            get
            {
                return m_revisionCounter;
            }

            set
            {
                if (!Object.ReferenceEquals(m_revisionCounter, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_revisionCounter = value;
            }
        }

        /// <remarks />
        public PropertyState<DateTime> SoftwareReleaseDate
        {
            get
            {
                return m_softwareReleaseDate;
            }

            set
            {
                if (!Object.ReferenceEquals(m_softwareReleaseDate, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_softwareReleaseDate = value;
            }
        }

        /// <remarks />
        public PropertyState<string[]> PatchIdentifiers
        {
            get
            {
                return m_patchIdentifiers;
            }

            set
            {
                if (!Object.ReferenceEquals(m_patchIdentifiers, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_patchIdentifiers = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_manufacturer != null)
            {
                children.Add(m_manufacturer);
            }

            if (m_manufacturerUri != null)
            {
                children.Add(m_manufacturerUri);
            }

            if (m_model != null)
            {
                children.Add(m_model);
            }

            if (m_hardwareRevision != null)
            {
                children.Add(m_hardwareRevision);
            }

            if (m_softwareRevision != null)
            {
                children.Add(m_softwareRevision);
            }

            if (m_deviceRevision != null)
            {
                children.Add(m_deviceRevision);
            }

            if (m_productCode != null)
            {
                children.Add(m_productCode);
            }

            if (m_deviceManual != null)
            {
                children.Add(m_deviceManual);
            }

            if (m_deviceClass != null)
            {
                children.Add(m_deviceClass);
            }

            if (m_serialNumber != null)
            {
                children.Add(m_serialNumber);
            }

            if (m_productInstanceUri != null)
            {
                children.Add(m_productInstanceUri);
            }

            if (m_revisionCounter != null)
            {
                children.Add(m_revisionCounter);
            }

            if (m_softwareReleaseDate != null)
            {
                children.Add(m_softwareReleaseDate);
            }

            if (m_patchIdentifiers != null)
            {
                children.Add(m_patchIdentifiers);
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
                case Opc.Ua.DI.BrowseNames.Manufacturer:
                {
                    if (createOrReplace)
                    {
                        if (Manufacturer == null)
                        {
                            if (replacement == null)
                            {
                                Manufacturer = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                Manufacturer = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = Manufacturer;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ManufacturerUri:
                {
                    if (createOrReplace)
                    {
                        if (ManufacturerUri == null)
                        {
                            if (replacement == null)
                            {
                                ManufacturerUri = new PropertyState<string>(this);
                            }
                            else
                            {
                                ManufacturerUri = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ManufacturerUri;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Model:
                {
                    if (createOrReplace)
                    {
                        if (Model == null)
                        {
                            if (replacement == null)
                            {
                                Model = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                Model = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = Model;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.HardwareRevision:
                {
                    if (createOrReplace)
                    {
                        if (HardwareRevision == null)
                        {
                            if (replacement == null)
                            {
                                HardwareRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                HardwareRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = HardwareRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SoftwareRevision:
                {
                    if (createOrReplace)
                    {
                        if (SoftwareRevision == null)
                        {
                            if (replacement == null)
                            {
                                SoftwareRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                SoftwareRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = SoftwareRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceRevision:
                {
                    if (createOrReplace)
                    {
                        if (DeviceRevision == null)
                        {
                            if (replacement == null)
                            {
                                DeviceRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProductCode:
                {
                    if (createOrReplace)
                    {
                        if (ProductCode == null)
                        {
                            if (replacement == null)
                            {
                                ProductCode = new PropertyState<string>(this);
                            }
                            else
                            {
                                ProductCode = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ProductCode;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceManual:
                {
                    if (createOrReplace)
                    {
                        if (DeviceManual == null)
                        {
                            if (replacement == null)
                            {
                                DeviceManual = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceManual = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceManual;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceClass:
                {
                    if (createOrReplace)
                    {
                        if (DeviceClass == null)
                        {
                            if (replacement == null)
                            {
                                DeviceClass = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceClass = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceClass;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SerialNumber:
                {
                    if (createOrReplace)
                    {
                        if (SerialNumber == null)
                        {
                            if (replacement == null)
                            {
                                SerialNumber = new PropertyState<string>(this);
                            }
                            else
                            {
                                SerialNumber = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = SerialNumber;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProductInstanceUri:
                {
                    if (createOrReplace)
                    {
                        if (ProductInstanceUri == null)
                        {
                            if (replacement == null)
                            {
                                ProductInstanceUri = new PropertyState<string>(this);
                            }
                            else
                            {
                                ProductInstanceUri = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ProductInstanceUri;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.RevisionCounter:
                {
                    if (createOrReplace)
                    {
                        if (RevisionCounter == null)
                        {
                            if (replacement == null)
                            {
                                RevisionCounter = new PropertyState<int>(this);
                            }
                            else
                            {
                                RevisionCounter = (PropertyState<int>)replacement;
                            }
                        }
                    }

                    instance = RevisionCounter;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SoftwareReleaseDate:
                {
                    if (createOrReplace)
                    {
                        if (SoftwareReleaseDate == null)
                        {
                            if (replacement == null)
                            {
                                SoftwareReleaseDate = new PropertyState<DateTime>(this);
                            }
                            else
                            {
                                SoftwareReleaseDate = (PropertyState<DateTime>)replacement;
                            }
                        }
                    }

                    instance = SoftwareReleaseDate;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.PatchIdentifiers:
                {
                    if (createOrReplace)
                    {
                        if (PatchIdentifiers == null)
                        {
                            if (replacement == null)
                            {
                                PatchIdentifiers = new PropertyState<string[]>(this);
                            }
                            else
                            {
                                PatchIdentifiers = (PropertyState<string[]>)replacement;
                            }
                        }
                    }

                    instance = PatchIdentifiers;
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
        private PropertyState<LocalizedText> m_manufacturer;
        private PropertyState<string> m_manufacturerUri;
        private PropertyState<LocalizedText> m_model;
        private PropertyState<string> m_hardwareRevision;
        private PropertyState<string> m_softwareRevision;
        private PropertyState<string> m_deviceRevision;
        private PropertyState<string> m_productCode;
        private PropertyState<string> m_deviceManual;
        private PropertyState<string> m_deviceClass;
        private PropertyState<string> m_serialNumber;
        private PropertyState<string> m_productInstanceUri;
        private PropertyState<int> m_revisionCounter;
        private PropertyState<DateTime> m_softwareReleaseDate;
        private PropertyState<string[]> m_patchIdentifiers;
        #endregion
    }
    #endif
    #endregion

    #region ITagNameplateTypeState Class
    #if (!OPCUA_EXCLUDE_ITagNameplateTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ITagNameplateTypeState : BaseInterfaceState
    {
        #region Constructors
        /// <remarks />
        public ITagNameplateTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ITagNameplateType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (AssetId != null)
            {
                AssetId.Initialize(context, AssetId_InitializationString);
            }

            if (ComponentName != null)
            {
                ComponentName.Initialize(context, ComponentName_InitializationString);
            }
        }

        #region Initialization String
        private const string AssetId_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAHAAAAQXNz" +
           "ZXRJZAEByToALgBEyToAAAAM/////wEB/////wAAAAA=";

        private const string ComponentName_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQANAAAAQ29t" +
           "cG9uZW50TmFtZQEByjoALgBEyjoAAAAV/////wEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAZAAAASVRh" +
           "Z05hbWVwbGF0ZVR5cGVJbnN0YW5jZQEByDoBAcg6yDoAAP////8CAAAAFWCJCgIAAAABAAcAAABBc3Nl" +
           "dElkAQHJOgAuAETJOgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQANAAAAQ29tcG9uZW50TmFtZQEB" +
           "yjoALgBEyjoAAAAV/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<string> AssetId
        {
            get
            {
                return m_assetId;
            }

            set
            {
                if (!Object.ReferenceEquals(m_assetId, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_assetId = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText> ComponentName
        {
            get
            {
                return m_componentName;
            }

            set
            {
                if (!Object.ReferenceEquals(m_componentName, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_componentName = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_assetId != null)
            {
                children.Add(m_assetId);
            }

            if (m_componentName != null)
            {
                children.Add(m_componentName);
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
                case Opc.Ua.DI.BrowseNames.AssetId:
                {
                    if (createOrReplace)
                    {
                        if (AssetId == null)
                        {
                            if (replacement == null)
                            {
                                AssetId = new PropertyState<string>(this);
                            }
                            else
                            {
                                AssetId = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = AssetId;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ComponentName:
                {
                    if (createOrReplace)
                    {
                        if (ComponentName == null)
                        {
                            if (replacement == null)
                            {
                                ComponentName = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                ComponentName = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = ComponentName;
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
        private PropertyState<string> m_assetId;
        private PropertyState<LocalizedText> m_componentName;
        #endregion
    }
    #endif
    #endregion

    #region IDeviceHealthTypeState Class
    #if (!OPCUA_EXCLUDE_IDeviceHealthTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class IDeviceHealthTypeState : BaseInterfaceState
    {
        #region Constructors
        /// <remarks />
        public IDeviceHealthTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.IDeviceHealthType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (DeviceHealth != null)
            {
                DeviceHealth.Initialize(context, DeviceHealth_InitializationString);
            }

            if (DeviceHealthAlarms != null)
            {
                DeviceHealthAlarms.Initialize(context, DeviceHealthAlarms_InitializationString);
            }
        }

        #region Initialization String
        private const string DeviceHealth_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAARGV2" +
           "aWNlSGVhbHRoAQHMOgAvAD/MOgAAAQFkGP////8BAf////8AAAAA";

        private const string DeviceHealthAlarms_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQASAAAARGV2" +
           "aWNlSGVhbHRoQWxhcm1zAQHNOgAvAD3NOgAA/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAZAAAASURl" +
           "dmljZUhlYWx0aFR5cGVJbnN0YW5jZQEByzoBAcs6yzoAAP////8CAAAAFWCJCgIAAAABAAwAAABEZXZp" +
           "Y2VIZWFsdGgBAcw6AC8AP8w6AAABAWQY/////wEB/////wAAAAAEYIAKAQAAAAEAEgAAAERldmljZUhl" +
           "YWx0aEFsYXJtcwEBzToALwA9zToAAP////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<DeviceHealthEnumeration> DeviceHealth
        {
            get
            {
                return m_deviceHealth;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceHealth, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceHealth = value;
            }
        }

        /// <remarks />
        public FolderState DeviceHealthAlarms
        {
            get
            {
                return m_deviceHealthAlarms;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceHealthAlarms, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceHealthAlarms = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_deviceHealth != null)
            {
                children.Add(m_deviceHealth);
            }

            if (m_deviceHealthAlarms != null)
            {
                children.Add(m_deviceHealthAlarms);
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
                case Opc.Ua.DI.BrowseNames.DeviceHealth:
                {
                    if (createOrReplace)
                    {
                        if (DeviceHealth == null)
                        {
                            if (replacement == null)
                            {
                                DeviceHealth = new BaseDataVariableState<DeviceHealthEnumeration>(this);
                            }
                            else
                            {
                                DeviceHealth = (BaseDataVariableState<DeviceHealthEnumeration>)replacement;
                            }
                        }
                    }

                    instance = DeviceHealth;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceHealthAlarms:
                {
                    if (createOrReplace)
                    {
                        if (DeviceHealthAlarms == null)
                        {
                            if (replacement == null)
                            {
                                DeviceHealthAlarms = new FolderState(this);
                            }
                            else
                            {
                                DeviceHealthAlarms = (FolderState)replacement;
                            }
                        }
                    }

                    instance = DeviceHealthAlarms;
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
        private BaseDataVariableState<DeviceHealthEnumeration> m_deviceHealth;
        private FolderState m_deviceHealthAlarms;
        #endregion
    }
    #endif
    #endregion

    #region ISupportInfoTypeState Class
    #if (!OPCUA_EXCLUDE_ISupportInfoTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ISupportInfoTypeState : BaseInterfaceState
    {
        #region Constructors
        /// <remarks />
        public ISupportInfoTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ISupportInfoType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (DeviceTypeImage != null)
            {
                DeviceTypeImage.Initialize(context, DeviceTypeImage_InitializationString);
            }

            if (Documentation != null)
            {
                Documentation.Initialize(context, Documentation_InitializationString);
            }

            if (ProtocolSupport != null)
            {
                ProtocolSupport.Initialize(context, ProtocolSupport_InitializationString);
            }

            if (ImageSet != null)
            {
                ImageSet.Initialize(context, ImageSet_InitializationString);
            }
        }

        #region Initialization String
        private const string DeviceTypeImage_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAPAAAARGV2" +
           "aWNlVHlwZUltYWdlAQHPOgAvAD3POgAA/////wEAAAAVYMkKAgAAAA8AAABJbWFnZUlkZW50aWZpZXIB" +
           "ABEAAAA8SW1hZ2VJZGVudGlmaWVyPgEB0DoALwA/0DoAAAAe/////wEB/////wAAAAA=";

        private const string Documentation_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQANAAAARG9j" +
           "dW1lbnRhdGlvbgEB0ToALwA90ToAAP////8BAAAAFWDJCgIAAAASAAAARG9jdW1lbnRJZGVudGlmaWVy" +
           "AQAUAAAAPERvY3VtZW50SWRlbnRpZmllcj4BAdI6AC8AP9I6AAAAD/////8BAf////8AAAAA";

        private const string ProtocolSupport_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAPAAAAUHJv" +
           "dG9jb2xTdXBwb3J0AQHTOgAvAD3TOgAA/////wEAAAAVYMkKAgAAABkAAABQcm90b2NvbFN1cHBvcnRJ" +
           "ZGVudGlmaWVyAQAbAAAAPFByb3RvY29sU3VwcG9ydElkZW50aWZpZXI+AQHUOgAvAD/UOgAAAA//////" +
           "AQH/////AAAAAA==";

        private const string ImageSet_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAIAAAASW1h" +
           "Z2VTZXQBAdU6AC8APdU6AAD/////AQAAABVgyQoCAAAADwAAAEltYWdlSWRlbnRpZmllcgEAEQAAADxJ" +
           "bWFnZUlkZW50aWZpZXI+AQHWOgAvAD/WOgAAAB7/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAYAAAASVN1" +
           "cHBvcnRJbmZvVHlwZUluc3RhbmNlAQHOOgEBzjrOOgAA/////wQAAAAEYIAKAQAAAAEADwAAAERldmlj" +
           "ZVR5cGVJbWFnZQEBzzoALwA9zzoAAP////8BAAAAFWDJCgIAAAAPAAAASW1hZ2VJZGVudGlmaWVyAQAR" +
           "AAAAPEltYWdlSWRlbnRpZmllcj4BAdA6AC8AP9A6AAAAHv////8BAf////8AAAAABGCACgEAAAABAA0A" +
           "AABEb2N1bWVudGF0aW9uAQHROgAvAD3ROgAA/////wEAAAAVYMkKAgAAABIAAABEb2N1bWVudElkZW50" +
           "aWZpZXIBABQAAAA8RG9jdW1lbnRJZGVudGlmaWVyPgEB0joALwA/0joAAAAP/////wEB/////wAAAAAE" +
           "YIAKAQAAAAEADwAAAFByb3RvY29sU3VwcG9ydAEB0zoALwA90zoAAP////8BAAAAFWDJCgIAAAAZAAAA" +
           "UHJvdG9jb2xTdXBwb3J0SWRlbnRpZmllcgEAGwAAADxQcm90b2NvbFN1cHBvcnRJZGVudGlmaWVyPgEB" +
           "1DoALwA/1DoAAAAP/////wEB/////wAAAAAEYIAKAQAAAAEACAAAAEltYWdlU2V0AQHVOgAvAD3VOgAA" +
           "/////wEAAAAVYMkKAgAAAA8AAABJbWFnZUlkZW50aWZpZXIBABEAAAA8SW1hZ2VJZGVudGlmaWVyPgEB" +
           "1joALwA/1joAAAAe/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public FolderState DeviceTypeImage
        {
            get
            {
                return m_deviceTypeImage;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceTypeImage, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceTypeImage = value;
            }
        }

        /// <remarks />
        public FolderState Documentation
        {
            get
            {
                return m_documentation;
            }

            set
            {
                if (!Object.ReferenceEquals(m_documentation, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_documentation = value;
            }
        }

        /// <remarks />
        public FolderState ProtocolSupport
        {
            get
            {
                return m_protocolSupport;
            }

            set
            {
                if (!Object.ReferenceEquals(m_protocolSupport, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_protocolSupport = value;
            }
        }

        /// <remarks />
        public FolderState ImageSet
        {
            get
            {
                return m_imageSet;
            }

            set
            {
                if (!Object.ReferenceEquals(m_imageSet, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_imageSet = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_deviceTypeImage != null)
            {
                children.Add(m_deviceTypeImage);
            }

            if (m_documentation != null)
            {
                children.Add(m_documentation);
            }

            if (m_protocolSupport != null)
            {
                children.Add(m_protocolSupport);
            }

            if (m_imageSet != null)
            {
                children.Add(m_imageSet);
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
                case Opc.Ua.DI.BrowseNames.DeviceTypeImage:
                {
                    if (createOrReplace)
                    {
                        if (DeviceTypeImage == null)
                        {
                            if (replacement == null)
                            {
                                DeviceTypeImage = new FolderState(this);
                            }
                            else
                            {
                                DeviceTypeImage = (FolderState)replacement;
                            }
                        }
                    }

                    instance = DeviceTypeImage;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Documentation:
                {
                    if (createOrReplace)
                    {
                        if (Documentation == null)
                        {
                            if (replacement == null)
                            {
                                Documentation = new FolderState(this);
                            }
                            else
                            {
                                Documentation = (FolderState)replacement;
                            }
                        }
                    }

                    instance = Documentation;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProtocolSupport:
                {
                    if (createOrReplace)
                    {
                        if (ProtocolSupport == null)
                        {
                            if (replacement == null)
                            {
                                ProtocolSupport = new FolderState(this);
                            }
                            else
                            {
                                ProtocolSupport = (FolderState)replacement;
                            }
                        }
                    }

                    instance = ProtocolSupport;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ImageSet:
                {
                    if (createOrReplace)
                    {
                        if (ImageSet == null)
                        {
                            if (replacement == null)
                            {
                                ImageSet = new FolderState(this);
                            }
                            else
                            {
                                ImageSet = (FolderState)replacement;
                            }
                        }
                    }

                    instance = ImageSet;
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
        private FolderState m_deviceTypeImage;
        private FolderState m_documentation;
        private FolderState m_protocolSupport;
        private FolderState m_imageSet;
        #endregion
    }
    #endif
    #endregion

    #region ComponentTypeState Class
    #if (!OPCUA_EXCLUDE_ComponentTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ComponentTypeState : TopologyElementTypeState
    {
        #region Constructors
        /// <remarks />
        public ComponentTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ComponentType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (Manufacturer != null)
            {
                Manufacturer.Initialize(context, Manufacturer_InitializationString);
            }

            if (ManufacturerUri != null)
            {
                ManufacturerUri.Initialize(context, ManufacturerUri_InitializationString);
            }

            if (Model != null)
            {
                Model.Initialize(context, Model_InitializationString);
            }

            if (HardwareRevision != null)
            {
                HardwareRevision.Initialize(context, HardwareRevision_InitializationString);
            }

            if (SoftwareRevision != null)
            {
                SoftwareRevision.Initialize(context, SoftwareRevision_InitializationString);
            }

            if (DeviceRevision != null)
            {
                DeviceRevision.Initialize(context, DeviceRevision_InitializationString);
            }

            if (ProductCode != null)
            {
                ProductCode.Initialize(context, ProductCode_InitializationString);
            }

            if (DeviceManual != null)
            {
                DeviceManual.Initialize(context, DeviceManual_InitializationString);
            }

            if (DeviceClass != null)
            {
                DeviceClass.Initialize(context, DeviceClass_InitializationString);
            }

            if (SerialNumber != null)
            {
                SerialNumber.Initialize(context, SerialNumber_InitializationString);
            }

            if (ProductInstanceUri != null)
            {
                ProductInstanceUri.Initialize(context, ProductInstanceUri_InitializationString);
            }

            if (RevisionCounter != null)
            {
                RevisionCounter.Initialize(context, RevisionCounter_InitializationString);
            }

            if (AssetId != null)
            {
                AssetId.Initialize(context, AssetId_InitializationString);
            }

            if (ComponentName != null)
            {
                ComponentName.Initialize(context, ComponentName_InitializationString);
            }
        }

        #region Initialization String
        private const string Manufacturer_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAATWFu" +
           "dWZhY3R1cmVyAQHuOgAuAETuOgAAABX/////AQH/////AAAAAA==";

        private const string ManufacturerUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAATWFu" +
           "dWZhY3R1cmVyVXJpAQHvOgAuAETvOgAAAAz/////AQH/////AAAAAA==";

        private const string Model_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAFAAAATW9k" +
           "ZWwBAfA6AC4ARPA6AAAAFf////8BAf////8AAAAA";

        private const string HardwareRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAQAAAASGFy" +
           "ZHdhcmVSZXZpc2lvbgEB8ToALgBE8ToAAAAM/////wEB/////wAAAAA=";

        private const string SoftwareRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAQAAAAU29m" +
           "dHdhcmVSZXZpc2lvbgEB8joALgBE8joAAAAM/////wEB/////wAAAAA=";

        private const string DeviceRevision_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAOAAAARGV2" +
           "aWNlUmV2aXNpb24BAfM6AC4ARPM6AAAADP////8BAf////8AAAAA";

        private const string ProductCode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAAUHJv" +
           "ZHVjdENvZGUBAfQ6AC4ARPQ6AAAADP////8BAf////8AAAAA";

        private const string DeviceManual_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAARGV2" +
           "aWNlTWFudWFsAQH1OgAuAET1OgAAAAz/////AQH/////AAAAAA==";

        private const string DeviceClass_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAARGV2" +
           "aWNlQ2xhc3MBAfY6AC4ARPY6AAAADP////8BAf////8AAAAA";

        private const string SerialNumber_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAAU2Vy" +
           "aWFsTnVtYmVyAQH3OgAuAET3OgAAAAz/////AQH/////AAAAAA==";

        private const string ProductInstanceUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQASAAAAUHJv" +
           "ZHVjdEluc3RhbmNlVXJpAQH4OgAuAET4OgAAAAz/////AQH/////AAAAAA==";

        private const string RevisionCounter_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAAUmV2" +
           "aXNpb25Db3VudGVyAQH5OgAuAET5OgAAAAb/////AQH/////AAAAAA==";

        private const string AssetId_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAHAAAAQXNz" +
           "ZXRJZAEB+joALgBE+joAAAAM/////wEB/////wAAAAA=";

        private const string ComponentName_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQANAAAAQ29t" +
           "cG9uZW50TmFtZQEB+zoALgBE+zoAAAAV/////wEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAVAAAAQ29t" +
           "cG9uZW50VHlwZUluc3RhbmNlAQHXOgEB1zrXOgAAAgAAAAEAw0QAAQG7OgEAw0QAAQHIOg4AAAAVYIkK" +
           "AgAAAAEADAAAAE1hbnVmYWN0dXJlcgEB7joALgBE7joAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "DwAAAE1hbnVmYWN0dXJlclVyaQEB7zoALgBE7zoAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEABQAA" +
           "AE1vZGVsAQHwOgAuAETwOgAAABX/////AQH/////AAAAABVgiQoCAAAAAQAQAAAASGFyZHdhcmVSZXZp" +
           "c2lvbgEB8ToALgBE8ToAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEAAAAFNvZnR3YXJlUmV2aXNp" +
           "b24BAfI6AC4ARPI6AAAADP////8BAf////8AAAAAFWCJCgIAAAABAA4AAABEZXZpY2VSZXZpc2lvbgEB" +
           "8zoALgBE8zoAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEACwAAAFByb2R1Y3RDb2RlAQH0OgAuAET0" +
           "OgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAMAAAARGV2aWNlTWFudWFsAQH1OgAuAET1OgAAAAz/" +
           "////AQH/////AAAAABVgiQoCAAAAAQALAAAARGV2aWNlQ2xhc3MBAfY6AC4ARPY6AAAADP////8BAf//" +
           "//8AAAAAFWCJCgIAAAABAAwAAABTZXJpYWxOdW1iZXIBAfc6AC4ARPc6AAAADP////8BAf////8AAAAA" +
           "FWCJCgIAAAABABIAAABQcm9kdWN0SW5zdGFuY2VVcmkBAfg6AC4ARPg6AAAADP////8BAf////8AAAAA" +
           "FWCJCgIAAAABAA8AAABSZXZpc2lvbkNvdW50ZXIBAfk6AC4ARPk6AAAABv////8BAf////8AAAAAFWCJ" +
           "CgIAAAABAAcAAABBc3NldElkAQH6OgAuAET6OgAAAAz/////AQH/////AAAAABVgiQoCAAAAAQANAAAA" +
           "Q29tcG9uZW50TmFtZQEB+zoALgBE+zoAAAAV/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<LocalizedText> Manufacturer
        {
            get
            {
                return m_manufacturer;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturer, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturer = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ManufacturerUri
        {
            get
            {
                return m_manufacturerUri;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturerUri, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturerUri = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText> Model
        {
            get
            {
                return m_model;
            }

            set
            {
                if (!Object.ReferenceEquals(m_model, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_model = value;
            }
        }

        /// <remarks />
        public PropertyState<string> HardwareRevision
        {
            get
            {
                return m_hardwareRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_hardwareRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_hardwareRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> SoftwareRevision
        {
            get
            {
                return m_softwareRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_softwareRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_softwareRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceRevision
        {
            get
            {
                return m_deviceRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ProductCode
        {
            get
            {
                return m_productCode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_productCode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_productCode = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceManual
        {
            get
            {
                return m_deviceManual;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceManual, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceManual = value;
            }
        }

        /// <remarks />
        public PropertyState<string> DeviceClass
        {
            get
            {
                return m_deviceClass;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceClass, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceClass = value;
            }
        }

        /// <remarks />
        public PropertyState<string> SerialNumber
        {
            get
            {
                return m_serialNumber;
            }

            set
            {
                if (!Object.ReferenceEquals(m_serialNumber, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_serialNumber = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ProductInstanceUri
        {
            get
            {
                return m_productInstanceUri;
            }

            set
            {
                if (!Object.ReferenceEquals(m_productInstanceUri, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_productInstanceUri = value;
            }
        }

        /// <remarks />
        public PropertyState<int> RevisionCounter
        {
            get
            {
                return m_revisionCounter;
            }

            set
            {
                if (!Object.ReferenceEquals(m_revisionCounter, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_revisionCounter = value;
            }
        }

        /// <remarks />
        public PropertyState<string> AssetId
        {
            get
            {
                return m_assetId;
            }

            set
            {
                if (!Object.ReferenceEquals(m_assetId, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_assetId = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText> ComponentName
        {
            get
            {
                return m_componentName;
            }

            set
            {
                if (!Object.ReferenceEquals(m_componentName, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_componentName = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_manufacturer != null)
            {
                children.Add(m_manufacturer);
            }

            if (m_manufacturerUri != null)
            {
                children.Add(m_manufacturerUri);
            }

            if (m_model != null)
            {
                children.Add(m_model);
            }

            if (m_hardwareRevision != null)
            {
                children.Add(m_hardwareRevision);
            }

            if (m_softwareRevision != null)
            {
                children.Add(m_softwareRevision);
            }

            if (m_deviceRevision != null)
            {
                children.Add(m_deviceRevision);
            }

            if (m_productCode != null)
            {
                children.Add(m_productCode);
            }

            if (m_deviceManual != null)
            {
                children.Add(m_deviceManual);
            }

            if (m_deviceClass != null)
            {
                children.Add(m_deviceClass);
            }

            if (m_serialNumber != null)
            {
                children.Add(m_serialNumber);
            }

            if (m_productInstanceUri != null)
            {
                children.Add(m_productInstanceUri);
            }

            if (m_revisionCounter != null)
            {
                children.Add(m_revisionCounter);
            }

            if (m_assetId != null)
            {
                children.Add(m_assetId);
            }

            if (m_componentName != null)
            {
                children.Add(m_componentName);
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
                case Opc.Ua.DI.BrowseNames.Manufacturer:
                {
                    if (createOrReplace)
                    {
                        if (Manufacturer == null)
                        {
                            if (replacement == null)
                            {
                                Manufacturer = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                Manufacturer = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = Manufacturer;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ManufacturerUri:
                {
                    if (createOrReplace)
                    {
                        if (ManufacturerUri == null)
                        {
                            if (replacement == null)
                            {
                                ManufacturerUri = new PropertyState<string>(this);
                            }
                            else
                            {
                                ManufacturerUri = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ManufacturerUri;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Model:
                {
                    if (createOrReplace)
                    {
                        if (Model == null)
                        {
                            if (replacement == null)
                            {
                                Model = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                Model = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = Model;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.HardwareRevision:
                {
                    if (createOrReplace)
                    {
                        if (HardwareRevision == null)
                        {
                            if (replacement == null)
                            {
                                HardwareRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                HardwareRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = HardwareRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SoftwareRevision:
                {
                    if (createOrReplace)
                    {
                        if (SoftwareRevision == null)
                        {
                            if (replacement == null)
                            {
                                SoftwareRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                SoftwareRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = SoftwareRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceRevision:
                {
                    if (createOrReplace)
                    {
                        if (DeviceRevision == null)
                        {
                            if (replacement == null)
                            {
                                DeviceRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProductCode:
                {
                    if (createOrReplace)
                    {
                        if (ProductCode == null)
                        {
                            if (replacement == null)
                            {
                                ProductCode = new PropertyState<string>(this);
                            }
                            else
                            {
                                ProductCode = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ProductCode;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceManual:
                {
                    if (createOrReplace)
                    {
                        if (DeviceManual == null)
                        {
                            if (replacement == null)
                            {
                                DeviceManual = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceManual = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceManual;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceClass:
                {
                    if (createOrReplace)
                    {
                        if (DeviceClass == null)
                        {
                            if (replacement == null)
                            {
                                DeviceClass = new PropertyState<string>(this);
                            }
                            else
                            {
                                DeviceClass = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = DeviceClass;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SerialNumber:
                {
                    if (createOrReplace)
                    {
                        if (SerialNumber == null)
                        {
                            if (replacement == null)
                            {
                                SerialNumber = new PropertyState<string>(this);
                            }
                            else
                            {
                                SerialNumber = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = SerialNumber;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProductInstanceUri:
                {
                    if (createOrReplace)
                    {
                        if (ProductInstanceUri == null)
                        {
                            if (replacement == null)
                            {
                                ProductInstanceUri = new PropertyState<string>(this);
                            }
                            else
                            {
                                ProductInstanceUri = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ProductInstanceUri;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.RevisionCounter:
                {
                    if (createOrReplace)
                    {
                        if (RevisionCounter == null)
                        {
                            if (replacement == null)
                            {
                                RevisionCounter = new PropertyState<int>(this);
                            }
                            else
                            {
                                RevisionCounter = (PropertyState<int>)replacement;
                            }
                        }
                    }

                    instance = RevisionCounter;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.AssetId:
                {
                    if (createOrReplace)
                    {
                        if (AssetId == null)
                        {
                            if (replacement == null)
                            {
                                AssetId = new PropertyState<string>(this);
                            }
                            else
                            {
                                AssetId = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = AssetId;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ComponentName:
                {
                    if (createOrReplace)
                    {
                        if (ComponentName == null)
                        {
                            if (replacement == null)
                            {
                                ComponentName = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                ComponentName = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = ComponentName;
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
        private PropertyState<LocalizedText> m_manufacturer;
        private PropertyState<string> m_manufacturerUri;
        private PropertyState<LocalizedText> m_model;
        private PropertyState<string> m_hardwareRevision;
        private PropertyState<string> m_softwareRevision;
        private PropertyState<string> m_deviceRevision;
        private PropertyState<string> m_productCode;
        private PropertyState<string> m_deviceManual;
        private PropertyState<string> m_deviceClass;
        private PropertyState<string> m_serialNumber;
        private PropertyState<string> m_productInstanceUri;
        private PropertyState<int> m_revisionCounter;
        private PropertyState<string> m_assetId;
        private PropertyState<LocalizedText> m_componentName;
        #endregion
    }
    #endif
    #endregion

    #region DeviceTypeState Class
    #if (!OPCUA_EXCLUDE_DeviceTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class DeviceTypeState : ComponentTypeState
    {
        #region Constructors
        /// <remarks />
        public DeviceTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.DeviceType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (ManufacturerUri != null)
            {
                ManufacturerUri.Initialize(context, ManufacturerUri_InitializationString);
            }

            if (ProductCode != null)
            {
                ProductCode.Initialize(context, ProductCode_InitializationString);
            }

            if (DeviceClass != null)
            {
                DeviceClass.Initialize(context, DeviceClass_InitializationString);
            }

            if (ProductInstanceUri != null)
            {
                ProductInstanceUri.Initialize(context, ProductInstanceUri_InitializationString);
            }

            if (DeviceHealth != null)
            {
                DeviceHealth.Initialize(context, DeviceHealth_InitializationString);
            }

            if (DeviceHealthAlarms != null)
            {
                DeviceHealthAlarms.Initialize(context, DeviceHealthAlarms_InitializationString);
            }

            if (DeviceTypeImage != null)
            {
                DeviceTypeImage.Initialize(context, DeviceTypeImage_InitializationString);
            }

            if (Documentation != null)
            {
                Documentation.Initialize(context, Documentation_InitializationString);
            }

            if (ProtocolSupport != null)
            {
                ProtocolSupport.Initialize(context, ProtocolSupport_InitializationString);
            }

            if (ImageSet != null)
            {
                ImageSet.Initialize(context, ImageSet_InitializationString);
            }
        }

        #region Initialization String
        private const string ManufacturerUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAATWFu" +
           "dWZhY3R1cmVyVXJpAQH8OgAuAET8OgAAAAz/////AQH/////AAAAAA==";

        private const string ProductCode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAAUHJv" +
           "ZHVjdENvZGUBAf06AC4ARP06AAAADP////8BAf////8AAAAA";

        private const string DeviceClass_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAARGV2" +
           "aWNlQ2xhc3MBAUYZAC4AREYZAAAADP////8BAf////8AAAAA";

        private const string ProductInstanceUri_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQASAAAAUHJv" +
           "ZHVjdEluc3RhbmNlVXJpAQH+OgAuAET+OgAAAAz/////AQH/////AAAAAA==";

        private const string DeviceHealth_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAARGV2" +
           "aWNlSGVhbHRoAQFAGAAvAD9AGAAAAQFkGP////8BAf////8AAAAA";

        private const string DeviceHealthAlarms_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQASAAAARGV2" +
           "aWNlSGVhbHRoQWxhcm1zAQEBOwAvAD0BOwAA/////wAAAAA=";

        private const string DeviceTypeImage_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAPAAAARGV2" +
           "aWNlVHlwZUltYWdlAQFBGAMAAAAAIQAAAE9yZ2FuaXplcyBwaWN0dXJlcyBvZiB0aGUgZGV2aWNlLgAv" +
           "AD1BGAAA/////wEAAAA1YMkKAgAAAA8AAABJbWFnZUlkZW50aWZpZXIBABEAAAA8SW1hZ2VJZGVudGlm" +
           "aWVyPgEBQhgDAAAAABcAAABBbiBpbWFnZSBvZiB0aGUgZGV2aWNlLgAvAD9CGAAAAB7/////AQH/////" +
           "AAAAAA==";

        private const string Documentation_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQANAAAARG9j" +
           "dW1lbnRhdGlvbgEBQxgDAAAAACMAAABPcmdhbml6ZXMgZG9jdW1lbnRzIGZvciB0aGUgZGV2aWNlLgAv" +
           "AD1DGAAA/////wEAAAA1YMkKAgAAABIAAABEb2N1bWVudElkZW50aWZpZXIBABQAAAA8RG9jdW1lbnRJ" +
           "ZGVudGlmaWVyPgEBRBgDAAAAABoAAABBIGRvY3VtZW50IGZvciB0aGUgZGV2aWNlLgAvAD9EGAAAAA//" +
           "////AQH/////AAAAAA==";

        private const string ProtocolSupport_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAPAAAAUHJv" +
           "dG9jb2xTdXBwb3J0AQFFGAMAAAAAJwAAAFByb3RvY29sLXNwZWNpZmljIGZpbGVzIGZvciB0aGUgZGV2" +
           "aWNlLgAvAD1FGAAA/////wEAAAA1YMkKAgAAABkAAABQcm90b2NvbFN1cHBvcnRJZGVudGlmaWVyAQAb" +
           "AAAAPFByb3RvY29sU3VwcG9ydElkZW50aWZpZXI+AQFGGAMAAAAAKAAAAEEgcHJvdG9jb2wtc3BlY2lm" +
           "aWMgZmlsZSBmb3IgdGhlIGRldmljZS4ALwA/RhgAAAAP/////wEB/////wAAAAA=";

        private const string ImageSet_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAIAAAASW1h" +
           "Z2VTZXQBAUcYAwAAAAAxAAAAT3JnYW5pemVzIGltYWdlcyB0aGF0IGFyZSB1c2VkIHdpdGhpbiBVSUVs" +
           "ZW1lbnRzLgAvAD1HGAAA/////wEAAAA1YMkKAgAAAA8AAABJbWFnZUlkZW50aWZpZXIBABEAAAA8SW1h" +
           "Z2VJZGVudGlmaWVyPgEBSBgDAAAAABkAAABBbiBpbWFnZSBmb3IgYSBVSUVsZW1lbnQuAC8AP0gYAAAA" +
           "Hv////8BAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQASAAAARGV2" +
           "aWNlVHlwZUluc3RhbmNlAQHqAwEB6gPqAwAAAgAAAAEAw0QAAQHOOgEAw0QAAQHLOhMAAAA1YIkKAgAA" +
           "AAEADAAAAE1hbnVmYWN0dXJlcgEBcxcDAAAAADAAAABOYW1lIG9mIHRoZSBjb21wYW55IHRoYXQgbWFu" +
           "dWZhY3R1cmVkIHRoZSBkZXZpY2UALgBEcxcAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAE1h" +
           "bnVmYWN0dXJlclVyaQEB/DoALgBE/DoAAAAM/////wEB/////wAAAAA1YIkKAgAAAAEABQAAAE1vZGVs" +
           "AQF0FwMAAAAAGAAAAE1vZGVsIG5hbWUgb2YgdGhlIGRldmljZQAuAER0FwAAABX/////AQH/////AAAA" +
           "ADVgiQoCAAAAAQAQAAAASGFyZHdhcmVSZXZpc2lvbgEBeBcDAAAAACwAAABSZXZpc2lvbiBsZXZlbCBv" +
           "ZiB0aGUgaGFyZHdhcmUgb2YgdGhlIGRldmljZQAuAER4FwAAAAz/////AQH/////AAAAADVgiQoCAAAA" +
           "AQAQAAAAU29mdHdhcmVSZXZpc2lvbgEBdxcDAAAAADUAAABSZXZpc2lvbiBsZXZlbCBvZiB0aGUgc29m" +
           "dHdhcmUvZmlybXdhcmUgb2YgdGhlIGRldmljZQAuAER3FwAAAAz/////AQH/////AAAAADVgiQoCAAAA" +
           "AQAOAAAARGV2aWNlUmV2aXNpb24BAXYXAwAAAAAkAAAAT3ZlcmFsbCByZXZpc2lvbiBsZXZlbCBvZiB0" +
           "aGUgZGV2aWNlAC4ARHYXAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABQcm9kdWN0Q29kZQEB" +
           "/ToALgBE/ToAAAAM/////wEB/////wAAAAA1YIkKAgAAAAEADAAAAERldmljZU1hbnVhbAEBdRcDAAAA" +
           "AFoAAABBZGRyZXNzIChwYXRobmFtZSBpbiB0aGUgZmlsZSBzeXN0ZW0gb3IgYSBVUkwgfCBXZWIgYWRk" +
           "cmVzcykgb2YgdXNlciBtYW51YWwgZm9yIHRoZSBkZXZpY2UALgBEdRcAAAAM/////wEB/////wAAAAAV" +
           "YIkKAgAAAAEACwAAAERldmljZUNsYXNzAQFGGQAuAERGGQAAAAz/////AQH/////AAAAADVgiQoCAAAA" +
           "AQAMAAAAU2VyaWFsTnVtYmVyAQFxFwMAAAAATQAAAElkZW50aWZpZXIgdGhhdCB1bmlxdWVseSBpZGVu" +
           "dGlmaWVzLCB3aXRoaW4gYSBtYW51ZmFjdHVyZXIsIGEgZGV2aWNlIGluc3RhbmNlAC4ARHEXAAAADP//" +
           "//8BAf////8AAAAAFWCJCgIAAAABABIAAABQcm9kdWN0SW5zdGFuY2VVcmkBAf46AC4ARP46AAAADP//" +
           "//8BAf////8AAAAANWCJCgIAAAABAA8AAABSZXZpc2lvbkNvdW50ZXIBAXIXAwAAAABpAAAAQW4gaW5j" +
           "cmVtZW50YWwgY291bnRlciBpbmRpY2F0aW5nIHRoZSBudW1iZXIgb2YgdGltZXMgdGhlIHN0YXRpYyBk" +
           "YXRhIHdpdGhpbiB0aGUgRGV2aWNlIGhhcyBiZWVuIG1vZGlmaWVkAC4ARHIXAAAABv////8BAf////8A" +
           "AAAABGDACgEAAAAMAAAAQ1BJZGVudGlmaWVyAQAOAAAAPENQSWRlbnRpZmllcj4BAasZAC8BAaQYqxkA" +
           "AP////8BAAAAJGCACgEAAAABAA4AAABOZXR3b3JrQWRkcmVzcwEBwBkDAAAAACoAAABUaGUgYWRkcmVz" +
           "cyBvZiB0aGUgZGV2aWNlIG9uIHRoaXMgbmV0d29yay4ALwEB7QPAGQAA/////wAAAAAVYIkKAgAAAAEA" +
           "DAAAAERldmljZUhlYWx0aAEBQBgALwA/QBgAAAEBZBj/////AQH/////AAAAAARggAoBAAAAAQASAAAA" +
           "RGV2aWNlSGVhbHRoQWxhcm1zAQEBOwAvAD0BOwAA/////wAAAAAkYIAKAQAAAAEADwAAAERldmljZVR5" +
           "cGVJbWFnZQEBQRgDAAAAACEAAABPcmdhbml6ZXMgcGljdHVyZXMgb2YgdGhlIGRldmljZS4ALwA9QRgA" +
           "AP////8BAAAANWDJCgIAAAAPAAAASW1hZ2VJZGVudGlmaWVyAQARAAAAPEltYWdlSWRlbnRpZmllcj4B" +
           "AUIYAwAAAAAXAAAAQW4gaW1hZ2Ugb2YgdGhlIGRldmljZS4ALwA/QhgAAAAe/////wEB/////wAAAAAk" +
           "YIAKAQAAAAEADQAAAERvY3VtZW50YXRpb24BAUMYAwAAAAAjAAAAT3JnYW5pemVzIGRvY3VtZW50cyBm" +
           "b3IgdGhlIGRldmljZS4ALwA9QxgAAP////8BAAAANWDJCgIAAAASAAAARG9jdW1lbnRJZGVudGlmaWVy" +
           "AQAUAAAAPERvY3VtZW50SWRlbnRpZmllcj4BAUQYAwAAAAAaAAAAQSBkb2N1bWVudCBmb3IgdGhlIGRl" +
           "dmljZS4ALwA/RBgAAAAP/////wEB/////wAAAAAkYIAKAQAAAAEADwAAAFByb3RvY29sU3VwcG9ydAEB" +
           "RRgDAAAAACcAAABQcm90b2NvbC1zcGVjaWZpYyBmaWxlcyBmb3IgdGhlIGRldmljZS4ALwA9RRgAAP//" +
           "//8BAAAANWDJCgIAAAAZAAAAUHJvdG9jb2xTdXBwb3J0SWRlbnRpZmllcgEAGwAAADxQcm90b2NvbFN1" +
           "cHBvcnRJZGVudGlmaWVyPgEBRhgDAAAAACgAAABBIHByb3RvY29sLXNwZWNpZmljIGZpbGUgZm9yIHRo" +
           "ZSBkZXZpY2UuAC8AP0YYAAAAD/////8BAf////8AAAAAJGCACgEAAAABAAgAAABJbWFnZVNldAEBRxgD" +
           "AAAAADEAAABPcmdhbml6ZXMgaW1hZ2VzIHRoYXQgYXJlIHVzZWQgd2l0aGluIFVJRWxlbWVudHMuAC8A" +
           "PUcYAAD/////AQAAADVgyQoCAAAADwAAAEltYWdlSWRlbnRpZmllcgEAEQAAADxJbWFnZUlkZW50aWZp" +
           "ZXI+AQFIGAMAAAAAGQAAAEFuIGltYWdlIGZvciBhIFVJRWxlbWVudC4ALwA/SBgAAAAe/////wEB////" +
           "/wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<DeviceHealthEnumeration> DeviceHealth
        {
            get
            {
                return m_deviceHealth;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceHealth, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceHealth = value;
            }
        }

        /// <remarks />
        public FolderState DeviceHealthAlarms
        {
            get
            {
                return m_deviceHealthAlarms;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceHealthAlarms, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceHealthAlarms = value;
            }
        }

        /// <remarks />
        public FolderState DeviceTypeImage
        {
            get
            {
                return m_deviceTypeImage;
            }

            set
            {
                if (!Object.ReferenceEquals(m_deviceTypeImage, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_deviceTypeImage = value;
            }
        }

        /// <remarks />
        public FolderState Documentation
        {
            get
            {
                return m_documentation;
            }

            set
            {
                if (!Object.ReferenceEquals(m_documentation, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_documentation = value;
            }
        }

        /// <remarks />
        public FolderState ProtocolSupport
        {
            get
            {
                return m_protocolSupport;
            }

            set
            {
                if (!Object.ReferenceEquals(m_protocolSupport, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_protocolSupport = value;
            }
        }

        /// <remarks />
        public FolderState ImageSet
        {
            get
            {
                return m_imageSet;
            }

            set
            {
                if (!Object.ReferenceEquals(m_imageSet, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_imageSet = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_deviceHealth != null)
            {
                children.Add(m_deviceHealth);
            }

            if (m_deviceHealthAlarms != null)
            {
                children.Add(m_deviceHealthAlarms);
            }

            if (m_deviceTypeImage != null)
            {
                children.Add(m_deviceTypeImage);
            }

            if (m_documentation != null)
            {
                children.Add(m_documentation);
            }

            if (m_protocolSupport != null)
            {
                children.Add(m_protocolSupport);
            }

            if (m_imageSet != null)
            {
                children.Add(m_imageSet);
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
                case Opc.Ua.DI.BrowseNames.DeviceHealth:
                {
                    if (createOrReplace)
                    {
                        if (DeviceHealth == null)
                        {
                            if (replacement == null)
                            {
                                DeviceHealth = new BaseDataVariableState<DeviceHealthEnumeration>(this);
                            }
                            else
                            {
                                DeviceHealth = (BaseDataVariableState<DeviceHealthEnumeration>)replacement;
                            }
                        }
                    }

                    instance = DeviceHealth;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceHealthAlarms:
                {
                    if (createOrReplace)
                    {
                        if (DeviceHealthAlarms == null)
                        {
                            if (replacement == null)
                            {
                                DeviceHealthAlarms = new FolderState(this);
                            }
                            else
                            {
                                DeviceHealthAlarms = (FolderState)replacement;
                            }
                        }
                    }

                    instance = DeviceHealthAlarms;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.DeviceTypeImage:
                {
                    if (createOrReplace)
                    {
                        if (DeviceTypeImage == null)
                        {
                            if (replacement == null)
                            {
                                DeviceTypeImage = new FolderState(this);
                            }
                            else
                            {
                                DeviceTypeImage = (FolderState)replacement;
                            }
                        }
                    }

                    instance = DeviceTypeImage;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Documentation:
                {
                    if (createOrReplace)
                    {
                        if (Documentation == null)
                        {
                            if (replacement == null)
                            {
                                Documentation = new FolderState(this);
                            }
                            else
                            {
                                Documentation = (FolderState)replacement;
                            }
                        }
                    }

                    instance = Documentation;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ProtocolSupport:
                {
                    if (createOrReplace)
                    {
                        if (ProtocolSupport == null)
                        {
                            if (replacement == null)
                            {
                                ProtocolSupport = new FolderState(this);
                            }
                            else
                            {
                                ProtocolSupport = (FolderState)replacement;
                            }
                        }
                    }

                    instance = ProtocolSupport;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ImageSet:
                {
                    if (createOrReplace)
                    {
                        if (ImageSet == null)
                        {
                            if (replacement == null)
                            {
                                ImageSet = new FolderState(this);
                            }
                            else
                            {
                                ImageSet = (FolderState)replacement;
                            }
                        }
                    }

                    instance = ImageSet;
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
        private BaseDataVariableState<DeviceHealthEnumeration> m_deviceHealth;
        private FolderState m_deviceHealthAlarms;
        private FolderState m_deviceTypeImage;
        private FolderState m_documentation;
        private FolderState m_protocolSupport;
        private FolderState m_imageSet;
        #endregion
    }
    #endif
    #endregion

    #region SoftwareTypeState Class
    #if (!OPCUA_EXCLUDE_SoftwareTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SoftwareTypeState : ComponentTypeState
    {
        #region Constructors
        /// <remarks />
        public SoftwareTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.SoftwareType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAUAAAAU29m" +
           "dHdhcmVUeXBlSW5zdGFuY2UBAQI7AQECOwI7AAD/////AwAAABVgiQoCAAAAAQAMAAAATWFudWZhY3R1" +
           "cmVyAQEZOwAuAEQZOwAAABX/////AQH/////AAAAABVgiQoCAAAAAQAFAAAATW9kZWwBARs7AC4ARBs7" +
           "AAAAFf////8BAf////8AAAAAFWCJCgIAAAABABAAAABTb2Z0d2FyZVJldmlzaW9uAQEdOwAuAEQdOwAA" +
           "AAz/////AQH/////AAAAAA==";
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

    #region BlockTypeState Class
    #if (!OPCUA_EXCLUDE_BlockTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class BlockTypeState : TopologyElementTypeState
    {
        #region Constructors
        /// <remarks />
        public BlockTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.BlockType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (RevisionCounter != null)
            {
                RevisionCounter.Initialize(context, RevisionCounter_InitializationString);
            }

            if (ActualMode != null)
            {
                ActualMode.Initialize(context, ActualMode_InitializationString);
            }

            if (PermittedMode != null)
            {
                PermittedMode.Initialize(context, PermittedMode_InitializationString);
            }

            if (NormalMode != null)
            {
                NormalMode.Initialize(context, NormalMode_InitializationString);
            }

            if (TargetMode != null)
            {
                TargetMode.Initialize(context, TargetMode_InitializationString);
            }
        }

        #region Initialization String
        private const string RevisionCounter_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zVgiQoCAAAAAQAPAAAAUmV2" +
           "aXNpb25Db3VudGVyAQF5FwMAAAAAZQAAAEluY3JlbWVudGFsIGNvdW50ZXIgaW5kaWNhdGluZyB0aGUg" +
           "bnVtYmVyIG9mIHRpbWVzIHRoZSBzdGF0aWMgZGF0YSB3aXRoaW4gdGhlIEJsb2NrIGhhcyBiZWVuIG1v" +
           "ZGlmaWVkAC4ARHkXAAAABv////8BAf////8AAAAA";

        private const string ActualMode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zVgiQoCAAAAAQAKAAAAQWN0" +
           "dWFsTW9kZQEBehcDAAAAADYAAABDdXJyZW50IG1vZGUgb2Ygb3BlcmF0aW9uIHRoZSBCbG9jayBpcyBh" +
           "YmxlIHRvIGFjaGlldmUALgBEehcAAAAV/////wEB/////wAAAAA=";

        private const string PermittedMode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zdgiQoCAAAAAQANAAAAUGVy" +
           "bWl0dGVkTW9kZQEBexcDAAAAAFMAAABNb2RlcyBvZiBvcGVyYXRpb24gdGhhdCBhcmUgYWxsb3dlZCBm" +
           "b3IgdGhlIEJsb2NrIGJhc2VkIG9uIGFwcGxpY2F0aW9uIHJlcXVpcmVtZW50cwAuAER7FwAAABUBAAAA" +
           "AQAAAAAAAAABAf////8AAAAA";

        private const string NormalMode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zdgiQoCAAAAAQAKAAAATm9y" +
           "bWFsTW9kZQEBfBcDAAAAAEIAAABNb2RlIHRoZSBCbG9jayBzaG91bGQgYmUgc2V0IHRvIGR1cmluZyBu" +
           "b3JtYWwgb3BlcmF0aW5nIGNvbmRpdGlvbnMALgBEfBcAAAAVAQAAAAEAAAAAAAAAAQH/////AAAAAA==";

        private const string TargetMode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zdgiQoCAAAAAQAKAAAAVGFy" +
           "Z2V0TW9kZQEBfRcDAAAAAC8AAABNb2RlIG9mIG9wZXJhdGlvbiB0aGF0IGlzIGRlc2lyZWQgZm9yIHRo" +
           "ZSBCbG9jawAuAER9FwAAABUBAAAAAQAAAAAAAAABAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQARAAAAQmxv" +
           "Y2tUeXBlSW5zdGFuY2UBAesDAQHrA+sDAAD/////BQAAADVgiQoCAAAAAQAPAAAAUmV2aXNpb25Db3Vu" +
           "dGVyAQF5FwMAAAAAZQAAAEluY3JlbWVudGFsIGNvdW50ZXIgaW5kaWNhdGluZyB0aGUgbnVtYmVyIG9m" +
           "IHRpbWVzIHRoZSBzdGF0aWMgZGF0YSB3aXRoaW4gdGhlIEJsb2NrIGhhcyBiZWVuIG1vZGlmaWVkAC4A" +
           "RHkXAAAABv////8BAf////8AAAAANWCJCgIAAAABAAoAAABBY3R1YWxNb2RlAQF6FwMAAAAANgAAAEN1" +
           "cnJlbnQgbW9kZSBvZiBvcGVyYXRpb24gdGhlIEJsb2NrIGlzIGFibGUgdG8gYWNoaWV2ZQAuAER6FwAA" +
           "ABX/////AQH/////AAAAADdgiQoCAAAAAQANAAAAUGVybWl0dGVkTW9kZQEBexcDAAAAAFMAAABNb2Rl" +
           "cyBvZiBvcGVyYXRpb24gdGhhdCBhcmUgYWxsb3dlZCBmb3IgdGhlIEJsb2NrIGJhc2VkIG9uIGFwcGxp" +
           "Y2F0aW9uIHJlcXVpcmVtZW50cwAuAER7FwAAABUBAAAAAQAAAAAAAAABAf////8AAAAAN2CJCgIAAAAB" +
           "AAoAAABOb3JtYWxNb2RlAQF8FwMAAAAAQgAAAE1vZGUgdGhlIEJsb2NrIHNob3VsZCBiZSBzZXQgdG8g" +
           "ZHVyaW5nIG5vcm1hbCBvcGVyYXRpbmcgY29uZGl0aW9ucwAuAER8FwAAABUBAAAAAQAAAAAAAAABAf//" +
           "//8AAAAAN2CJCgIAAAABAAoAAABUYXJnZXRNb2RlAQF9FwMAAAAALwAAAE1vZGUgb2Ygb3BlcmF0aW9u" +
           "IHRoYXQgaXMgZGVzaXJlZCBmb3IgdGhlIEJsb2NrAC4ARH0XAAAAFQEAAAABAAAAAAAAAAEB/////wAA" +
           "AAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<int> RevisionCounter
        {
            get
            {
                return m_revisionCounter;
            }

            set
            {
                if (!Object.ReferenceEquals(m_revisionCounter, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_revisionCounter = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText> ActualMode
        {
            get
            {
                return m_actualMode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_actualMode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_actualMode = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText[]> PermittedMode
        {
            get
            {
                return m_permittedMode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_permittedMode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_permittedMode = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText[]> NormalMode
        {
            get
            {
                return m_normalMode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_normalMode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_normalMode = value;
            }
        }

        /// <remarks />
        public PropertyState<LocalizedText[]> TargetMode
        {
            get
            {
                return m_targetMode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_targetMode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_targetMode = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_revisionCounter != null)
            {
                children.Add(m_revisionCounter);
            }

            if (m_actualMode != null)
            {
                children.Add(m_actualMode);
            }

            if (m_permittedMode != null)
            {
                children.Add(m_permittedMode);
            }

            if (m_normalMode != null)
            {
                children.Add(m_normalMode);
            }

            if (m_targetMode != null)
            {
                children.Add(m_targetMode);
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
                case Opc.Ua.DI.BrowseNames.RevisionCounter:
                {
                    if (createOrReplace)
                    {
                        if (RevisionCounter == null)
                        {
                            if (replacement == null)
                            {
                                RevisionCounter = new PropertyState<int>(this);
                            }
                            else
                            {
                                RevisionCounter = (PropertyState<int>)replacement;
                            }
                        }
                    }

                    instance = RevisionCounter;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ActualMode:
                {
                    if (createOrReplace)
                    {
                        if (ActualMode == null)
                        {
                            if (replacement == null)
                            {
                                ActualMode = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                ActualMode = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = ActualMode;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.PermittedMode:
                {
                    if (createOrReplace)
                    {
                        if (PermittedMode == null)
                        {
                            if (replacement == null)
                            {
                                PermittedMode = new PropertyState<LocalizedText[]>(this);
                            }
                            else
                            {
                                PermittedMode = (PropertyState<LocalizedText[]>)replacement;
                            }
                        }
                    }

                    instance = PermittedMode;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.NormalMode:
                {
                    if (createOrReplace)
                    {
                        if (NormalMode == null)
                        {
                            if (replacement == null)
                            {
                                NormalMode = new PropertyState<LocalizedText[]>(this);
                            }
                            else
                            {
                                NormalMode = (PropertyState<LocalizedText[]>)replacement;
                            }
                        }
                    }

                    instance = NormalMode;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.TargetMode:
                {
                    if (createOrReplace)
                    {
                        if (TargetMode == null)
                        {
                            if (replacement == null)
                            {
                                TargetMode = new PropertyState<LocalizedText[]>(this);
                            }
                            else
                            {
                                TargetMode = (PropertyState<LocalizedText[]>)replacement;
                            }
                        }
                    }

                    instance = TargetMode;
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
        private PropertyState<int> m_revisionCounter;
        private PropertyState<LocalizedText> m_actualMode;
        private PropertyState<LocalizedText[]> m_permittedMode;
        private PropertyState<LocalizedText[]> m_normalMode;
        private PropertyState<LocalizedText[]> m_targetMode;
        #endregion
    }
    #endif
    #endregion

    #region DeviceHealthDiagnosticAlarmTypeState Class
    #if (!OPCUA_EXCLUDE_DeviceHealthDiagnosticAlarmTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class DeviceHealthDiagnosticAlarmTypeState : InstrumentDiagnosticAlarmState
    {
        #region Constructors
        /// <remarks />
        public DeviceHealthDiagnosticAlarmTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.DeviceHealthDiagnosticAlarmType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAnAAAARGV2" +
           "aWNlSGVhbHRoRGlhZ25vc3RpY0FsYXJtVHlwZUluc3RhbmNlAQEnOwEBJzsnOwAA/////xsAAAAVYIkI" +
           "AgAAAAAABwAAAEV2ZW50SWQBAQAAAC4ARAAP/////wEB/////wAAAAAVYIkIAgAAAAAACQAAAEV2ZW50" +
           "VHlwZQEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTm9kZQEBAAAALgBE" +
           "ABH/////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTmFtZQEBAAAALgBEAAz/////AQH/////" +
           "AAAAABVgiQgCAAAAAAAEAAAAVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAsA" +
           "AABSZWNlaXZlVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAcAAABNZXNzYWdl" +
           "AQEAAAAuAEQAFf////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABTZXZlcml0eQEBAAAALgBEAAX/////" +
           "AQH/////AAAAABVgiQgCAAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBAAAALgBEABH/////AQH/////" +
           "AAAAABVgiQgCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQEAAAAuAEQAFf////8BAf////8AAAAA" +
           "FWCJCAIAAAAAAA0AAABDb25kaXRpb25OYW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAA" +
           "AAgAAABCcmFuY2hJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAGAAAAUmV0YWluAQEA" +
           "AAAuAEQAAf////8BAf////8AAAAAFWCJCAIAAAAAAAwAAABFbmFibGVkU3RhdGUBAQAAAC8BACMjABX/" +
           "////AQEBAAAAAQAsIwABAQAAAQAAABVgiQgCAAAAAAACAAAASWQBAQAAAC4ARAAB/////wEB/////wAA" +
           "AAAVYIkIAgAAAAAABwAAAFF1YWxpdHkBAQAAAC8BACojABP/////AQH/////AQAAABVgiQgCAAAAAAAP" +
           "AAAAU291cmNlVGltZXN0YW1wAQEAAAAuAEQBACYB/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAExh" +
           "c3RTZXZlcml0eQEBAAAALwEAKiMABf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1l" +
           "c3RhbXABAQAAAC4ARAEAJgH/////AQH/////AAAAABVgiQgCAAAAAAAHAAAAQ29tbWVudAEBAAAALwEA" +
           "KiMAFf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEAJgH/" +
           "////AQH/////AAAAABVgiQgCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEAAAAuAEQADP////8BAf////8A" +
           "AAAABGGCCAQAAAAAAAcAAABEaXNhYmxlAQEAAAAvAQBEIwEBAQAAAAEA+QsAAQDzCgAAAAAEYYIIBAAA" +
           "AAAABgAAAEVuYWJsZQEBAAAALwEAQyMBAQEAAAABAPkLAAEA8woAAAAABGGCCAQAAAAAAAoAAABBZGRD" +
           "b21tZW50AQEAAAAvAQBFIwEBAQAAAAEA+QsAAQANCwEAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1l" +
           "bnRzAQEAAAAuAESWAgAAAAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBp" +
           "ZGVudGlmaWVyIGZvciB0aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV////" +
           "/wAAAAADAAAAACQAAABUaGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEA" +
           "AAAAAAAAAQH/////AAAAABVgiQgCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBAAAALwEAIyMAFf////8BAQEA" +
           "AAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAAARhgggE" +
           "AAAAAAALAAAAQWNrbm93bGVkZ2UBAQAAAC8BAJcjAQEBAAAAAQD5CwABAPAiAQAAABdgqQgCAAAAAAAO" +
           "AAAASW5wdXRBcmd1bWVudHMBAQAAAC4ARJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAA" +
           "AwAAAAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcA" +
           "AABDb21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0" +
           "aW9uLgEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAFWCJCAIAAAAAAAsAAABBY3RpdmVTdGF0ZQEBAAAA" +
           "LwEAIyMAFf////8BAQEAAAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////" +
           "AQH/////AAAAABVgiQgCAAAAAAAJAAAASW5wdXROb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJ" +
           "CAIAAAAAABMAAABTdXBwcmVzc2VkT3JTaGVsdmVkAQEAAAAuAEQAAf////8BAf////8AAAAAFWCJCAIA" +
           "AAAAAAsAAABOb3JtYWxTdGF0ZQEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region FailureAlarmTypeState Class
    #if (!OPCUA_EXCLUDE_FailureAlarmTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class FailureAlarmTypeState : DeviceHealthDiagnosticAlarmTypeState
    {
        #region Constructors
        /// <remarks />
        public FailureAlarmTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.FailureAlarmType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAYAAAARmFp" +
           "bHVyZUFsYXJtVHlwZUluc3RhbmNlAQG8OwEBvDu8OwAA/////xsAAAAVYIkIAgAAAAAABwAAAEV2ZW50" +
           "SWQBAQAAAC4ARAAP/////wEB/////wAAAAAVYIkIAgAAAAAACQAAAEV2ZW50VHlwZQEBAAAALgBEABH/" +
           "////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTm9kZQEBAAAALgBEABH/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAAKAAAAU291cmNlTmFtZQEBAAAALgBEAAz/////AQH/////AAAAABVgiQgCAAAAAAAE" +
           "AAAAVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABSZWNlaXZlVGltZQEB" +
           "AAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAcAAABNZXNzYWdlAQEAAAAuAEQAFf////8B" +
           "Af////8AAAAAFWCJCAIAAAAAAAgAAABTZXZlcml0eQEBAAAALgBEAAX/////AQH/////AAAAABVgiQgC" +
           "AAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAS" +
           "AAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQEAAAAuAEQAFf////8BAf////8AAAAAFWCJCAIAAAAAAA0AAABD" +
           "b25kaXRpb25OYW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABCcmFuY2hJZAEB" +
           "AAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAGAAAAUmV0YWluAQEAAAAuAEQAAf////8BAf//" +
           "//8AAAAAFWCJCAIAAAAAAAwAAABFbmFibGVkU3RhdGUBAQAAAC8BACMjABX/////AQEBAAAAAQAsIwAB" +
           "AQAAAQAAABVgiQgCAAAAAAACAAAASWQBAQAAAC4ARAAB/////wEB/////wAAAAAVYIkIAgAAAAAABwAA" +
           "AFF1YWxpdHkBAQAAAC8BACojABP/////AQH/////AQAAABVgiQgCAAAAAAAPAAAAU291cmNlVGltZXN0" +
           "YW1wAQEAAAAuAEQBACYB/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAExhc3RTZXZlcml0eQEBAAAA" +
           "LwEAKiMABf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEA" +
           "JgH/////AQH/////AAAAABVgiQgCAAAAAAAHAAAAQ29tbWVudAEBAAAALwEAKiMAFf////8BAf////8B" +
           "AAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEAJgH/////AQH/////AAAAABVg" +
           "iQgCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEAAAAuAEQADP////8BAf////8AAAAABGGCCAQAAAAAAAcA" +
           "AABEaXNhYmxlAQEAAAAvAQBEIwEBAQAAAAEA+QsAAQDzCgAAAAAEYYIIBAAAAAAABgAAAEVuYWJsZQEB" +
           "AAAALwEAQyMBAQEAAAABAPkLAAEA8woAAAAABGGCCAQAAAAAAAoAAABBZGRDb21tZW50AQEAAAAvAQBF" +
           "IwEBAQAAAAEA+QsAAQANCwEAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEAAAAuAESWAgAA" +
           "AAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0" +
           "aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABU" +
           "aGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEAAAAAAAAAAQH/////AAAA" +
           "ABVgiQgCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBAAAALwEAIyMAFf////8BAQEAAAABACwjAQEBAAABAAAA" +
           "FWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAAARhgggEAAAAAAALAAAAQWNrbm93" +
           "bGVkZ2UBAQAAAC8BAJcjAQEBAAAAAQD5CwABAPAiAQAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVu" +
           "dHMBAQAAAC4ARJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAAAAAoAAAAVGhlIGlk" +
           "ZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABDb21tZW50ABX/////" +
           "AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9uLgEAKAEBAAAAAQAA" +
           "AAAAAAABAf////8AAAAAFWCJCAIAAAAAAAsAAABBY3RpdmVTdGF0ZQEBAAAALwEAIyMAFf////8BAQEA" +
           "AAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAABVgiQgC" +
           "AAAAAAAJAAAASW5wdXROb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIAAAAAABMAAABTdXBw" +
           "cmVzc2VkT3JTaGVsdmVkAQEAAAAuAEQAAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABOb3JtYWxT" +
           "dGF0ZQEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region CheckFunctionAlarmTypeState Class
    #if (!OPCUA_EXCLUDE_CheckFunctionAlarmTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class CheckFunctionAlarmTypeState : DeviceHealthDiagnosticAlarmTypeState
    {
        #region Constructors
        /// <remarks />
        public CheckFunctionAlarmTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.CheckFunctionAlarmType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAeAAAAQ2hl" +
           "Y2tGdW5jdGlvbkFsYXJtVHlwZUluc3RhbmNlAQFRPAEBUTxRPAAA/////xsAAAAVYIkIAgAAAAAABwAA" +
           "AEV2ZW50SWQBAQAAAC4ARAAP/////wEB/////wAAAAAVYIkIAgAAAAAACQAAAEV2ZW50VHlwZQEBAAAA" +
           "LgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTm9kZQEBAAAALgBEABH/////AQH/" +
           "////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTmFtZQEBAAAALgBEAAz/////AQH/////AAAAABVgiQgC" +
           "AAAAAAAEAAAAVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABSZWNlaXZl" +
           "VGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAcAAABNZXNzYWdlAQEAAAAuAEQA" +
           "Ff////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABTZXZlcml0eQEBAAAALgBEAAX/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgC" +
           "AAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQEAAAAuAEQAFf////8BAf////8AAAAAFWCJCAIAAAAA" +
           "AA0AAABDb25kaXRpb25OYW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABCcmFu" +
           "Y2hJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAGAAAAUmV0YWluAQEAAAAuAEQAAf//" +
           "//8BAf////8AAAAAFWCJCAIAAAAAAAwAAABFbmFibGVkU3RhdGUBAQAAAC8BACMjABX/////AQEBAAAA" +
           "AQAsIwABAQAAAQAAABVgiQgCAAAAAAACAAAASWQBAQAAAC4ARAAB/////wEB/////wAAAAAVYIkIAgAA" +
           "AAAABwAAAFF1YWxpdHkBAQAAAC8BACojABP/////AQH/////AQAAABVgiQgCAAAAAAAPAAAAU291cmNl" +
           "VGltZXN0YW1wAQEAAAAuAEQBACYB/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAExhc3RTZXZlcml0" +
           "eQEBAAAALwEAKiMABf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAA" +
           "AC4ARAEAJgH/////AQH/////AAAAABVgiQgCAAAAAAAHAAAAQ29tbWVudAEBAAAALwEAKiMAFf////8B" +
           "Af////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEAJgH/////AQH/////" +
           "AAAAABVgiQgCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEAAAAuAEQADP////8BAf////8AAAAABGGCCAQA" +
           "AAAAAAcAAABEaXNhYmxlAQEAAAAvAQBEIwEBAQAAAAEA+QsAAQDzCgAAAAAEYYIIBAAAAAAABgAAAEVu" +
           "YWJsZQEBAAAALwEAQyMBAQEAAAABAPkLAAEA8woAAAAABGGCCAQAAAAAAAoAAABBZGRDb21tZW50AQEA" +
           "AAAvAQBFIwEBAQAAAAEA+QsAAQANCwEAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEAAAAu" +
           "AESWAgAAAAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVy" +
           "IGZvciB0aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAA" +
           "ACQAAABUaGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEAAAAAAAAAAQH/" +
           "////AAAAABVgiQgCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBAAAALwEAIyMAFf////8BAQEAAAABACwjAQEB" +
           "AAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAAARhgggEAAAAAAALAAAA" +
           "QWNrbm93bGVkZ2UBAQAAAC8BAJcjAQEBAAAAAQD5CwABAPAiAQAAABdgqQgCAAAAAAAOAAAASW5wdXRB" +
           "cmd1bWVudHMBAQAAAC4ARJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAAAAAoAAAA" +
           "VGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABDb21tZW50" +
           "ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9uLgEAKAEB" +
           "AAAAAQAAAAAAAAABAf////8AAAAAFWCJCAIAAAAAAAsAAABBY3RpdmVTdGF0ZQEBAAAALwEAIyMAFf//" +
           "//8BAQEAAAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAAJAAAASW5wdXROb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIAAAAAABMA" +
           "AABTdXBwcmVzc2VkT3JTaGVsdmVkAQEAAAAuAEQAAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABO" +
           "b3JtYWxTdGF0ZQEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region OffSpecAlarmTypeState Class
    #if (!OPCUA_EXCLUDE_OffSpecAlarmTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class OffSpecAlarmTypeState : DeviceHealthDiagnosticAlarmTypeState
    {
        #region Constructors
        /// <remarks />
        public OffSpecAlarmTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.OffSpecAlarmType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAYAAAAT2Zm" +
           "U3BlY0FsYXJtVHlwZUluc3RhbmNlAQHmPAEB5jzmPAAA/////xsAAAAVYIkIAgAAAAAABwAAAEV2ZW50" +
           "SWQBAQAAAC4ARAAP/////wEB/////wAAAAAVYIkIAgAAAAAACQAAAEV2ZW50VHlwZQEBAAAALgBEABH/" +
           "////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTm9kZQEBAAAALgBEABH/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAAKAAAAU291cmNlTmFtZQEBAAAALgBEAAz/////AQH/////AAAAABVgiQgCAAAAAAAE" +
           "AAAAVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABSZWNlaXZlVGltZQEB" +
           "AAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAcAAABNZXNzYWdlAQEAAAAuAEQAFf////8B" +
           "Af////8AAAAAFWCJCAIAAAAAAAgAAABTZXZlcml0eQEBAAAALgBEAAX/////AQH/////AAAAABVgiQgC" +
           "AAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAS" +
           "AAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQEAAAAuAEQAFf////8BAf////8AAAAAFWCJCAIAAAAAAA0AAABD" +
           "b25kaXRpb25OYW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABCcmFuY2hJZAEB" +
           "AAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAGAAAAUmV0YWluAQEAAAAuAEQAAf////8BAf//" +
           "//8AAAAAFWCJCAIAAAAAAAwAAABFbmFibGVkU3RhdGUBAQAAAC8BACMjABX/////AQEBAAAAAQAsIwAB" +
           "AQAAAQAAABVgiQgCAAAAAAACAAAASWQBAQAAAC4ARAAB/////wEB/////wAAAAAVYIkIAgAAAAAABwAA" +
           "AFF1YWxpdHkBAQAAAC8BACojABP/////AQH/////AQAAABVgiQgCAAAAAAAPAAAAU291cmNlVGltZXN0" +
           "YW1wAQEAAAAuAEQBACYB/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAExhc3RTZXZlcml0eQEBAAAA" +
           "LwEAKiMABf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEA" +
           "JgH/////AQH/////AAAAABVgiQgCAAAAAAAHAAAAQ29tbWVudAEBAAAALwEAKiMAFf////8BAf////8B" +
           "AAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEAJgH/////AQH/////AAAAABVg" +
           "iQgCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEAAAAuAEQADP////8BAf////8AAAAABGGCCAQAAAAAAAcA" +
           "AABEaXNhYmxlAQEAAAAvAQBEIwEBAQAAAAEA+QsAAQDzCgAAAAAEYYIIBAAAAAAABgAAAEVuYWJsZQEB" +
           "AAAALwEAQyMBAQEAAAABAPkLAAEA8woAAAAABGGCCAQAAAAAAAoAAABBZGRDb21tZW50AQEAAAAvAQBF" +
           "IwEBAQAAAAEA+QsAAQANCwEAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEAAAAuAESWAgAA" +
           "AAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVudGlmaWVyIGZvciB0" +
           "aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAAAAADAAAAACQAAABU" +
           "aGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEAAAAAAAAAAQH/////AAAA" +
           "ABVgiQgCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBAAAALwEAIyMAFf////8BAQEAAAABACwjAQEBAAABAAAA" +
           "FWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAAARhgggEAAAAAAALAAAAQWNrbm93" +
           "bGVkZ2UBAQAAAC8BAJcjAQEBAAAAAQD5CwABAPAiAQAAABdgqQgCAAAAAAAOAAAASW5wdXRBcmd1bWVu" +
           "dHMBAQAAAC4ARJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAAAAAoAAAAVGhlIGlk" +
           "ZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABDb21tZW50ABX/////" +
           "AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9uLgEAKAEBAAAAAQAA" +
           "AAAAAAABAf////8AAAAAFWCJCAIAAAAAAAsAAABBY3RpdmVTdGF0ZQEBAAAALwEAIyMAFf////8BAQEA" +
           "AAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAABVgiQgC" +
           "AAAAAAAJAAAASW5wdXROb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIAAAAAABMAAABTdXBw" +
           "cmVzc2VkT3JTaGVsdmVkAQEAAAAuAEQAAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABOb3JtYWxT" +
           "dGF0ZQEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region MaintenanceRequiredAlarmTypeState Class
    #if (!OPCUA_EXCLUDE_MaintenanceRequiredAlarmTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class MaintenanceRequiredAlarmTypeState : DeviceHealthDiagnosticAlarmTypeState
    {
        #region Constructors
        /// <remarks />
        public MaintenanceRequiredAlarmTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.MaintenanceRequiredAlarmType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAkAAAATWFp" +
           "bnRlbmFuY2VSZXF1aXJlZEFsYXJtVHlwZUluc3RhbmNlAQF7PQEBez17PQAA/////xsAAAAVYIkIAgAA" +
           "AAAABwAAAEV2ZW50SWQBAQAAAC4ARAAP/////wEB/////wAAAAAVYIkIAgAAAAAACQAAAEV2ZW50VHlw" +
           "ZQEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTm9kZQEBAAAALgBEABH/" +
           "////AQH/////AAAAABVgiQgCAAAAAAAKAAAAU291cmNlTmFtZQEBAAAALgBEAAz/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAAEAAAAVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAsAAABS" +
           "ZWNlaXZlVGltZQEBAAAALgBEAQAmAf////8BAf////8AAAAAFWCJCAIAAAAAAAcAAABNZXNzYWdlAQEA" +
           "AAAuAEQAFf////8BAf////8AAAAAFWCJCAIAAAAAAAgAAABTZXZlcml0eQEBAAAALgBEAAX/////AQH/" +
           "////AAAAABVgiQgCAAAAAAAQAAAAQ29uZGl0aW9uQ2xhc3NJZAEBAAAALgBEABH/////AQH/////AAAA" +
           "ABVgiQgCAAAAAAASAAAAQ29uZGl0aW9uQ2xhc3NOYW1lAQEAAAAuAEQAFf////8BAf////8AAAAAFWCJ" +
           "CAIAAAAAAA0AAABDb25kaXRpb25OYW1lAQEAAAAuAEQADP////8BAf////8AAAAAFWCJCAIAAAAAAAgA" +
           "AABCcmFuY2hJZAEBAAAALgBEABH/////AQH/////AAAAABVgiQgCAAAAAAAGAAAAUmV0YWluAQEAAAAu" +
           "AEQAAf////8BAf////8AAAAAFWCJCAIAAAAAAAwAAABFbmFibGVkU3RhdGUBAQAAAC8BACMjABX/////" +
           "AQEBAAAAAQAsIwABAQAAAQAAABVgiQgCAAAAAAACAAAASWQBAQAAAC4ARAAB/////wEB/////wAAAAAV" +
           "YIkIAgAAAAAABwAAAFF1YWxpdHkBAQAAAC8BACojABP/////AQH/////AQAAABVgiQgCAAAAAAAPAAAA" +
           "U291cmNlVGltZXN0YW1wAQEAAAAuAEQBACYB/////wEB/////wAAAAAVYIkIAgAAAAAADAAAAExhc3RT" +
           "ZXZlcml0eQEBAAAALwEAKiMABf////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3Rh" +
           "bXABAQAAAC4ARAEAJgH/////AQH/////AAAAABVgiQgCAAAAAAAHAAAAQ29tbWVudAEBAAAALwEAKiMA" +
           "Ff////8BAf////8BAAAAFWCJCAIAAAAAAA8AAABTb3VyY2VUaW1lc3RhbXABAQAAAC4ARAEAJgH/////" +
           "AQH/////AAAAABVgiQgCAAAAAAAMAAAAQ2xpZW50VXNlcklkAQEAAAAuAEQADP////8BAf////8AAAAA" +
           "BGGCCAQAAAAAAAcAAABEaXNhYmxlAQEAAAAvAQBEIwEBAQAAAAEA+QsAAQDzCgAAAAAEYYIIBAAAAAAA" +
           "BgAAAEVuYWJsZQEBAAAALwEAQyMBAQEAAAABAPkLAAEA8woAAAAABGGCCAQAAAAAAAoAAABBZGRDb21t" +
           "ZW50AQEAAAAvAQBFIwEBAQAAAAEA+QsAAQANCwEAAAAXYKkIAgAAAAAADgAAAElucHV0QXJndW1lbnRz" +
           "AQEAAAAuAESWAgAAAAEAKgEBRgAAAAcAAABFdmVudElkAA//////AAAAAAMAAAAAKAAAAFRoZSBpZGVu" +
           "dGlmaWVyIGZvciB0aGUgZXZlbnQgdG8gY29tbWVudC4BACoBAUIAAAAHAAAAQ29tbWVudAAV/////wAA" +
           "AAADAAAAACQAAABUaGUgY29tbWVudCB0byBhZGQgdG8gdGhlIGNvbmRpdGlvbi4BACgBAQAAAAEAAAAA" +
           "AAAAAQH/////AAAAABVgiQgCAAAAAAAKAAAAQWNrZWRTdGF0ZQEBAAAALwEAIyMAFf////8BAQEAAAAB" +
           "ACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/////AAAAAARhgggEAAAA" +
           "AAALAAAAQWNrbm93bGVkZ2UBAQAAAC8BAJcjAQEBAAAAAQD5CwABAPAiAQAAABdgqQgCAAAAAAAOAAAA" +
           "SW5wdXRBcmd1bWVudHMBAQAAAC4ARJYCAAAAAQAqAQFGAAAABwAAAEV2ZW50SWQAD/////8AAAAAAwAA" +
           "AAAoAAAAVGhlIGlkZW50aWZpZXIgZm9yIHRoZSBldmVudCB0byBjb21tZW50LgEAKgEBQgAAAAcAAABD" +
           "b21tZW50ABX/////AAAAAAMAAAAAJAAAAFRoZSBjb21tZW50IHRvIGFkZCB0byB0aGUgY29uZGl0aW9u" +
           "LgEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAFWCJCAIAAAAAAAsAAABBY3RpdmVTdGF0ZQEBAAAALwEA" +
           "IyMAFf////8BAQEAAAABACwjAQEBAAABAAAAFWCJCAIAAAAAAAIAAABJZAEBAAAALgBEAAH/////AQH/" +
           "////AAAAABVgiQgCAAAAAAAJAAAASW5wdXROb2RlAQEAAAAuAEQAEf////8BAf////8AAAAAFWCJCAIA" +
           "AAAAABMAAABTdXBwcmVzc2VkT3JTaGVsdmVkAQEAAAAuAEQAAf////8BAf////8AAAAAFWCJCAIAAAAA" +
           "AAsAAABOb3JtYWxTdGF0ZQEBAAAALgBEABH/////AQH/////AAAAAA==";
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

    #region ConfigurableObjectTypeState Class
    #if (!OPCUA_EXCLUDE_ConfigurableObjectTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ConfigurableObjectTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public ConfigurableObjectTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ConfigurableObjectType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAeAAAAQ29u" +
           "ZmlndXJhYmxlT2JqZWN0VHlwZUluc3RhbmNlAQHsAwEB7APsAwAA/////wIAAAAkYIAKAQAAAAEADgAA" +
           "AFN1cHBvcnRlZFR5cGVzAQGMEwMAAAAAcgAAAEZvbGRlciBtYWludGFpbmluZyB0aGUgc2V0IG9mIChz" +
           "dWItdHlwZXMgb2YpIEJhc2VPYmplY3RUeXBlcyB0aGF0IGNhbiBiZSBpbnN0YW50aWF0ZWQgaW4gdGhl" +
           "IENvbmZpZ3VyYWJsZUNvbXBvbmVudAAvAD2MEwAA/////wAAAAAEYMAKAQAAABAAAABPYmplY3RJZGVu" +
           "dGlmaWVyAQASAAAAPE9iamVjdElkZW50aWZpZXI+AQGKFwAvADqKFwAA/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public FolderState SupportedTypes
        {
            get
            {
                return m_supportedTypes;
            }

            set
            {
                if (!Object.ReferenceEquals(m_supportedTypes, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_supportedTypes = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_supportedTypes != null)
            {
                children.Add(m_supportedTypes);
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
                case Opc.Ua.DI.BrowseNames.SupportedTypes:
                {
                    if (createOrReplace)
                    {
                        if (SupportedTypes == null)
                        {
                            if (replacement == null)
                            {
                                SupportedTypes = new FolderState(this);
                            }
                            else
                            {
                                SupportedTypes = (FolderState)replacement;
                            }
                        }
                    }

                    instance = SupportedTypes;
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
        private FolderState m_supportedTypes;
        #endregion
    }
    #endif
    #endregion

    #region FunctionalGroupTypeState Class
    #if (!OPCUA_EXCLUDE_FunctionalGroupTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class FunctionalGroupTypeState : FolderState
    {
        #region Constructors
        /// <remarks />
        public FunctionalGroupTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.FunctionalGroupType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (UIElement != null)
            {
                UIElement.Initialize(context, UIElement_InitializationString);
            }
        }

        #region Initialization String
        private const string UIElement_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////zVgiQoCAAAAAQAJAAAAVUlF" +
           "bGVtZW50AQFjGAMAAAAAMAAAAEEgdXNlciBpbnRlcmZhY2UgZWxlbWVudCBhc3NpZ25lZCB0byB0aGlz" +
           "IGdyb3VwLgAvAQFmGGMYAAAAGP////8BAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAARnVu" +
           "Y3Rpb25hbEdyb3VwVHlwZUluc3RhbmNlAQHtAwEB7QPtAwAA/////wIAAAAkYMAKAQAAAA8AAABHcm91" +
           "cElkZW50aWZpZXIBABEAAAA8R3JvdXBJZGVudGlmaWVyPgEBixcDAAAAAFEAAABBbiBhcHBsaWNhdGlv" +
           "biBzcGVjaWZpYyBmdW5jdGlvbmFsIGdyb3VwIHVzZWQgdG8gb3JnYW5pemUgcGFyYW1ldGVycyBhbmQg" +
           "bWV0aG9kcy4ALwEB7QOLFwAA/////wEAAAA1YIkKAgAAAAEACQAAAFVJRWxlbWVudAEBYhgDAAAAADAA" +
           "AABBIHVzZXIgaW50ZXJmYWNlIGVsZW1lbnQgYXNzaWduZWQgdG8gdGhpcyBncm91cC4ALwEBZhhiGAAA" +
           "ABj/////AQH/////AAAAADVgiQoCAAAAAQAJAAAAVUlFbGVtZW50AQFjGAMAAAAAMAAAAEEgdXNlciBp" +
           "bnRlcmZhY2UgZWxlbWVudCBhc3NpZ25lZCB0byB0aGlzIGdyb3VwLgAvAQFmGGMYAAAAGP////8BAf//" +
           "//8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public UIElementTypeState UIElement
        {
            get
            {
                return m_uIElement;
            }

            set
            {
                if (!Object.ReferenceEquals(m_uIElement, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_uIElement = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_uIElement != null)
            {
                children.Add(m_uIElement);
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
                case Opc.Ua.DI.BrowseNames.UIElement:
                {
                    if (createOrReplace)
                    {
                        if (UIElement == null)
                        {
                            if (replacement == null)
                            {
                                UIElement = new UIElementTypeState(this);
                            }
                            else
                            {
                                UIElement = (UIElementTypeState)replacement;
                            }
                        }
                    }

                    instance = UIElement;
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
        private UIElementTypeState m_uIElement;
        #endregion
    }
    #endif
    #endregion

    #region ProtocolTypeState Class
    #if (!OPCUA_EXCLUDE_ProtocolTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ProtocolTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public ProtocolTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ProtocolType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAUAAAAUHJv" +
           "dG9jb2xUeXBlSW5zdGFuY2UBAe4DAQHuA+4DAAD/////AAAAAA==";
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

    #region UIElementTypeState Class
    #if (!OPCUA_EXCLUDE_UIElementTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class UIElementTypeState : BaseDataVariableState
    {
        #region Constructors
        /// <remarks />
        public UIElementTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.VariableTypes.UIElementType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
        }

        /// <remarks />
        protected override NodeId GetDefaultDataTypeId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DataTypes.BaseDataType, Opc.Ua.Namespaces.OpcUa, namespaceUris);
        }

        /// <remarks />
        protected override int GetDefaultValueRank()
        {
            return ValueRanks.Scalar;
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQICAAAAAQAVAAAAVUlF" +
           "bGVtZW50VHlwZUluc3RhbmNlAQFmGAEBZhhmGAAAABj/////AQH/////AAAAAA==";
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

    #region UIElementTypeState<T> Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public class UIElementTypeState<T> : UIElementTypeState
    {
        #region Constructors
        /// <remarks />
        public UIElementTypeState(NodeState parent) : base(parent)
        {
            Value = default(T);
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context)
        {
            base.Initialize(context);

            Value = default(T);
            DataType = TypeInfo.GetDataTypeId(typeof(T));
            ValueRank = TypeInfo.GetValueRank(typeof(T));
        }

        /// <remarks />
        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }
        #endregion

        #region Public Members
        /// <remarks />
        public new T Value
        {
            get
            {
                return CheckTypeBeforeCast<T>(((BaseVariableState)this).Value, true);
            }

            set
            {
                ((BaseVariableState)this).Value = value;
            }
        }
        #endregion
    }
    #endregion
    #endif
    #endregion

    #region NetworkTypeState Class
    #if (!OPCUA_EXCLUDE_NetworkTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class NetworkTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public NetworkTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.NetworkType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (Lock != null)
            {
                Lock.Initialize(context, Lock_InitializationString);
            }
        }

        #region Initialization String
        private const string Lock_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////yRggAoBAAAAAQAEAAAATG9j" +
           "awEBlhgDAAAAABkAAABVc2VkIHRvIGxvY2sgdGhlIE5ldHdvcmsuAC8BAfQYlhgAAP////8IAAAAFWCJ" +
           "CgIAAAABAAYAAABMb2NrZWQBAWEZAC4ARGEZAAAAAf////8BAf////8AAAAAFWCJCgIAAAABAA0AAABM" +
           "b2NraW5nQ2xpZW50AQGYGAAuAESYGAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQALAAAATG9ja2lu" +
           "Z1VzZXIBAZkYAC4ARJkYAAAADP////8BAf////8AAAAAFWCJCgIAAAABABEAAABSZW1haW5pbmdMb2Nr" +
           "VGltZQEBmhgALgBEmhgAAAEAIgH/////AQH/////AAAAAARhggoEAAAAAQAIAAAASW5pdExvY2sBAZsY" +
           "AC8BAfkYmxgAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGcGAAuAEScGAAA" +
           "lgEAAAABACoBARYAAAAHAAAAQ29udGV4dAAM/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAA" +
           "AAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBnRgALgBEnRgAAJYBAAAAAQAqAQEdAAAADgAA" +
           "AEluaXRMb2NrU3RhdHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhggoEAAAA" +
           "AQAJAAAAUmVuZXdMb2NrAQGeGAAvAQH8GJ4YAAABAf////8BAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRB" +
           "cmd1bWVudHMBAZ8YAC4ARJ8YAACWAQAAAAEAKgEBHgAAAA8AAABSZW5ld0xvY2tTdGF0dXMABv////8A" +
           "AAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAABGGCCgQAAAABAAgAAABFeGl0TG9jawEBoBgALwEB" +
           "/higGAAAAQH/////AQAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGhGAAuAEShGAAAlgEA" +
           "AAABACoBAR0AAAAOAAAARXhpdExvY2tTdGF0dXMABv////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf//" +
           "//8AAAAABGGCCgQAAAABAAkAAABCcmVha0xvY2sBAaIYAC8BAQAZohgAAAEB/////wEAAAAXYKkKAgAA" +
           "AAAADwAAAE91dHB1dEFyZ3VtZW50cwEBoxgALgBEoxgAAJYBAAAAAQAqAQEeAAAADwAAAEJyZWFrTG9j" +
           "a1N0YXR1cwAG/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQATAAAATmV0" +
           "d29ya1R5cGVJbnN0YW5jZQEBZxgBAWcYZxgAAP////8DAAAABGDACgEAAAARAAAAUHJvZmlsZUlkZW50" +
           "aWZpZXIBABMAAAA8UHJvZmlsZUlkZW50aWZpZXI+AQHEGQAvAQHuA8QZAAD/////AAAAACRgwAoBAAAA" +
           "DAAAAENQSWRlbnRpZmllcgEADgAAADxDUElkZW50aWZpZXI+AQFoGAMAAAAAQgAAAFRoZSBDb25uZWN0" +
           "aW9uUG9pbnQocykgdGhhdCBoYXZlIGJlZW4gY29uZmlndXJlZCBmb3IgdGhpcyBOZXR3b3JrLgEBjhcB" +
           "AaQYaBgAAP////8BAAAAJGCACgEAAAABAA4AAABOZXR3b3JrQWRkcmVzcwEBlBgDAAAAACoAAABUaGUg" +
           "YWRkcmVzcyBvZiB0aGUgZGV2aWNlIG9uIHRoaXMgbmV0d29yay4ALwEB7QOUGAAA/////wAAAAAkYIAK" +
           "AQAAAAEABAAAAExvY2sBAZYYAwAAAAAZAAAAVXNlZCB0byBsb2NrIHRoZSBOZXR3b3JrLgAvAQH0GJYY" +
           "AAD/////CAAAABVgiQoCAAAAAQAGAAAATG9ja2VkAQFhGQAuAERhGQAAAAH/////AQH/////AAAAABVg" +
           "iQoCAAAAAQANAAAATG9ja2luZ0NsaWVudAEBmBgALgBEmBgAAAAM/////wEB/////wAAAAAVYIkKAgAA" +
           "AAEACwAAAExvY2tpbmdVc2VyAQGZGAAuAESZGAAAAAz/////AQH/////AAAAABVgiQoCAAAAAQARAAAA" +
           "UmVtYWluaW5nTG9ja1RpbWUBAZoYAC4ARJoYAAABACIB/////wEB/////wAAAAAEYYIKBAAAAAEACAAA" +
           "AEluaXRMb2NrAQGbGAAvAQH5GJsYAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50" +
           "cwEBnBgALgBEnBgAAJYBAAAAAQAqAQEWAAAABwAAAENvbnRleHQADP////8AAAAAAAEAKAEBAAAAAQAA" +
           "AAAAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZ0YAC4ARJ0YAACWAQAA" +
           "AAEAKgEBHQAAAA4AAABJbml0TG9ja1N0YXR1cwAG/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB////" +
           "/wAAAAAEYYIKBAAAAAEACQAAAFJlbmV3TG9jawEBnhgALwEB/BieGAAAAQH/////AQAAABdgqQoCAAAA" +
           "AAAPAAAAT3V0cHV0QXJndW1lbnRzAQGfGAAuAESfGAAAlgEAAAABACoBAR4AAAAPAAAAUmVuZXdMb2Nr" +
           "U3RhdHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhggoEAAAAAQAIAAAARXhp" +
           "dExvY2sBAaAYAC8BAf4YoBgAAAEB/////wEAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEB" +
           "oRgALgBEoRgAAJYBAAAAAQAqAQEdAAAADgAAAEV4aXRMb2NrU3RhdHVzAAb/////AAAAAAABACgBAQAA" +
           "AAEAAAAAAAAAAQH/////AAAAAARhggoEAAAAAQAJAAAAQnJlYWtMb2NrAQGiGAAvAQEAGaIYAAABAf//" +
           "//8BAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAaMYAC4ARKMYAACWAQAAAAEAKgEBHgAA" +
           "AA8AAABCcmVha0xvY2tTdGF0dXMABv////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public LockingServicesTypeState Lock
        {
            get
            {
                return m_lock;
            }

            set
            {
                if (!Object.ReferenceEquals(m_lock, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_lock = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_lock != null)
            {
                children.Add(m_lock);
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
                case Opc.Ua.DI.BrowseNames.Lock:
                {
                    if (createOrReplace)
                    {
                        if (Lock == null)
                        {
                            if (replacement == null)
                            {
                                Lock = new LockingServicesTypeState(this);
                            }
                            else
                            {
                                Lock = (LockingServicesTypeState)replacement;
                            }
                        }
                    }

                    instance = Lock;
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
        private LockingServicesTypeState m_lock;
        #endregion
    }
    #endif
    #endregion

    #region ConnectionPointTypeState Class
    #if (!OPCUA_EXCLUDE_ConnectionPointTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ConnectionPointTypeState : TopologyElementTypeState
    {
        #region Constructors
        /// <remarks />
        public ConnectionPointTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ConnectionPointType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAAQ29u" +
           "bmVjdGlvblBvaW50VHlwZUluc3RhbmNlAQGkGAEBpBikGAAA/////wMAAAAkYIAKAQAAAAEADgAAAE5l" +
           "dHdvcmtBZGRyZXNzAQHSGAMAAAAAKgAAAFRoZSBhZGRyZXNzIG9mIHRoZSBkZXZpY2Ugb24gdGhpcyBu" +
           "ZXR3b3JrLgAvAQHtA9IYAAD/////AAAAAARgwAoBAAAAEQAAAFByb2ZpbGVJZGVudGlmaWVyAQATAAAA" +
           "PFByb2ZpbGVJZGVudGlmaWVyPgEBYxkALwEB7gNjGQAA/////wAAAAAEYMAKAQAAABEAAABOZXR3b3Jr" +
           "SWRlbnRpZmllcgEAEwAAADxOZXR3b3JrSWRlbnRpZmllcj4BAccZAQGOFwEBZxjHGQAA/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public FunctionalGroupTypeState NetworkAddress
        {
            get
            {
                return m_networkAddress;
            }

            set
            {
                if (!Object.ReferenceEquals(m_networkAddress, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_networkAddress = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_networkAddress != null)
            {
                children.Add(m_networkAddress);
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
                case Opc.Ua.DI.BrowseNames.NetworkAddress:
                {
                    if (createOrReplace)
                    {
                        if (NetworkAddress == null)
                        {
                            if (replacement == null)
                            {
                                NetworkAddress = new FunctionalGroupTypeState(this);
                            }
                            else
                            {
                                NetworkAddress = (FunctionalGroupTypeState)replacement;
                            }
                        }
                    }

                    instance = NetworkAddress;
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
        private FunctionalGroupTypeState m_networkAddress;
        #endregion
    }
    #endif
    #endregion

    #region TransferServicesTypeState Class
    #if (!OPCUA_EXCLUDE_TransferServicesTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TransferServicesTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public TransferServicesTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.TransferServicesType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAcAAAAVHJh" +
           "bnNmZXJTZXJ2aWNlc1R5cGVJbnN0YW5jZQEBfhkBAX4ZfhkAAP////8DAAAABGGCCgQAAAABABAAAABU" +
           "cmFuc2ZlclRvRGV2aWNlAQF/GQAvAQF/GX8ZAAABAf////8BAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRB" +
           "cmd1bWVudHMBAYAZAC4ARIAZAACWAgAAAAEAKgEBGQAAAAoAAABUcmFuc2ZlcklEAAb/////AAAAAAAB" +
           "ACoBASEAAAASAAAASW5pdFRyYW5zZmVyU3RhdHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/" +
           "////AAAAAARhggoEAAAAAQASAAAAVHJhbnNmZXJGcm9tRGV2aWNlAQGBGQAvAQGBGYEZAAABAf////8B" +
           "AAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAYIZAC4ARIIZAACWAgAAAAEAKgEBGQAAAAoA" +
           "AABUcmFuc2ZlcklEAAb/////AAAAAAABACoBASEAAAASAAAASW5pdFRyYW5zZmVyU3RhdHVzAAb/////" +
           "AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhggoEAAAAAQAXAAAARmV0Y2hUcmFuc2ZlclJl" +
           "c3VsdERhdGEBAYMZAC8BAYMZgxkAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRz" +
           "AQGEGQAuAESEGQAAlgQAAAABACoBARkAAAAKAAAAVHJhbnNmZXJJRAAG/////wAAAAAAAQAqAQEdAAAA" +
           "DgAAAFNlcXVlbmNlTnVtYmVyAAb/////AAAAAAABACoBASoAAAAbAAAATWF4UGFyYW1ldGVyUmVzdWx0" +
           "c1RvUmV0dXJuAAb/////AAAAAAABACoBAR4AAAAPAAAAT21pdEdvb2RSZXN1bHRzAAH/////AAAAAAAB" +
           "ACgBAQAAAAEAAAAAAAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGFGQAu" +
           "AESFGQAAlgEAAAABACoBAR4AAAAPAAAARmV0Y2hSZXN1bHREYXRhABb/////AAAAAAABACgBAQAAAAEA" +
           "AAAAAAAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public TransferToDeviceMethodState TransferToDevice
        {
            get
            {
                return m_transferToDeviceMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_transferToDeviceMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_transferToDeviceMethod = value;
            }
        }

        /// <remarks />
        public TransferFromDeviceMethodState TransferFromDevice
        {
            get
            {
                return m_transferFromDeviceMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_transferFromDeviceMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_transferFromDeviceMethod = value;
            }
        }

        /// <remarks />
        public FetchTransferResultDataMethodState FetchTransferResultData
        {
            get
            {
                return m_fetchTransferResultDataMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_fetchTransferResultDataMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_fetchTransferResultDataMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_transferToDeviceMethod != null)
            {
                children.Add(m_transferToDeviceMethod);
            }

            if (m_transferFromDeviceMethod != null)
            {
                children.Add(m_transferFromDeviceMethod);
            }

            if (m_fetchTransferResultDataMethod != null)
            {
                children.Add(m_fetchTransferResultDataMethod);
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
                case Opc.Ua.DI.BrowseNames.TransferToDevice:
                {
                    if (createOrReplace)
                    {
                        if (TransferToDevice == null)
                        {
                            if (replacement == null)
                            {
                                TransferToDevice = new TransferToDeviceMethodState(this);
                            }
                            else
                            {
                                TransferToDevice = (TransferToDeviceMethodState)replacement;
                            }
                        }
                    }

                    instance = TransferToDevice;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.TransferFromDevice:
                {
                    if (createOrReplace)
                    {
                        if (TransferFromDevice == null)
                        {
                            if (replacement == null)
                            {
                                TransferFromDevice = new TransferFromDeviceMethodState(this);
                            }
                            else
                            {
                                TransferFromDevice = (TransferFromDeviceMethodState)replacement;
                            }
                        }
                    }

                    instance = TransferFromDevice;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.FetchTransferResultData:
                {
                    if (createOrReplace)
                    {
                        if (FetchTransferResultData == null)
                        {
                            if (replacement == null)
                            {
                                FetchTransferResultData = new FetchTransferResultDataMethodState(this);
                            }
                            else
                            {
                                FetchTransferResultData = (FetchTransferResultDataMethodState)replacement;
                            }
                        }
                    }

                    instance = FetchTransferResultData;
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
        private TransferToDeviceMethodState m_transferToDeviceMethod;
        private TransferFromDeviceMethodState m_transferFromDeviceMethod;
        private FetchTransferResultDataMethodState m_fetchTransferResultDataMethod;
        #endregion
    }
    #endif
    #endregion

    #region LockingServicesTypeState Class
    #if (!OPCUA_EXCLUDE_LockingServicesTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class LockingServicesTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public LockingServicesTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.LockingServicesType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAATG9j" +
           "a2luZ1NlcnZpY2VzVHlwZUluc3RhbmNlAQH0GAEB9Bj0GAAA/////wkAAAAVYKkKAgAAAAAAGQAAAERl" +
           "ZmF1bHRJbnN0YW5jZUJyb3dzZU5hbWUBARI+AC4ARBI+AAAUAQAEAAAATG9jawAU/////wEB/////wAA" +
           "AAAVYIkKAgAAAAEABgAAAExvY2tlZAEBhhkALgBEhhkAAAAB/////wEB/////wAAAAAVYIkKAgAAAAEA" +
           "DQAAAExvY2tpbmdDbGllbnQBAfYYAC4ARPYYAAAADP////8BAf////8AAAAAFWCJCgIAAAABAAsAAABM" +
           "b2NraW5nVXNlcgEB9xgALgBE9xgAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEQAAAFJlbWFpbmlu" +
           "Z0xvY2tUaW1lAQH4GAAuAET4GAAAAQAiAf////8BAf////8AAAAABGGCCgQAAAABAAgAAABJbml0TG9j" +
           "awEB+RgALwEB+Rj5GAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAfoYAC4A" +
           "RPoYAACWAQAAAAEAKgEBFgAAAAcAAABDb250ZXh0AAz/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/" +
           "////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQH7GAAuAET7GAAAlgEAAAABACoBAR0A" +
           "AAAOAAAASW5pdExvY2tTdGF0dXMABv////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAABGGC" +
           "CgQAAAABAAkAAABSZW5ld0xvY2sBAfwYAC8BAfwY/BgAAAEB/////wEAAAAXYKkKAgAAAAAADwAAAE91" +
           "dHB1dEFyZ3VtZW50cwEB/RgALgBE/RgAAJYBAAAAAQAqAQEeAAAADwAAAFJlbmV3TG9ja1N0YXR1cwAG" +
           "/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAEACAAAAEV4aXRMb2NrAQH+" +
           "GAAvAQH+GP4YAAABAf////8BAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAf8YAC4ARP8Y" +
           "AACWAQAAAAEAKgEBHQAAAA4AAABFeGl0TG9ja1N0YXR1cwAG/////wAAAAAAAQAoAQEAAAABAAAAAAAA" +
           "AAEB/////wAAAAAEYYIKBAAAAAEACQAAAEJyZWFrTG9jawEBABkALwEBABkAGQAAAQH/////AQAAABdg" +
           "qQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQEBGQAuAEQBGQAAlgEAAAABACoBAR4AAAAPAAAAQnJl" +
           "YWtMb2NrU3RhdHVzAAb/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<bool> Locked
        {
            get
            {
                return m_locked;
            }

            set
            {
                if (!Object.ReferenceEquals(m_locked, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_locked = value;
            }
        }

        /// <remarks />
        public PropertyState<string> LockingClient
        {
            get
            {
                return m_lockingClient;
            }

            set
            {
                if (!Object.ReferenceEquals(m_lockingClient, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_lockingClient = value;
            }
        }

        /// <remarks />
        public PropertyState<string> LockingUser
        {
            get
            {
                return m_lockingUser;
            }

            set
            {
                if (!Object.ReferenceEquals(m_lockingUser, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_lockingUser = value;
            }
        }

        /// <remarks />
        public PropertyState<double> RemainingLockTime
        {
            get
            {
                return m_remainingLockTime;
            }

            set
            {
                if (!Object.ReferenceEquals(m_remainingLockTime, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_remainingLockTime = value;
            }
        }

        /// <remarks />
        public InitLockMethodState InitLock
        {
            get
            {
                return m_initLockMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_initLockMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_initLockMethod = value;
            }
        }

        /// <remarks />
        public RenewLockMethodState RenewLock
        {
            get
            {
                return m_renewLockMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_renewLockMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_renewLockMethod = value;
            }
        }

        /// <remarks />
        public ExitLockMethodState ExitLock
        {
            get
            {
                return m_exitLockMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_exitLockMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_exitLockMethod = value;
            }
        }

        /// <remarks />
        public BreakLockMethodState BreakLock
        {
            get
            {
                return m_breakLockMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_breakLockMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_breakLockMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_locked != null)
            {
                children.Add(m_locked);
            }

            if (m_lockingClient != null)
            {
                children.Add(m_lockingClient);
            }

            if (m_lockingUser != null)
            {
                children.Add(m_lockingUser);
            }

            if (m_remainingLockTime != null)
            {
                children.Add(m_remainingLockTime);
            }

            if (m_initLockMethod != null)
            {
                children.Add(m_initLockMethod);
            }

            if (m_renewLockMethod != null)
            {
                children.Add(m_renewLockMethod);
            }

            if (m_exitLockMethod != null)
            {
                children.Add(m_exitLockMethod);
            }

            if (m_breakLockMethod != null)
            {
                children.Add(m_breakLockMethod);
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
                case Opc.Ua.DI.BrowseNames.Locked:
                {
                    if (createOrReplace)
                    {
                        if (Locked == null)
                        {
                            if (replacement == null)
                            {
                                Locked = new PropertyState<bool>(this);
                            }
                            else
                            {
                                Locked = (PropertyState<bool>)replacement;
                            }
                        }
                    }

                    instance = Locked;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.LockingClient:
                {
                    if (createOrReplace)
                    {
                        if (LockingClient == null)
                        {
                            if (replacement == null)
                            {
                                LockingClient = new PropertyState<string>(this);
                            }
                            else
                            {
                                LockingClient = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = LockingClient;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.LockingUser:
                {
                    if (createOrReplace)
                    {
                        if (LockingUser == null)
                        {
                            if (replacement == null)
                            {
                                LockingUser = new PropertyState<string>(this);
                            }
                            else
                            {
                                LockingUser = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = LockingUser;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.RemainingLockTime:
                {
                    if (createOrReplace)
                    {
                        if (RemainingLockTime == null)
                        {
                            if (replacement == null)
                            {
                                RemainingLockTime = new PropertyState<double>(this);
                            }
                            else
                            {
                                RemainingLockTime = (PropertyState<double>)replacement;
                            }
                        }
                    }

                    instance = RemainingLockTime;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.InitLock:
                {
                    if (createOrReplace)
                    {
                        if (InitLock == null)
                        {
                            if (replacement == null)
                            {
                                InitLock = new InitLockMethodState(this);
                            }
                            else
                            {
                                InitLock = (InitLockMethodState)replacement;
                            }
                        }
                    }

                    instance = InitLock;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.RenewLock:
                {
                    if (createOrReplace)
                    {
                        if (RenewLock == null)
                        {
                            if (replacement == null)
                            {
                                RenewLock = new RenewLockMethodState(this);
                            }
                            else
                            {
                                RenewLock = (RenewLockMethodState)replacement;
                            }
                        }
                    }

                    instance = RenewLock;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ExitLock:
                {
                    if (createOrReplace)
                    {
                        if (ExitLock == null)
                        {
                            if (replacement == null)
                            {
                                ExitLock = new ExitLockMethodState(this);
                            }
                            else
                            {
                                ExitLock = (ExitLockMethodState)replacement;
                            }
                        }
                    }

                    instance = ExitLock;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.BreakLock:
                {
                    if (createOrReplace)
                    {
                        if (BreakLock == null)
                        {
                            if (replacement == null)
                            {
                                BreakLock = new BreakLockMethodState(this);
                            }
                            else
                            {
                                BreakLock = (BreakLockMethodState)replacement;
                            }
                        }
                    }

                    instance = BreakLock;
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
        private PropertyState<bool> m_locked;
        private PropertyState<string> m_lockingClient;
        private PropertyState<string> m_lockingUser;
        private PropertyState<double> m_remainingLockTime;
        private InitLockMethodState m_initLockMethod;
        private RenewLockMethodState m_renewLockMethod;
        private ExitLockMethodState m_exitLockMethod;
        private BreakLockMethodState m_breakLockMethod;
        #endregion
    }
    #endif
    #endregion

    #region SoftwareUpdateTypeState Class
    #if (!OPCUA_EXCLUDE_SoftwareUpdateTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SoftwareUpdateTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public SoftwareUpdateTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.SoftwareUpdateType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (Loading != null)
            {
                Loading.Initialize(context, Loading_InitializationString);
            }

            if (PrepareForUpdate != null)
            {
                PrepareForUpdate.Initialize(context, PrepareForUpdate_InitializationString);
            }

            if (Installation != null)
            {
                Installation.Initialize(context, Installation_InitializationString);
            }

            if (PowerCycle != null)
            {
                PowerCycle.Initialize(context, PowerCycle_InitializationString);
            }

            if (Confirmation != null)
            {
                Confirmation.Initialize(context, Confirmation_InitializationString);
            }

            if (Parameters != null)
            {
                Parameters.Initialize(context, Parameters_InitializationString);
            }

            if (UpdateStatus != null)
            {
                UpdateStatus.Initialize(context, UpdateStatus_InitializationString);
            }

            if (VendorErrorCode != null)
            {
                VendorErrorCode.Initialize(context, VendorErrorCode_InitializationString);
            }
        }

        #region Initialization String
        private const string Loading_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAHAAAATG9h" +
           "ZGluZwEBAgAALwEBhwACAAAA/////wAAAAA=";

        private const string PrepareForUpdate_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAQAAAAUHJl" +
           "cGFyZUZvclVwZGF0ZQEBBAAALwEB1QAEAAAA/////wMAAAAVYIkKAgAAAAAADAAAAEN1cnJlbnRTdGF0" +
           "ZQEBBQAALwEAyAoFAAAAABX/////AQH/////AQAAABVgiQoCAAAAAAACAAAASWQBAQYAAC4ARAYAAAAA" +
           "Ef////8BAf////8AAAAABGGCCgQAAAABAAcAAABQcmVwYXJlAQETAAAvAQHkABMAAAABAf////8AAAAA" +
           "BGGCCgQAAAABAAUAAABBYm9ydAEBFAAALwEB5QAUAAAAAQH/////AAAAAA==";

        private const string Installation_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAMAAAASW5z" +
           "dGFsbGF0aW9uAQEoAAAvAQH5ACgAAAD/////AgAAABVgiQoCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQEp" +
           "AAAvAQDICikAAAAAFf////8BAf////8BAAAAFWCJCgIAAAAAAAIAAABJZAEBKgAALgBEKgAAAAAR////" +
           "/wEB/////wAAAAAEYYIKBAAAAAEABgAAAFJlc3VtZQEBPQAALwEBDgE9AAAAAQH/////AAAAAA==";

        private const string PowerCycle_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAKAAAAUG93" +
           "ZXJDeWNsZQEBTAAALwEBHQFMAAAA/////wEAAAAVYIkKAgAAAAAADAAAAEN1cnJlbnRTdGF0ZQEBTQAA" +
           "LwEAyApNAAAAABX/////AQH/////AQAAABVgiQoCAAAAAAACAAAASWQBAU4AAC4ARE4AAAAAEf////8B" +
           "Af////8AAAAA";

        private const string Confirmation_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAMAAAAQ29u" +
           "ZmlybWF0aW9uAQFiAAAvAQEzAWIAAAD/////AwAAABVgiQoCAAAAAAAMAAAAQ3VycmVudFN0YXRlAQFj" +
           "AAAvAQDICmMAAAAAFf////8BAf////8BAAAAFWCJCgIAAAAAAAIAAABJZAEBZAAALgBEZAAAAAAR////" +
           "/wEB/////wAAAAAEYYIKBAAAAAEABwAAAENvbmZpcm0BAXAAAC8BAUEBcAAAAAEB/////wAAAAAVYIkK" +
           "AgAAAAEAEwAAAENvbmZpcm1hdGlvblRpbWVvdXQBAXEAAC8AP3EAAAABACIB/////wEB/////wAAAAA=";

        private const string Parameters_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAKAAAAUGFy" +
           "YW1ldGVycwEBegAALwEAgD16AAAA/////wQAAAAVYIkKAgAAAAAAFwAAAENsaWVudFByb2Nlc3NpbmdU" +
           "aW1lb3V0AQF7AAAuAER7AAAAAQAiAf////8BAf////8AAAAABGGCCgQAAAAAABMAAABHZW5lcmF0ZUZp" +
           "bGVGb3JSZWFkAQF8AAAvAQCCPXwAAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50" +
           "cwEBfQAALgBEfQAAAJYBAAAAAQAqAQEeAAAADwAAAEdlbmVyYXRlT3B0aW9ucwAY/////wAAAAAAAQAo" +
           "AQEAAAABAAAAAAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBfgAALgBE" +
           "fgAAAJYDAAAAAQAqAQEZAAAACgAAAEZpbGVOb2RlSWQAEf////8AAAAAAAEAKgEBGQAAAAoAAABGaWxl" +
           "SGFuZGxlAAf/////AAAAAAABACoBASUAAAAWAAAAQ29tcGxldGlvblN0YXRlTWFjaGluZQAR/////wAA" +
           "AAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAAAFAAAAEdlbmVyYXRlRmlsZUZvcldy" +
           "aXRlAQF/AAAvAQCFPX8AAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBgAAA" +
           "LgBEgAAAAJYBAAAAAQAqAQEeAAAADwAAAEdlbmVyYXRlT3B0aW9ucwAY/////wAAAAAAAQAoAQEAAAAB" +
           "AAAAAAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBgQAALgBEgQAAAJYC" +
           "AAAAAQAqAQEZAAAACgAAAEZpbGVOb2RlSWQAEf////8AAAAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxl" +
           "AAf/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhggoEAAAAAAAOAAAAQ2xvc2VBbmRD" +
           "b21taXQBAYIAAC8BAIc9ggAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGD" +
           "AAAuAESDAAAAlgEAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAABAAAA" +
           "AAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBhAAALgBEhAAAAJYBAAAA" +
           "AQAqAQElAAAAFgAAAENvbXBsZXRpb25TdGF0ZU1hY2hpbmUAEf////8AAAAAAAEAKAEBAAAAAQAAAAAA" +
           "AAABAf////8AAAAA";

        private const string UpdateStatus_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAAVXBk" +
           "YXRlU3RhdHVzAQGFAAAvAD+FAAAAABX/////AQH/////AAAAAA==";

        private const string VendorErrorCode_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAAVmVu" +
           "ZG9yRXJyb3JDb2RlAQGSAQAvAD+SAQAAAAb/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAaAAAAU29m" +
           "dHdhcmVVcGRhdGVUeXBlSW5zdGFuY2UBAQEAAQEBAAEAAAD/////CQAAAARggAoBAAAAAQAHAAAATG9h" +
           "ZGluZwEBAgAALwEBhwACAAAA/////wAAAAAEYIAKAQAAAAEAEAAAAFByZXBhcmVGb3JVcGRhdGUBAQQA" +
           "AC8BAdUABAAAAP////8DAAAAFWCJCgIAAAAAAAwAAABDdXJyZW50U3RhdGUBAQUAAC8BAMgKBQAAAAAV" +
           "/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElkAQEGAAAuAEQGAAAAABH/////AQH/////AAAAAARh" +
           "ggoEAAAAAQAHAAAAUHJlcGFyZQEBEwAALwEB5AATAAAAAQH/////AAAAAARhggoEAAAAAQAFAAAAQWJv" +
           "cnQBARQAAC8BAeUAFAAAAAEB/////wAAAAAEYIAKAQAAAAEADAAAAEluc3RhbGxhdGlvbgEBKAAALwEB" +
           "+QAoAAAA/////wIAAAAVYIkKAgAAAAAADAAAAEN1cnJlbnRTdGF0ZQEBKQAALwEAyAopAAAAABX/////" +
           "AQH/////AQAAABVgiQoCAAAAAAACAAAASWQBASoAAC4ARCoAAAAAEf////8BAf////8AAAAABGGCCgQA" +
           "AAABAAYAAABSZXN1bWUBAT0AAC8BAQ4BPQAAAAEB/////wAAAAAEYIAKAQAAAAEACgAAAFBvd2VyQ3lj" +
           "bGUBAUwAAC8BAR0BTAAAAP////8BAAAAFWCJCgIAAAAAAAwAAABDdXJyZW50U3RhdGUBAU0AAC8BAMgK" +
           "TQAAAAAV/////wEB/////wEAAAAVYIkKAgAAAAAAAgAAAElkAQFOAAAuAEROAAAAABH/////AQH/////" +
           "AAAAAARggAoBAAAAAQAMAAAAQ29uZmlybWF0aW9uAQFiAAAvAQEzAWIAAAD/////AwAAABVgiQoCAAAA" +
           "AAAMAAAAQ3VycmVudFN0YXRlAQFjAAAvAQDICmMAAAAAFf////8BAf////8BAAAAFWCJCgIAAAAAAAIA" +
           "AABJZAEBZAAALgBEZAAAAAAR/////wEB/////wAAAAAEYYIKBAAAAAEABwAAAENvbmZpcm0BAXAAAC8B" +
           "AUEBcAAAAAEB/////wAAAAAVYIkKAgAAAAEAEwAAAENvbmZpcm1hdGlvblRpbWVvdXQBAXEAAC8AP3EA" +
           "AAABACIB/////wEB/////wAAAAAEYIAKAQAAAAEACgAAAFBhcmFtZXRlcnMBAXoAAC8BAIA9egAAAP//" +
           "//8EAAAAFWCJCgIAAAAAABcAAABDbGllbnRQcm9jZXNzaW5nVGltZW91dAEBewAALgBEewAAAAEAIgH/" +
           "////AQH/////AAAAAARhggoEAAAAAAATAAAAR2VuZXJhdGVGaWxlRm9yUmVhZAEBfAAALwEAgj18AAAA" +
           "AQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAX0AAC4ARH0AAACWAQAAAAEAKgEB" +
           "HgAAAA8AAABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAA" +
           "F2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAX4AAC4ARH4AAACWAwAAAAEAKgEBGQAAAAoAAABG" +
           "aWxlTm9kZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAqAQEl" +
           "AAAAFgAAAENvbXBsZXRpb25TdGF0ZU1hY2hpbmUAEf////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf//" +
           "//8AAAAABGGCCgQAAAAAABQAAABHZW5lcmF0ZUZpbGVGb3JXcml0ZQEBfwAALwEAhT1/AAAAAQH/////" +
           "AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAYAAAC4ARIAAAACWAQAAAAEAKgEBHgAAAA8A" +
           "AABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2CpCgIA" +
           "AAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAYEAAC4ARIEAAACWAgAAAAEAKgEBGQAAAAoAAABGaWxlTm9k" +
           "ZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAABAAAA" +
           "AAAAAAEB/////wAAAAAEYYIKBAAAAAAADgAAAENsb3NlQW5kQ29tbWl0AQGCAAAvAQCHPYIAAAABAf//" +
           "//8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBgwAALgBEgwAAAJYBAAAAAQAqAQEZAAAA" +
           "CgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2CpCgIAAAAA" +
           "AA8AAABPdXRwdXRBcmd1bWVudHMBAYQAAC4ARIQAAACWAQAAAAEAKgEBJQAAABYAAABDb21wbGV0aW9u" +
           "U3RhdGVNYWNoaW5lABH/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABVgiQoCAAAAAQAM" +
           "AAAAVXBkYXRlU3RhdHVzAQGFAAAvAD+FAAAAABX/////AQH/////AAAAABVgiQoCAAAAAQAPAAAAVmVu" +
           "ZG9yRXJyb3JDb2RlAQGSAQAvAD+SAQAAAAb/////AQH/////AAAAABVgqQoCAAAAAAAZAAAARGVmYXVs" +
           "dEluc3RhbmNlQnJvd3NlTmFtZQEBhgAALgBEhgAAABQBAA4AAABTb2Z0d2FyZVVwZGF0ZQAU/////wEB" +
           "/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public SoftwareLoadingTypeState Loading
        {
            get
            {
                return m_loading;
            }

            set
            {
                if (!Object.ReferenceEquals(m_loading, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_loading = value;
            }
        }

        /// <remarks />
        public PrepareForUpdateStateMachineTypeState PrepareForUpdate
        {
            get
            {
                return m_prepareForUpdate;
            }

            set
            {
                if (!Object.ReferenceEquals(m_prepareForUpdate, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_prepareForUpdate = value;
            }
        }

        /// <remarks />
        public InstallationStateMachineTypeState Installation
        {
            get
            {
                return m_installation;
            }

            set
            {
                if (!Object.ReferenceEquals(m_installation, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_installation = value;
            }
        }

        /// <remarks />
        public PowerCycleStateMachineTypeState PowerCycle
        {
            get
            {
                return m_powerCycle;
            }

            set
            {
                if (!Object.ReferenceEquals(m_powerCycle, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_powerCycle = value;
            }
        }

        /// <remarks />
        public ConfirmationStateMachineTypeState Confirmation
        {
            get
            {
                return m_confirmation;
            }

            set
            {
                if (!Object.ReferenceEquals(m_confirmation, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_confirmation = value;
            }
        }

        /// <remarks />
        public TemporaryFileTransferState Parameters
        {
            get
            {
                return m_parameters;
            }

            set
            {
                if (!Object.ReferenceEquals(m_parameters, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_parameters = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<LocalizedText> UpdateStatus
        {
            get
            {
                return m_updateStatus;
            }

            set
            {
                if (!Object.ReferenceEquals(m_updateStatus, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_updateStatus = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<int> VendorErrorCode
        {
            get
            {
                return m_vendorErrorCode;
            }

            set
            {
                if (!Object.ReferenceEquals(m_vendorErrorCode, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_vendorErrorCode = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_loading != null)
            {
                children.Add(m_loading);
            }

            if (m_prepareForUpdate != null)
            {
                children.Add(m_prepareForUpdate);
            }

            if (m_installation != null)
            {
                children.Add(m_installation);
            }

            if (m_powerCycle != null)
            {
                children.Add(m_powerCycle);
            }

            if (m_confirmation != null)
            {
                children.Add(m_confirmation);
            }

            if (m_parameters != null)
            {
                children.Add(m_parameters);
            }

            if (m_updateStatus != null)
            {
                children.Add(m_updateStatus);
            }

            if (m_vendorErrorCode != null)
            {
                children.Add(m_vendorErrorCode);
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
                case Opc.Ua.DI.BrowseNames.Loading:
                {
                    if (createOrReplace)
                    {
                        if (Loading == null)
                        {
                            if (replacement == null)
                            {
                                Loading = new SoftwareLoadingTypeState(this);
                            }
                            else
                            {
                                Loading = (SoftwareLoadingTypeState)replacement;
                            }
                        }
                    }

                    instance = Loading;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.PrepareForUpdate:
                {
                    if (createOrReplace)
                    {
                        if (PrepareForUpdate == null)
                        {
                            if (replacement == null)
                            {
                                PrepareForUpdate = new PrepareForUpdateStateMachineTypeState(this);
                            }
                            else
                            {
                                PrepareForUpdate = (PrepareForUpdateStateMachineTypeState)replacement;
                            }
                        }
                    }

                    instance = PrepareForUpdate;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Installation:
                {
                    if (createOrReplace)
                    {
                        if (Installation == null)
                        {
                            if (replacement == null)
                            {
                                Installation = new InstallationStateMachineTypeState(this);
                            }
                            else
                            {
                                Installation = (InstallationStateMachineTypeState)replacement;
                            }
                        }
                    }

                    instance = Installation;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.PowerCycle:
                {
                    if (createOrReplace)
                    {
                        if (PowerCycle == null)
                        {
                            if (replacement == null)
                            {
                                PowerCycle = new PowerCycleStateMachineTypeState(this);
                            }
                            else
                            {
                                PowerCycle = (PowerCycleStateMachineTypeState)replacement;
                            }
                        }
                    }

                    instance = PowerCycle;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Confirmation:
                {
                    if (createOrReplace)
                    {
                        if (Confirmation == null)
                        {
                            if (replacement == null)
                            {
                                Confirmation = new ConfirmationStateMachineTypeState(this);
                            }
                            else
                            {
                                Confirmation = (ConfirmationStateMachineTypeState)replacement;
                            }
                        }
                    }

                    instance = Confirmation;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Parameters:
                {
                    if (createOrReplace)
                    {
                        if (Parameters == null)
                        {
                            if (replacement == null)
                            {
                                Parameters = new TemporaryFileTransferState(this);
                            }
                            else
                            {
                                Parameters = (TemporaryFileTransferState)replacement;
                            }
                        }
                    }

                    instance = Parameters;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.UpdateStatus:
                {
                    if (createOrReplace)
                    {
                        if (UpdateStatus == null)
                        {
                            if (replacement == null)
                            {
                                UpdateStatus = new BaseDataVariableState<LocalizedText>(this);
                            }
                            else
                            {
                                UpdateStatus = (BaseDataVariableState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = UpdateStatus;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.VendorErrorCode:
                {
                    if (createOrReplace)
                    {
                        if (VendorErrorCode == null)
                        {
                            if (replacement == null)
                            {
                                VendorErrorCode = new BaseDataVariableState<int>(this);
                            }
                            else
                            {
                                VendorErrorCode = (BaseDataVariableState<int>)replacement;
                            }
                        }
                    }

                    instance = VendorErrorCode;
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
        private SoftwareLoadingTypeState m_loading;
        private PrepareForUpdateStateMachineTypeState m_prepareForUpdate;
        private InstallationStateMachineTypeState m_installation;
        private PowerCycleStateMachineTypeState m_powerCycle;
        private ConfirmationStateMachineTypeState m_confirmation;
        private TemporaryFileTransferState m_parameters;
        private BaseDataVariableState<LocalizedText> m_updateStatus;
        private BaseDataVariableState<int> m_vendorErrorCode;
        #endregion
    }
    #endif
    #endregion

    #region SoftwareLoadingTypeState Class
    #if (!OPCUA_EXCLUDE_SoftwareLoadingTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SoftwareLoadingTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public SoftwareLoadingTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.SoftwareLoadingType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (UpdateKey != null)
            {
                UpdateKey.Initialize(context, UpdateKey_InitializationString);
            }
        }

        #region Initialization String
        private const string UpdateKey_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAJAAAAVXBk" +
           "YXRlS2V5AQGIAAAvAD+IAAAAAAz/////AQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAAU29m" +
           "dHdhcmVMb2FkaW5nVHlwZUluc3RhbmNlAQGHAAEBhwCHAAAA/////wEAAAAVYIkKAgAAAAEACQAAAFVw" +
           "ZGF0ZUtleQEBiAAALwA/iAAAAAAM/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<string> UpdateKey
        {
            get
            {
                return m_updateKey;
            }

            set
            {
                if (!Object.ReferenceEquals(m_updateKey, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_updateKey = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_updateKey != null)
            {
                children.Add(m_updateKey);
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
                case Opc.Ua.DI.BrowseNames.UpdateKey:
                {
                    if (createOrReplace)
                    {
                        if (UpdateKey == null)
                        {
                            if (replacement == null)
                            {
                                UpdateKey = new BaseDataVariableState<string>(this);
                            }
                            else
                            {
                                UpdateKey = (BaseDataVariableState<string>)replacement;
                            }
                        }
                    }

                    instance = UpdateKey;
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
        private BaseDataVariableState<string> m_updateKey;
        #endregion
    }
    #endif
    #endregion

    #region PackageLoadingTypeState Class
    #if (!OPCUA_EXCLUDE_PackageLoadingTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PackageLoadingTypeState : SoftwareLoadingTypeState
    {
        #region Constructors
        /// <remarks />
        public PackageLoadingTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.PackageLoadingType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (WriteBlockSize != null)
            {
                WriteBlockSize.Initialize(context, WriteBlockSize_InitializationString);
            }
        }

        #region Initialization String
        private const string WriteBlockSize_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAOAAAAV3Jp" +
           "dGVCbG9ja1NpemUBAZgAAC4ARJgAAAAAB/////8BAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAaAAAAUGFj" +
           "a2FnZUxvYWRpbmdUeXBlSW5zdGFuY2UBAYkAAQGJAIkAAAD/////BAAAAARggAoBAAAAAQAOAAAAQ3Vy" +
           "cmVudFZlcnNpb24BAYsAAC8BAdQAiwAAAP////8DAAAAFWCJCgIAAAABAAwAAABNYW51ZmFjdHVyZXIB" +
           "AVkBAC4ARFkBAAAAFf////8BAf////8AAAAAFWCJCgIAAAABAA8AAABNYW51ZmFjdHVyZXJVcmkBAVoB" +
           "AC4ARFoBAAAADP////8BAf////8AAAAAFWCJCgIAAAABABAAAABTb2Z0d2FyZVJldmlzaW9uAQFbAQAu" +
           "AERbAQAAAAz/////AQH/////AAAAAARggAoBAAAAAQAMAAAARmlsZVRyYW5zZmVyAQGMAAAvAQCAPYwA" +
           "AAD/////BAAAABVgiQoCAAAAAAAXAAAAQ2xpZW50UHJvY2Vzc2luZ1RpbWVvdXQBAY0AAC4ARI0AAAAB" +
           "ACIB/////wEB/////wAAAAAEYYIKBAAAAAAAEwAAAEdlbmVyYXRlRmlsZUZvclJlYWQBAY4AAC8BAII9" +
           "jgAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGPAAAuAESPAAAAlgEAAAAB" +
           "ACoBAR4AAAAPAAAAR2VuZXJhdGVPcHRpb25zABj/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////" +
           "AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGQAAAuAESQAAAAlgMAAAABACoBARkAAAAK" +
           "AAAARmlsZU5vZGVJZAAR/////wAAAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEA" +
           "KgEBJQAAABYAAABDb21wbGV0aW9uU3RhdGVNYWNoaW5lABH/////AAAAAAABACgBAQAAAAEAAAAAAAAA" +
           "AQH/////AAAAAARhggoEAAAAAAAUAAAAR2VuZXJhdGVGaWxlRm9yV3JpdGUBAZEAAC8BAIU9kQAAAAEB" +
           "/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQGSAAAuAESSAAAAlgEAAAABACoBAR4A" +
           "AAAPAAAAR2VuZXJhdGVPcHRpb25zABj/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABdg" +
           "qQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGTAAAuAESTAAAAlgIAAAABACoBARkAAAAKAAAARmls" +
           "ZU5vZGVJZAAR/////wAAAAAAAQAqAQEZAAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAA" +
           "AQAAAAAAAAABAf////8AAAAABGGCCgQAAAAAAA4AAABDbG9zZUFuZENvbW1pdAEBlAAALwEAhz2UAAAA" +
           "AQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAZUAAC4ARJUAAACWAQAAAAEAKgEB" +
           "GQAAAAoAAABGaWxlSGFuZGxlAAf/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABdgqQoC" +
           "AAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQGWAAAuAESWAAAAlgEAAAABACoBASUAAAAWAAAAQ29tcGxl" +
           "dGlvblN0YXRlTWFjaGluZQAR/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAVYIkKAgAA" +
           "AAEADAAAAEVycm9yTWVzc2FnZQEBlwAALwA/lwAAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEADgAA" +
           "AFdyaXRlQmxvY2tTaXplAQGYAAAuAESYAAAAAAf/////AQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public SoftwareVersionTypeState CurrentVersion
        {
            get
            {
                return m_currentVersion;
            }

            set
            {
                if (!Object.ReferenceEquals(m_currentVersion, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_currentVersion = value;
            }
        }

        /// <remarks />
        public TemporaryFileTransferState FileTransfer
        {
            get
            {
                return m_fileTransfer;
            }

            set
            {
                if (!Object.ReferenceEquals(m_fileTransfer, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_fileTransfer = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<LocalizedText> ErrorMessage
        {
            get
            {
                return m_errorMessage;
            }

            set
            {
                if (!Object.ReferenceEquals(m_errorMessage, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_errorMessage = value;
            }
        }

        /// <remarks />
        public PropertyState<uint> WriteBlockSize
        {
            get
            {
                return m_writeBlockSize;
            }

            set
            {
                if (!Object.ReferenceEquals(m_writeBlockSize, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_writeBlockSize = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_currentVersion != null)
            {
                children.Add(m_currentVersion);
            }

            if (m_fileTransfer != null)
            {
                children.Add(m_fileTransfer);
            }

            if (m_errorMessage != null)
            {
                children.Add(m_errorMessage);
            }

            if (m_writeBlockSize != null)
            {
                children.Add(m_writeBlockSize);
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
                case Opc.Ua.DI.BrowseNames.CurrentVersion:
                {
                    if (createOrReplace)
                    {
                        if (CurrentVersion == null)
                        {
                            if (replacement == null)
                            {
                                CurrentVersion = new SoftwareVersionTypeState(this);
                            }
                            else
                            {
                                CurrentVersion = (SoftwareVersionTypeState)replacement;
                            }
                        }
                    }

                    instance = CurrentVersion;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.FileTransfer:
                {
                    if (createOrReplace)
                    {
                        if (FileTransfer == null)
                        {
                            if (replacement == null)
                            {
                                FileTransfer = new TemporaryFileTransferState(this);
                            }
                            else
                            {
                                FileTransfer = (TemporaryFileTransferState)replacement;
                            }
                        }
                    }

                    instance = FileTransfer;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ErrorMessage:
                {
                    if (createOrReplace)
                    {
                        if (ErrorMessage == null)
                        {
                            if (replacement == null)
                            {
                                ErrorMessage = new BaseDataVariableState<LocalizedText>(this);
                            }
                            else
                            {
                                ErrorMessage = (BaseDataVariableState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = ErrorMessage;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.WriteBlockSize:
                {
                    if (createOrReplace)
                    {
                        if (WriteBlockSize == null)
                        {
                            if (replacement == null)
                            {
                                WriteBlockSize = new PropertyState<uint>(this);
                            }
                            else
                            {
                                WriteBlockSize = (PropertyState<uint>)replacement;
                            }
                        }
                    }

                    instance = WriteBlockSize;
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
        private SoftwareVersionTypeState m_currentVersion;
        private TemporaryFileTransferState m_fileTransfer;
        private BaseDataVariableState<LocalizedText> m_errorMessage;
        private PropertyState<uint> m_writeBlockSize;
        #endregion
    }
    #endif
    #endregion

    #region DirectLoadingTypeState Class
    #if (!OPCUA_EXCLUDE_DirectLoadingTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class DirectLoadingTypeState : PackageLoadingTypeState
    {
        #region Constructors
        /// <remarks />
        public DirectLoadingTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.DirectLoadingType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (WriteTimeout != null)
            {
                WriteTimeout.Initialize(context, WriteTimeout_InitializationString);
            }
        }

        #region Initialization String
        private const string WriteTimeout_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAMAAAAV3Jp" +
           "dGVUaW1lb3V0AQGqAAAuAESqAAAAAQAiAf////8BAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAZAAAARGly" +
           "ZWN0TG9hZGluZ1R5cGVJbnN0YW5jZQEBmQABAZkAmQAAAP////8FAAAABGCACgEAAAABAA4AAABDdXJy" +
           "ZW50VmVyc2lvbgEBiwAALwEB1ACLAAAA/////wMAAAAVYIkKAgAAAAEADAAAAE1hbnVmYWN0dXJlcgEB" +
           "WQEALgBEWQEAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAE1hbnVmYWN0dXJlclVyaQEBWgEA" +
           "LgBEWgEAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEAAAAFNvZnR3YXJlUmV2aXNpb24BAVsBAC4A" +
           "RFsBAAAADP////8BAf////8AAAAABGCACgEAAAABAAwAAABGaWxlVHJhbnNmZXIBAYwAAC8BAIA9jAAA" +
           "AP////8EAAAAFWCJCgIAAAAAABcAAABDbGllbnRQcm9jZXNzaW5nVGltZW91dAEBjQAALgBEjQAAAAEA" +
           "IgH/////AQH/////AAAAAARhggoEAAAAAAATAAAAR2VuZXJhdGVGaWxlRm9yUmVhZAEBjgAALwEAgj2O" +
           "AAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAY8AAC4ARI8AAACWAQAAAAEA" +
           "KgEBHgAAAA8AAABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8A" +
           "AAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZAAAC4ARJAAAACWAwAAAAEAKgEBGQAAAAoA" +
           "AABGaWxlTm9kZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAq" +
           "AQElAAAAFgAAAENvbXBsZXRpb25TdGF0ZU1hY2hpbmUAEf////8AAAAAAAEAKAEBAAAAAQAAAAAAAAAB" +
           "Af////8AAAAABGGCCgQAAAAAABQAAABHZW5lcmF0ZUZpbGVGb3JXcml0ZQEBkQAALwEAhT2RAAAAAQH/" +
           "////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAZIAAC4ARJIAAACWAQAAAAEAKgEBHgAA" +
           "AA8AAABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2Cp" +
           "CgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZMAAC4ARJMAAACWAgAAAAEAKgEBGQAAAAoAAABGaWxl" +
           "Tm9kZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAAB" +
           "AAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAAADgAAAENsb3NlQW5kQ29tbWl0AQGUAAAvAQCHPZQAAAAB" +
           "Af////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBlQAALgBElQAAAJYBAAAAAQAqAQEZ" +
           "AAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2CpCgIA" +
           "AAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZYAAC4ARJYAAACWAQAAAAEAKgEBJQAAABYAAABDb21wbGV0" +
           "aW9uU3RhdGVNYWNoaW5lABH/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABVgiQoCAAAA" +
           "AQAMAAAARXJyb3JNZXNzYWdlAQGXAAAvAD+XAAAAABX/////AQH/////AAAAABVgiQoCAAAAAQAOAAAA" +
           "VXBkYXRlQmVoYXZpb3IBAakAAC8AP6kAAAABAU0B/////wEB/////wAAAAAVYIkKAgAAAAEADAAAAFdy" +
           "aXRlVGltZW91dAEBqgAALgBEqgAAAAEAIgH/////AQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<uint> UpdateBehavior
        {
            get
            {
                return m_updateBehavior;
            }

            set
            {
                if (!Object.ReferenceEquals(m_updateBehavior, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_updateBehavior = value;
            }
        }

        /// <remarks />
        public PropertyState<double> WriteTimeout
        {
            get
            {
                return m_writeTimeout;
            }

            set
            {
                if (!Object.ReferenceEquals(m_writeTimeout, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_writeTimeout = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_updateBehavior != null)
            {
                children.Add(m_updateBehavior);
            }

            if (m_writeTimeout != null)
            {
                children.Add(m_writeTimeout);
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
                case Opc.Ua.DI.BrowseNames.UpdateBehavior:
                {
                    if (createOrReplace)
                    {
                        if (UpdateBehavior == null)
                        {
                            if (replacement == null)
                            {
                                UpdateBehavior = new BaseDataVariableState<uint>(this);
                            }
                            else
                            {
                                UpdateBehavior = (BaseDataVariableState<uint>)replacement;
                            }
                        }
                    }

                    instance = UpdateBehavior;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.WriteTimeout:
                {
                    if (createOrReplace)
                    {
                        if (WriteTimeout == null)
                        {
                            if (replacement == null)
                            {
                                WriteTimeout = new PropertyState<double>(this);
                            }
                            else
                            {
                                WriteTimeout = (PropertyState<double>)replacement;
                            }
                        }
                    }

                    instance = WriteTimeout;
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
        private BaseDataVariableState<uint> m_updateBehavior;
        private PropertyState<double> m_writeTimeout;
        #endregion
    }
    #endif
    #endregion

    #region CachedLoadingTypeState Class
    #if (!OPCUA_EXCLUDE_CachedLoadingTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class CachedLoadingTypeState : PackageLoadingTypeState
    {
        #region Constructors
        /// <remarks />
        public CachedLoadingTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.CachedLoadingType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (FallbackVersion != null)
            {
                FallbackVersion.Initialize(context, FallbackVersion_InitializationString);
            }
        }

        #region Initialization String
        private const string FallbackVersion_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAoBAAAAAQAPAAAARmFs" +
           "bGJhY2tWZXJzaW9uAQG8AAAvAQHUALwAAAD/////AwAAABVgiQoCAAAAAQAMAAAATWFudWZhY3R1cmVy" +
           "AQF1AQAuAER1AQAAABX/////AQH/////AAAAABVgiQoCAAAAAQAPAAAATWFudWZhY3R1cmVyVXJpAQF2" +
           "AQAuAER2AQAAAAz/////AQH/////AAAAABVgiQoCAAAAAQAQAAAAU29mdHdhcmVSZXZpc2lvbgEBdwEA" +
           "LgBEdwEAAAAM/////wEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAZAAAAQ2Fj" +
           "aGVkTG9hZGluZ1R5cGVJbnN0YW5jZQEBqwABAasAqwAAAP////8GAAAABGCACgEAAAABAA4AAABDdXJy" +
           "ZW50VmVyc2lvbgEBiwAALwEB1ACLAAAA/////wMAAAAVYIkKAgAAAAEADAAAAE1hbnVmYWN0dXJlcgEB" +
           "WQEALgBEWQEAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAE1hbnVmYWN0dXJlclVyaQEBWgEA" +
           "LgBEWgEAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEAAAAFNvZnR3YXJlUmV2aXNpb24BAVsBAC4A" +
           "RFsBAAAADP////8BAf////8AAAAABGCACgEAAAABAAwAAABGaWxlVHJhbnNmZXIBAYwAAC8BAIA9jAAA" +
           "AP////8EAAAAFWCJCgIAAAAAABcAAABDbGllbnRQcm9jZXNzaW5nVGltZW91dAEBjQAALgBEjQAAAAEA" +
           "IgH/////AQH/////AAAAAARhggoEAAAAAAATAAAAR2VuZXJhdGVGaWxlRm9yUmVhZAEBjgAALwEAgj2O" +
           "AAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAY8AAC4ARI8AAACWAQAAAAEA" +
           "KgEBHgAAAA8AAABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8A" +
           "AAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZAAAC4ARJAAAACWAwAAAAEAKgEBGQAAAAoA" +
           "AABGaWxlTm9kZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAq" +
           "AQElAAAAFgAAAENvbXBsZXRpb25TdGF0ZU1hY2hpbmUAEf////8AAAAAAAEAKAEBAAAAAQAAAAAAAAAB" +
           "Af////8AAAAABGGCCgQAAAAAABQAAABHZW5lcmF0ZUZpbGVGb3JXcml0ZQEBkQAALwEAhT2RAAAAAQH/" +
           "////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAZIAAC4ARJIAAACWAQAAAAEAKgEBHgAA" +
           "AA8AAABHZW5lcmF0ZU9wdGlvbnMAGP////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2Cp" +
           "CgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZMAAC4ARJMAAACWAgAAAAEAKgEBGQAAAAoAAABGaWxl" +
           "Tm9kZUlkABH/////AAAAAAABACoBARkAAAAKAAAARmlsZUhhbmRsZQAH/////wAAAAAAAQAoAQEAAAAB" +
           "AAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAAADgAAAENsb3NlQW5kQ29tbWl0AQGUAAAvAQCHPZQAAAAB" +
           "Af////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBlQAALgBElQAAAJYBAAAAAQAqAQEZ" +
           "AAAACgAAAEZpbGVIYW5kbGUAB/////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAAF2CpCgIA" +
           "AAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAZYAAC4ARJYAAACWAQAAAAEAKgEBJQAAABYAAABDb21wbGV0" +
           "aW9uU3RhdGVNYWNoaW5lABH/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABVgiQoCAAAA" +
           "AQAMAAAARXJyb3JNZXNzYWdlAQGXAAAvAD+XAAAAABX/////AQH/////AAAAAARggAoBAAAAAQAOAAAA" +
           "UGVuZGluZ1ZlcnNpb24BAbsAAC8BAdQAuwAAAP////8DAAAAFWCJCgIAAAABAAwAAABNYW51ZmFjdHVy" +
           "ZXIBAW4BAC4ARG4BAAAAFf////8BAf////8AAAAAFWCJCgIAAAABAA8AAABNYW51ZmFjdHVyZXJVcmkB" +
           "AW8BAC4ARG8BAAAADP////8BAf////8AAAAAFWCJCgIAAAABABAAAABTb2Z0d2FyZVJldmlzaW9uAQFw" +
           "AQAuAERwAQAAAAz/////AQH/////AAAAAARggAoBAAAAAQAPAAAARmFsbGJhY2tWZXJzaW9uAQG8AAAv" +
           "AQHUALwAAAD/////AwAAABVgiQoCAAAAAQAMAAAATWFudWZhY3R1cmVyAQF1AQAuAER1AQAAABX/////" +
           "AQH/////AAAAABVgiQoCAAAAAQAPAAAATWFudWZhY3R1cmVyVXJpAQF2AQAuAER2AQAAAAz/////AQH/" +
           "////AAAAABVgiQoCAAAAAQAQAAAAU29mdHdhcmVSZXZpc2lvbgEBdwEALgBEdwEAAAAM/////wEB////" +
           "/wAAAAAEYYIKBAAAAAEAEQAAAEdldFVwZGF0ZUJlaGF2aW9yAQG9AAAvAQG9AL0AAAABAf////8CAAAA" +
           "F2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBvgAALgBEvgAAAJYDAAAAAQAqAQEeAAAADwAAAE1h" +
           "bnVmYWN0dXJlclVyaQAM/////wAAAAAAAQAqAQEfAAAAEAAAAFNvZnR3YXJlUmV2aXNpb24ADP////8A" +
           "AAAAAAEAKgEBIwAAABAAAABQYXRjaElkZW50aWZpZXJzAAwBAAAAAQAAAAAAAAAAAQAoAQEAAAABAAAA" +
           "AAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEBvwAALgBEvwAAAJYBAAAA" +
           "AQAqAQEfAAAADgAAAFVwZGF0ZUJlaGF2aW9yAQFNAf////8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf//" +
           "//8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public SoftwareVersionTypeState PendingVersion
        {
            get
            {
                return m_pendingVersion;
            }

            set
            {
                if (!Object.ReferenceEquals(m_pendingVersion, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_pendingVersion = value;
            }
        }

        /// <remarks />
        public SoftwareVersionTypeState FallbackVersion
        {
            get
            {
                return m_fallbackVersion;
            }

            set
            {
                if (!Object.ReferenceEquals(m_fallbackVersion, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_fallbackVersion = value;
            }
        }

        /// <remarks />
        public GetUpdateBehaviorMethodState GetUpdateBehavior
        {
            get
            {
                return m_getUpdateBehaviorMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_getUpdateBehaviorMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_getUpdateBehaviorMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_pendingVersion != null)
            {
                children.Add(m_pendingVersion);
            }

            if (m_fallbackVersion != null)
            {
                children.Add(m_fallbackVersion);
            }

            if (m_getUpdateBehaviorMethod != null)
            {
                children.Add(m_getUpdateBehaviorMethod);
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
                case Opc.Ua.DI.BrowseNames.PendingVersion:
                {
                    if (createOrReplace)
                    {
                        if (PendingVersion == null)
                        {
                            if (replacement == null)
                            {
                                PendingVersion = new SoftwareVersionTypeState(this);
                            }
                            else
                            {
                                PendingVersion = (SoftwareVersionTypeState)replacement;
                            }
                        }
                    }

                    instance = PendingVersion;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.FallbackVersion:
                {
                    if (createOrReplace)
                    {
                        if (FallbackVersion == null)
                        {
                            if (replacement == null)
                            {
                                FallbackVersion = new SoftwareVersionTypeState(this);
                            }
                            else
                            {
                                FallbackVersion = (SoftwareVersionTypeState)replacement;
                            }
                        }
                    }

                    instance = FallbackVersion;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.GetUpdateBehavior:
                {
                    if (createOrReplace)
                    {
                        if (GetUpdateBehavior == null)
                        {
                            if (replacement == null)
                            {
                                GetUpdateBehavior = new GetUpdateBehaviorMethodState(this);
                            }
                            else
                            {
                                GetUpdateBehavior = (GetUpdateBehaviorMethodState)replacement;
                            }
                        }
                    }

                    instance = GetUpdateBehavior;
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
        private SoftwareVersionTypeState m_pendingVersion;
        private SoftwareVersionTypeState m_fallbackVersion;
        private GetUpdateBehaviorMethodState m_getUpdateBehaviorMethod;
        #endregion
    }
    #endif
    #endregion

    #region FileSystemLoadingTypeState Class
    #if (!OPCUA_EXCLUDE_FileSystemLoadingTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class FileSystemLoadingTypeState : SoftwareLoadingTypeState
    {
        #region Constructors
        /// <remarks />
        public FileSystemLoadingTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.FileSystemLoadingType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (ValidateFiles != null)
            {
                ValidateFiles.Initialize(context, ValidateFiles_InitializationString);
            }
        }

        #region Initialization String
        private const string ValidateFiles_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggoEAAAAAQANAAAAVmFs" +
           "aWRhdGVGaWxlcwEB0QAALwEB0QDRAAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVu" +
           "dHMBAdIAAC4ARNIAAACWAQAAAAEAKgEBGgAAAAcAAABOb2RlSWRzABEBAAAAAQAAAAAAAAAAAQAoAQEA" +
           "AAABAAAAAAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEB0wAALgBE0wAA" +
           "AJYCAAAAAQAqAQEYAAAACQAAAEVycm9yQ29kZQAG/////wAAAAAAAQAqAQEbAAAADAAAAEVycm9yTWVz" +
           "c2FnZQAV/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAdAAAARmls" +
           "ZVN5c3RlbUxvYWRpbmdUeXBlSW5zdGFuY2UBAcAAAQHAAMAAAAD/////AwAAAARggAoBAAAAAAAKAAAA" +
           "RmlsZVN5c3RlbQEBwgAALwEAKTTCAAAA/////wQAAAAEYYIKBAAAAAAADwAAAENyZWF0ZURpcmVjdG9y" +
           "eQEBwwAALwEASzTDAAAAAQH/////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAcQAAC4A" +
           "RMQAAACWAQAAAAEAKgEBHAAAAA0AAABEaXJlY3RvcnlOYW1lAAz/////AAAAAAABACgBAQAAAAEAAAAA" +
           "AAAAAQH/////AAAAABdgqQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQHFAAAuAETFAAAAlgEAAAAB" +
           "ACoBAR4AAAAPAAAARGlyZWN0b3J5Tm9kZUlkABH/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////" +
           "AAAAAARhggoEAAAAAAAKAAAAQ3JlYXRlRmlsZQEBxgAALwEATjTGAAAAAQH/////AgAAABdgqQoCAAAA" +
           "AAAOAAAASW5wdXRBcmd1bWVudHMBAccAAC4ARMcAAACWAgAAAAEAKgEBFwAAAAgAAABGaWxlTmFtZQAM" +
           "/////wAAAAAAAQAqAQEeAAAADwAAAFJlcXVlc3RGaWxlT3BlbgAB/////wAAAAAAAQAoAQEAAAABAAAA" +
           "AAAAAAEB/////wAAAAAXYKkKAgAAAAAADwAAAE91dHB1dEFyZ3VtZW50cwEByAAALgBEyAAAAJYCAAAA" +
           "AQAqAQEZAAAACgAAAEZpbGVOb2RlSWQAEf////8AAAAAAAEAKgEBGQAAAAoAAABGaWxlSGFuZGxlAAf/" +
           "////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhwgoEAAAAFgAAAERlbGV0ZUZpbGVTeXN0" +
           "ZW1PYmplY3QAAAYAAABEZWxldGUBAckAAC8BAFE0yQAAAAEB/////wEAAAAXYKkKAgAAAAAADgAAAElu" +
           "cHV0QXJndW1lbnRzAQHKAAAuAETKAAAAlgEAAAABACoBAR0AAAAOAAAAT2JqZWN0VG9EZWxldGUAEf//" +
           "//8AAAAAAAEAKAEBAAAAAQAAAAAAAAABAf////8AAAAABGGCCgQAAAAAAAoAAABNb3ZlT3JDb3B5AQHL" +
           "AAAvAQBTNMsAAAABAf////8CAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEBzAAALgBEzAAA" +
           "AJYEAAAAAQAqAQEhAAAAEgAAAE9iamVjdFRvTW92ZU9yQ29weQAR/////wAAAAAAAQAqAQEeAAAADwAA" +
           "AFRhcmdldERpcmVjdG9yeQAR/////wAAAAAAAQAqAQEZAAAACgAAAENyZWF0ZUNvcHkAAf////8AAAAA" +
           "AAEAKgEBFgAAAAcAAABOZXdOYW1lAAz/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAABdg" +
           "qQoCAAAAAAAPAAAAT3V0cHV0QXJndW1lbnRzAQHNAAAuAETNAAAAlgEAAAABACoBARgAAAAJAAAATmV3" +
           "Tm9kZUlkABH/////AAAAAAABACgBAQAAAAEAAAAAAAAAAQH/////AAAAAARhggoEAAAAAQARAAAAR2V0" +
           "VXBkYXRlQmVoYXZpb3IBAc4AAC8BAc4AzgAAAAEB/////wIAAAAXYKkKAgAAAAAADgAAAElucHV0QXJn" +
           "dW1lbnRzAQHPAAAuAETPAAAAlgEAAAABACoBARoAAAAHAAAATm9kZUlkcwARAQAAAAEAAAAAAAAAAAEA" +
           "KAEBAAAAAQAAAAAAAAABAf////8AAAAAF2CpCgIAAAAAAA8AAABPdXRwdXRBcmd1bWVudHMBAdAAAC4A" +
           "RNAAAACWAQAAAAEAKgEBHwAAAA4AAABVcGRhdGVCZWhhdmlvcgEBTQH/////AAAAAAABACgBAQAAAAEA" +
           "AAAAAAAAAQH/////AAAAAARhggoEAAAAAQANAAAAVmFsaWRhdGVGaWxlcwEB0QAALwEB0QDRAAAAAQH/" +
           "////AgAAABdgqQoCAAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAdIAAC4ARNIAAACWAQAAAAEAKgEBGgAA" +
           "AAcAAABOb2RlSWRzABEBAAAAAQAAAAAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAXYKkKAgAA" +
           "AAAADwAAAE91dHB1dEFyZ3VtZW50cwEB0wAALgBE0wAAAJYCAAAAAQAqAQEYAAAACQAAAEVycm9yQ29k" +
           "ZQAG/////wAAAAAAAQAqAQEbAAAADAAAAEVycm9yTWVzc2FnZQAV/////wAAAAAAAQAoAQEAAAABAAAA" +
           "AAAAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public FileDirectoryState FileSystem
        {
            get
            {
                return m_fileSystem;
            }

            set
            {
                if (!Object.ReferenceEquals(m_fileSystem, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_fileSystem = value;
            }
        }

        /// <remarks />
        public GetUpdateBehaviorMethodState GetUpdateBehavior
        {
            get
            {
                return m_getUpdateBehaviorMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_getUpdateBehaviorMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_getUpdateBehaviorMethod = value;
            }
        }

        /// <remarks />
        public ValidateFilesMethodState ValidateFiles
        {
            get
            {
                return m_validateFilesMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_validateFilesMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_validateFilesMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_fileSystem != null)
            {
                children.Add(m_fileSystem);
            }

            if (m_getUpdateBehaviorMethod != null)
            {
                children.Add(m_getUpdateBehaviorMethod);
            }

            if (m_validateFilesMethod != null)
            {
                children.Add(m_validateFilesMethod);
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
                case Opc.Ua.BrowseNames.FileSystem:
                {
                    if (createOrReplace)
                    {
                        if (FileSystem == null)
                        {
                            if (replacement == null)
                            {
                                FileSystem = new FileDirectoryState(this);
                            }
                            else
                            {
                                FileSystem = (FileDirectoryState)replacement;
                            }
                        }
                    }

                    instance = FileSystem;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.GetUpdateBehavior:
                {
                    if (createOrReplace)
                    {
                        if (GetUpdateBehavior == null)
                        {
                            if (replacement == null)
                            {
                                GetUpdateBehavior = new GetUpdateBehaviorMethodState(this);
                            }
                            else
                            {
                                GetUpdateBehavior = (GetUpdateBehaviorMethodState)replacement;
                            }
                        }
                    }

                    instance = GetUpdateBehavior;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ValidateFiles:
                {
                    if (createOrReplace)
                    {
                        if (ValidateFiles == null)
                        {
                            if (replacement == null)
                            {
                                ValidateFiles = new ValidateFilesMethodState(this);
                            }
                            else
                            {
                                ValidateFiles = (ValidateFilesMethodState)replacement;
                            }
                        }
                    }

                    instance = ValidateFiles;
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
        private FileDirectoryState m_fileSystem;
        private GetUpdateBehaviorMethodState m_getUpdateBehaviorMethod;
        private ValidateFilesMethodState m_validateFilesMethod;
        #endregion
    }
    #endif
    #endregion

    #region SoftwareVersionTypeState Class
    #if (!OPCUA_EXCLUDE_SoftwareVersionTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class SoftwareVersionTypeState : BaseObjectState
    {
        #region Constructors
        /// <remarks />
        public SoftwareVersionTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.SoftwareVersionType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (PatchIdentifiers != null)
            {
                PatchIdentifiers.Initialize(context, PatchIdentifiers_InitializationString);
            }

            if (ReleaseDate != null)
            {
                ReleaseDate.Initialize(context, ReleaseDate_InitializationString);
            }

            if (ChangeLogReference != null)
            {
                ChangeLogReference.Initialize(context, ChangeLogReference_InitializationString);
            }

            if (Hash != null)
            {
                Hash.Initialize(context, Hash_InitializationString);
            }
        }

        #region Initialization String
        private const string PatchIdentifiers_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xdgiQoCAAAAAQAQAAAAUGF0" +
           "Y2hJZGVudGlmaWVycwEBfwEALgBEfwEAAAAMAQAAAAEAAAAAAAAAAQH/////AAAAAA==";

        private const string ReleaseDate_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQALAAAAUmVs" +
           "ZWFzZURhdGUBAYABAC4ARIABAAAADf////8BAf////8AAAAA";

        private const string ChangeLogReference_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQASAAAAQ2hh" +
           "bmdlTG9nUmVmZXJlbmNlAQGBAQAuAESBAQAAAAz/////AQH/////AAAAAA==";

        private const string Hash_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAEAAAASGFz" +
           "aAEBggEALgBEggEAAAAP/////wEB/////wAAAAA=";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAbAAAAU29m" +
           "dHdhcmVWZXJzaW9uVHlwZUluc3RhbmNlAQHUAAEB1ADUAAAA/////wcAAAAVYIkKAgAAAAEADAAAAE1h" +
           "bnVmYWN0dXJlcgEBfAEALgBEfAEAAAAV/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAE1hbnVmYWN0" +
           "dXJlclVyaQEBfQEALgBEfQEAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEAEAAAAFNvZnR3YXJlUmV2" +
           "aXNpb24BAX4BAC4ARH4BAAAADP////8BAf////8AAAAAF2CJCgIAAAABABAAAABQYXRjaElkZW50aWZp" +
           "ZXJzAQF/AQAuAER/AQAAAAwBAAAAAQAAAAAAAAABAf////8AAAAAFWCJCgIAAAABAAsAAABSZWxlYXNl" +
           "RGF0ZQEBgAEALgBEgAEAAAAN/////wEB/////wAAAAAVYIkKAgAAAAEAEgAAAENoYW5nZUxvZ1JlZmVy" +
           "ZW5jZQEBgQEALgBEgQEAAAAM/////wEB/////wAAAAAVYIkKAgAAAAEABAAAAEhhc2gBAYIBAC4ARIIB" +
           "AAAAD/////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public PropertyState<LocalizedText> Manufacturer
        {
            get
            {
                return m_manufacturer;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturer, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturer = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ManufacturerUri
        {
            get
            {
                return m_manufacturerUri;
            }

            set
            {
                if (!Object.ReferenceEquals(m_manufacturerUri, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_manufacturerUri = value;
            }
        }

        /// <remarks />
        public PropertyState<string> SoftwareRevision
        {
            get
            {
                return m_softwareRevision;
            }

            set
            {
                if (!Object.ReferenceEquals(m_softwareRevision, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_softwareRevision = value;
            }
        }

        /// <remarks />
        public PropertyState<string[]> PatchIdentifiers
        {
            get
            {
                return m_patchIdentifiers;
            }

            set
            {
                if (!Object.ReferenceEquals(m_patchIdentifiers, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_patchIdentifiers = value;
            }
        }

        /// <remarks />
        public PropertyState<DateTime> ReleaseDate
        {
            get
            {
                return m_releaseDate;
            }

            set
            {
                if (!Object.ReferenceEquals(m_releaseDate, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_releaseDate = value;
            }
        }

        /// <remarks />
        public PropertyState<string> ChangeLogReference
        {
            get
            {
                return m_changeLogReference;
            }

            set
            {
                if (!Object.ReferenceEquals(m_changeLogReference, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_changeLogReference = value;
            }
        }

        /// <remarks />
        public PropertyState<byte[]> Hash
        {
            get
            {
                return m_hash;
            }

            set
            {
                if (!Object.ReferenceEquals(m_hash, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_hash = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_manufacturer != null)
            {
                children.Add(m_manufacturer);
            }

            if (m_manufacturerUri != null)
            {
                children.Add(m_manufacturerUri);
            }

            if (m_softwareRevision != null)
            {
                children.Add(m_softwareRevision);
            }

            if (m_patchIdentifiers != null)
            {
                children.Add(m_patchIdentifiers);
            }

            if (m_releaseDate != null)
            {
                children.Add(m_releaseDate);
            }

            if (m_changeLogReference != null)
            {
                children.Add(m_changeLogReference);
            }

            if (m_hash != null)
            {
                children.Add(m_hash);
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
                case Opc.Ua.DI.BrowseNames.Manufacturer:
                {
                    if (createOrReplace)
                    {
                        if (Manufacturer == null)
                        {
                            if (replacement == null)
                            {
                                Manufacturer = new PropertyState<LocalizedText>(this);
                            }
                            else
                            {
                                Manufacturer = (PropertyState<LocalizedText>)replacement;
                            }
                        }
                    }

                    instance = Manufacturer;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ManufacturerUri:
                {
                    if (createOrReplace)
                    {
                        if (ManufacturerUri == null)
                        {
                            if (replacement == null)
                            {
                                ManufacturerUri = new PropertyState<string>(this);
                            }
                            else
                            {
                                ManufacturerUri = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ManufacturerUri;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.SoftwareRevision:
                {
                    if (createOrReplace)
                    {
                        if (SoftwareRevision == null)
                        {
                            if (replacement == null)
                            {
                                SoftwareRevision = new PropertyState<string>(this);
                            }
                            else
                            {
                                SoftwareRevision = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = SoftwareRevision;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.PatchIdentifiers:
                {
                    if (createOrReplace)
                    {
                        if (PatchIdentifiers == null)
                        {
                            if (replacement == null)
                            {
                                PatchIdentifiers = new PropertyState<string[]>(this);
                            }
                            else
                            {
                                PatchIdentifiers = (PropertyState<string[]>)replacement;
                            }
                        }
                    }

                    instance = PatchIdentifiers;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ReleaseDate:
                {
                    if (createOrReplace)
                    {
                        if (ReleaseDate == null)
                        {
                            if (replacement == null)
                            {
                                ReleaseDate = new PropertyState<DateTime>(this);
                            }
                            else
                            {
                                ReleaseDate = (PropertyState<DateTime>)replacement;
                            }
                        }
                    }

                    instance = ReleaseDate;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ChangeLogReference:
                {
                    if (createOrReplace)
                    {
                        if (ChangeLogReference == null)
                        {
                            if (replacement == null)
                            {
                                ChangeLogReference = new PropertyState<string>(this);
                            }
                            else
                            {
                                ChangeLogReference = (PropertyState<string>)replacement;
                            }
                        }
                    }

                    instance = ChangeLogReference;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Hash:
                {
                    if (createOrReplace)
                    {
                        if (Hash == null)
                        {
                            if (replacement == null)
                            {
                                Hash = new PropertyState<byte[]>(this);
                            }
                            else
                            {
                                Hash = (PropertyState<byte[]>)replacement;
                            }
                        }
                    }

                    instance = Hash;
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
        private PropertyState<LocalizedText> m_manufacturer;
        private PropertyState<string> m_manufacturerUri;
        private PropertyState<string> m_softwareRevision;
        private PropertyState<string[]> m_patchIdentifiers;
        private PropertyState<DateTime> m_releaseDate;
        private PropertyState<string> m_changeLogReference;
        private PropertyState<byte[]> m_hash;
        #endregion
    }
    #endif
    #endregion

    #region PrepareForUpdateStateMachineTypeState Class
    #if (!OPCUA_EXCLUDE_PrepareForUpdateStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PrepareForUpdateStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public PrepareForUpdateStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.PrepareForUpdateStateMachineType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (PercentComplete != null)
            {
                PercentComplete.Initialize(context, PercentComplete_InitializationString);
            }

            if (Resume != null)
            {
                Resume.Initialize(context, Resume_InitializationString);
            }
        }

        #region Initialization String
        private const string PercentComplete_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAAUGVy" +
           "Y2VudENvbXBsZXRlAQHjAAAvAD/jAAAAAAP/////AQH/////AAAAAA==";

        private const string Resume_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggoEAAAAAQAGAAAAUmVz" +
           "dW1lAQHmAAAvAQHmAOYAAAABAf////8AAAAA";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAoAAAAUHJl" +
           "cGFyZUZvclVwZGF0ZVN0YXRlTWFjaGluZVR5cGVJbnN0YW5jZQEB1QABAdUA1QAAAP////8OAAAAFWCJ" +
           "CAIAAAAAAAwAAABDdXJyZW50U3RhdGUBAQAAAC8BAMgKABX/////AQH/////AQAAABVgiQgCAAAAAAAC" +
           "AAAASWQBAQAAAC4ARAAR/////wEB/////wAAAAAVYIkKAgAAAAEADwAAAFBlcmNlbnRDb21wbGV0ZQEB" +
           "4wAALwA/4wAAAAAD/////wEB/////wAAAAAEYYIKBAAAAAEABwAAAFByZXBhcmUBAeQAAC8BAeQA5AAA" +
           "AAEB/////wAAAAAEYYIKBAAAAAEABQAAAEFib3J0AQHlAAAvAQHlAOUAAAABAf////8AAAAABGGCCgQA" +
           "AAABAAYAAABSZXN1bWUBAeYAAC8BAeYA5gAAAAEB/////wAAAAAEYIAKAQAAAAEABAAAAElkbGUBAecA" +
           "AC8BAAUJ5wAAAAMAAAAAMwEBAe8AADQBAQHxAAA0AQEB9wABAAAAFWCpCgIAAAAAAAsAAABTdGF0ZU51" +
           "bWJlcgEB6AAALgBE6AAAAAcBAAAAAAf/////AQH/////AAAAAARggAoBAAAAAQAJAAAAUHJlcGFyaW5n" +
           "AQHpAAAvAQADCekAAAADAAAAADQBAQHvAAAzAQEB8QAAMwEBAfMAAQAAABVgqQoCAAAAAAALAAAAU3Rh" +
           "dGVOdW1iZXIBAeoAAC4AROoAAAAHAgAAAAAH/////wEB/////wAAAAAEYIAKAQAAAAEAEQAAAFByZXBh" +
           "cmVkRm9yVXBkYXRlAQHrAAAvAQADCesAAAACAAAAADQBAQHzAAAzAQEB9QABAAAAFWCpCgIAAAAAAAsA" +
           "AABTdGF0ZU51bWJlcgEB7AAALgBE7AAAAAcDAAAAAAf/////AQH/////AAAAAARggAoBAAAAAQAIAAAA" +
           "UmVzdW1pbmcBAe0AAC8BAAMJ7QAAAAIAAAAANAEBAfUAADMBAQH3AAEAAAAVYKkKAgAAAAAACwAAAFN0" +
           "YXRlTnVtYmVyAQHuAAAuAETuAAAABwQAAAAAB/////8BAf////8AAAAABGCACgEAAAABAA8AAABJZGxl" +
           "VG9QcmVwYXJpbmcBAe8AAC8BAAYJ7wAAAAMAAAAAMwABAecAADQAAQHpAAA2AAEABwkBAAAAFWCpCgIA" +
           "AAAAABAAAABUcmFuc2l0aW9uTnVtYmVyAQHwAAAuAETwAAAABwwAAAAAB/////8BAf////8AAAAABGCA" +
           "CgEAAAABAA8AAABQcmVwYXJpbmdUb0lkbGUBAfEAAC8BAAYJ8QAAAAMAAAAANAABAecAADMAAQHpAAA2" +
           "AAEABwkBAAAAFWCpCgIAAAAAABAAAABUcmFuc2l0aW9uTnVtYmVyAQHyAAAuAETyAAAABxUAAAAAB///" +
           "//8BAf////8AAAAABGCACgEAAAABABwAAABQcmVwYXJpbmdUb1ByZXBhcmVkRm9yVXBkYXRlAQHzAAAv" +
           "AQAGCfMAAAADAAAAADMAAQHpAAA0AAEB6wAANgABAAcJAQAAABVgqQoCAAAAAAAQAAAAVHJhbnNpdGlv" +
           "bk51bWJlcgEB9AAALgBE9AAAAAcXAAAAAAf/////AQH/////AAAAAARggAoBAAAAAQAbAAAAUHJlcGFy" +
           "ZWRGb3JVcGRhdGVUb1Jlc3VtaW5nAQH1AAAvAQAGCfUAAAADAAAAADMAAQHrAAA0AAEB7QAANgABAAcJ" +
           "AQAAABVgqQoCAAAAAAAQAAAAVHJhbnNpdGlvbk51bWJlcgEB9gAALgBE9gAAAAciAAAAAAf/////AQH/" +
           "////AAAAAARggAoBAAAAAQAOAAAAUmVzdW1pbmdUb0lkbGUBAfcAAC8BAAYJ9wAAAAMAAAAANAABAecA" +
           "ADMAAQHtAAA2AAEABwkBAAAAFWCpCgIAAAAAABAAAABUcmFuc2l0aW9uTnVtYmVyAQH4AAAuAET4AAAA" +
           "BykAAAAAB/////8BAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<byte> PercentComplete
        {
            get
            {
                return m_percentComplete;
            }

            set
            {
                if (!Object.ReferenceEquals(m_percentComplete, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_percentComplete = value;
            }
        }

        /// <remarks />
        public MethodState Prepare
        {
            get
            {
                return m_prepareMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_prepareMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_prepareMethod = value;
            }
        }

        /// <remarks />
        public MethodState Abort
        {
            get
            {
                return m_abortMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_abortMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_abortMethod = value;
            }
        }

        /// <remarks />
        public MethodState Resume
        {
            get
            {
                return m_resumeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_resumeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_resumeMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_percentComplete != null)
            {
                children.Add(m_percentComplete);
            }

            if (m_prepareMethod != null)
            {
                children.Add(m_prepareMethod);
            }

            if (m_abortMethod != null)
            {
                children.Add(m_abortMethod);
            }

            if (m_resumeMethod != null)
            {
                children.Add(m_resumeMethod);
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
                case Opc.Ua.DI.BrowseNames.PercentComplete:
                {
                    if (createOrReplace)
                    {
                        if (PercentComplete == null)
                        {
                            if (replacement == null)
                            {
                                PercentComplete = new BaseDataVariableState<byte>(this);
                            }
                            else
                            {
                                PercentComplete = (BaseDataVariableState<byte>)replacement;
                            }
                        }
                    }

                    instance = PercentComplete;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Prepare:
                {
                    if (createOrReplace)
                    {
                        if (Prepare == null)
                        {
                            if (replacement == null)
                            {
                                Prepare = new MethodState(this);
                            }
                            else
                            {
                                Prepare = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Prepare;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Abort:
                {
                    if (createOrReplace)
                    {
                        if (Abort == null)
                        {
                            if (replacement == null)
                            {
                                Abort = new MethodState(this);
                            }
                            else
                            {
                                Abort = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Abort;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Resume:
                {
                    if (createOrReplace)
                    {
                        if (Resume == null)
                        {
                            if (replacement == null)
                            {
                                Resume = new MethodState(this);
                            }
                            else
                            {
                                Resume = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Resume;
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
        private BaseDataVariableState<byte> m_percentComplete;
        private MethodState m_prepareMethod;
        private MethodState m_abortMethod;
        private MethodState m_resumeMethod;
        #endregion
    }
    #endif
    #endregion

    #region InstallationStateMachineTypeState Class
    #if (!OPCUA_EXCLUDE_InstallationStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class InstallationStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public InstallationStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.InstallationStateMachineType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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

            if (PercentComplete != null)
            {
                PercentComplete.Initialize(context, PercentComplete_InitializationString);
            }

            if (InstallationDelay != null)
            {
                InstallationDelay.Initialize(context, InstallationDelay_InitializationString);
            }

            if (InstallSoftwarePackage != null)
            {
                InstallSoftwarePackage.Initialize(context, InstallSoftwarePackage_InitializationString);
            }

            if (InstallFiles != null)
            {
                InstallFiles.Initialize(context, InstallFiles_InitializationString);
            }
        }

        #region Initialization String
        private const string PercentComplete_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQAPAAAAUGVy" +
           "Y2VudENvbXBsZXRlAQEHAQAvAD8HAQAAAAP/////AQH/////AAAAAA==";

        private const string InstallationDelay_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////xVgiQoCAAAAAQARAAAASW5z" +
           "dGFsbGF0aW9uRGVsYXkBAQgBAC8APwgBAAABACIB/////wEB/////wAAAAA=";

        private const string InstallSoftwarePackage_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggoEAAAAAQAWAAAASW5z" +
           "dGFsbFNvZnR3YXJlUGFja2FnZQEBCQEALwEBCQEJAQAAAQH/////AQAAABdgqQoCAAAAAAAOAAAASW5w" +
           "dXRBcmd1bWVudHMBAQoBAC4ARAoBAACWBAAAAAEAKgEBHgAAAA8AAABNYW51ZmFjdHVyZXJVcmkADP//" +
           "//8AAAAAAAEAKgEBHwAAABAAAABTb2Z0d2FyZVJldmlzaW9uAAz/////AAAAAAABACoBASMAAAAQAAAA" +
           "UGF0Y2hJZGVudGlmaWVycwAMAQAAAAEAAAAAAAAAAAEAKgEBEwAAAAQAAABIYXNoAA//////AAAAAAAB" +
           "ACgBAQAAAAEAAAAAAAAAAQH/////AAAAAA==";

        private const string InstallFiles_InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggoEAAAAAQAMAAAASW5z" +
           "dGFsbEZpbGVzAQEMAQAvAQEMAQwBAAABAf////8BAAAAF2CpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50" +
           "cwEBDQEALgBEDQEAAJYBAAAAAQAqAQEaAAAABwAAAE5vZGVJZHMAEQEAAAABAAAAAAAAAAABACgBAQAA" +
           "AAEAAAAAAAAAAQH/////AAAAAA==";

        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAkAAAASW5z" +
           "dGFsbGF0aW9uU3RhdGVNYWNoaW5lVHlwZUluc3RhbmNlAQH5AAEB+QD5AAAA/////w0AAAAVYIkIAgAA" +
           "AAAADAAAAEN1cnJlbnRTdGF0ZQEBAAAALwEAyAoAFf////8BAf////8BAAAAFWCJCAIAAAAAAAIAAABJ" +
           "ZAEBAAAALgBEABH/////AQH/////AAAAABVgiQoCAAAAAQAPAAAAUGVyY2VudENvbXBsZXRlAQEHAQAv" +
           "AD8HAQAAAAP/////AQH/////AAAAABVgiQoCAAAAAQARAAAASW5zdGFsbGF0aW9uRGVsYXkBAQgBAC8A" +
           "PwgBAAABACIB/////wEB/////wAAAAAEYYIKBAAAAAEAFgAAAEluc3RhbGxTb2Z0d2FyZVBhY2thZ2UB" +
           "AQkBAC8BAQkBCQEAAAEB/////wEAAAAXYKkKAgAAAAAADgAAAElucHV0QXJndW1lbnRzAQEKAQAuAEQK" +
           "AQAAlgQAAAABACoBAR4AAAAPAAAATWFudWZhY3R1cmVyVXJpAAz/////AAAAAAABACoBAR8AAAAQAAAA" +
           "U29mdHdhcmVSZXZpc2lvbgAM/////wAAAAAAAQAqAQEjAAAAEAAAAFBhdGNoSWRlbnRpZmllcnMADAEA" +
           "AAABAAAAAAAAAAABACoBARMAAAAEAAAASGFzaAAP/////wAAAAAAAQAoAQEAAAABAAAAAAAAAAEB////" +
           "/wAAAAAEYYIKBAAAAAEADAAAAEluc3RhbGxGaWxlcwEBDAEALwEBDAEMAQAAAQH/////AQAAABdgqQoC" +
           "AAAAAAAOAAAASW5wdXRBcmd1bWVudHMBAQ0BAC4ARA0BAACWAQAAAAEAKgEBGgAAAAcAAABOb2RlSWRz" +
           "ABEBAAAAAQAAAAAAAAAAAQAoAQEAAAABAAAAAAAAAAEB/////wAAAAAEYYIKBAAAAAEABgAAAFJlc3Vt" +
           "ZQEBDgEALwEBDgEOAQAAAQH/////AAAAAARggAoBAAAAAQAEAAAASWRsZQEBDwEALwEABQkPAQAAAwAA" +
           "AAAzAQEBFQEANAEBARcBADQBAQEbAQEAAAAVYKkKAgAAAAAACwAAAFN0YXRlTnVtYmVyAQEQAQAuAEQQ" +
           "AQAABwEAAAAAB/////8BAf////8AAAAABGCACgEAAAABAAoAAABJbnN0YWxsaW5nAQERAQAvAQADCREB" +
           "AAADAAAAADQBAQEVAQAzAQEBFwEAMwEBARkBAQAAABVgqQoCAAAAAAALAAAAU3RhdGVOdW1iZXIBARIB" +
           "AC4ARBIBAAAHAgAAAAAH/////wEB/////wAAAAAEYIAKAQAAAAEABQAAAEVycm9yAQETAQAvAQADCRMB" +
           "AAACAAAAADQBAQEZAQAzAQEBGwEBAAAAFWCpCgIAAAAAAAsAAABTdGF0ZU51bWJlcgEBFAEALgBEFAEA" +
           "AAcDAAAAAAf/////AQH/////AAAAAARggAoBAAAAAQAQAAAASWRsZVRvSW5zdGFsbGluZwEBFQEALwEA" +
           "BgkVAQAAAwAAAAAzAAEBDwEANAABAREBADYAAQAHCQEAAAAVYKkKAgAAAAAAEAAAAFRyYW5zaXRpb25O" +
           "dW1iZXIBAYMBAC4ARIMBAAAHDAAAAAAH/////wEB/////wAAAAAEYIAKAQAAAAEAEAAAAEluc3RhbGxp" +
           "bmdUb0lkbGUBARcBAC8BAAYJFwEAAAMAAAAANAABAQ8BADMAAQERAQA2AAEABwkBAAAAFWCpCgIAAAAA" +
           "ABAAAABUcmFuc2l0aW9uTnVtYmVyAQEYAQAuAEQYAQAABxUAAAAAB/////8BAf////8AAAAABGCACgEA" +
           "AAABABEAAABJbnN0YWxsaW5nVG9FcnJvcgEBGQEALwEABgkZAQAAAwAAAAAzAAEBEQEANAABARMBADYA" +
           "AQAHCQEAAAAVYKkKAgAAAAAAEAAAAFRyYW5zaXRpb25OdW1iZXIBARoBAC4ARBoBAAAHFwAAAAAH////" +
           "/wEB/////wAAAAAEYIAKAQAAAAEACwAAAEVycm9yVG9JZGxlAQEbAQAvAQAGCRsBAAADAAAAADQAAQEP" +
           "AQAzAAEBEwEANgABAAcJAQAAABVgqQoCAAAAAAAQAAAAVHJhbnNpdGlvbk51bWJlcgEBHAEALgBEHAEA" +
           "AAcfAAAAAAf/////AQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public BaseDataVariableState<byte> PercentComplete
        {
            get
            {
                return m_percentComplete;
            }

            set
            {
                if (!Object.ReferenceEquals(m_percentComplete, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_percentComplete = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<double> InstallationDelay
        {
            get
            {
                return m_installationDelay;
            }

            set
            {
                if (!Object.ReferenceEquals(m_installationDelay, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_installationDelay = value;
            }
        }

        /// <remarks />
        public InstallSoftwarePackageMethodState InstallSoftwarePackage
        {
            get
            {
                return m_installSoftwarePackageMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_installSoftwarePackageMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_installSoftwarePackageMethod = value;
            }
        }

        /// <remarks />
        public InstallFilesMethodState InstallFiles
        {
            get
            {
                return m_installFilesMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_installFilesMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_installFilesMethod = value;
            }
        }

        /// <remarks />
        public MethodState Resume
        {
            get
            {
                return m_resumeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_resumeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_resumeMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_percentComplete != null)
            {
                children.Add(m_percentComplete);
            }

            if (m_installationDelay != null)
            {
                children.Add(m_installationDelay);
            }

            if (m_installSoftwarePackageMethod != null)
            {
                children.Add(m_installSoftwarePackageMethod);
            }

            if (m_installFilesMethod != null)
            {
                children.Add(m_installFilesMethod);
            }

            if (m_resumeMethod != null)
            {
                children.Add(m_resumeMethod);
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
                case Opc.Ua.DI.BrowseNames.PercentComplete:
                {
                    if (createOrReplace)
                    {
                        if (PercentComplete == null)
                        {
                            if (replacement == null)
                            {
                                PercentComplete = new BaseDataVariableState<byte>(this);
                            }
                            else
                            {
                                PercentComplete = (BaseDataVariableState<byte>)replacement;
                            }
                        }
                    }

                    instance = PercentComplete;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.InstallationDelay:
                {
                    if (createOrReplace)
                    {
                        if (InstallationDelay == null)
                        {
                            if (replacement == null)
                            {
                                InstallationDelay = new BaseDataVariableState<double>(this);
                            }
                            else
                            {
                                InstallationDelay = (BaseDataVariableState<double>)replacement;
                            }
                        }
                    }

                    instance = InstallationDelay;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.InstallSoftwarePackage:
                {
                    if (createOrReplace)
                    {
                        if (InstallSoftwarePackage == null)
                        {
                            if (replacement == null)
                            {
                                InstallSoftwarePackage = new InstallSoftwarePackageMethodState(this);
                            }
                            else
                            {
                                InstallSoftwarePackage = (InstallSoftwarePackageMethodState)replacement;
                            }
                        }
                    }

                    instance = InstallSoftwarePackage;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.InstallFiles:
                {
                    if (createOrReplace)
                    {
                        if (InstallFiles == null)
                        {
                            if (replacement == null)
                            {
                                InstallFiles = new InstallFilesMethodState(this);
                            }
                            else
                            {
                                InstallFiles = (InstallFilesMethodState)replacement;
                            }
                        }
                    }

                    instance = InstallFiles;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.Resume:
                {
                    if (createOrReplace)
                    {
                        if (Resume == null)
                        {
                            if (replacement == null)
                            {
                                Resume = new MethodState(this);
                            }
                            else
                            {
                                Resume = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Resume;
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
        private BaseDataVariableState<byte> m_percentComplete;
        private BaseDataVariableState<double> m_installationDelay;
        private InstallSoftwarePackageMethodState m_installSoftwarePackageMethod;
        private InstallFilesMethodState m_installFilesMethod;
        private MethodState m_resumeMethod;
        #endregion
    }
    #endif
    #endregion

    #region PowerCycleStateMachineTypeState Class
    #if (!OPCUA_EXCLUDE_PowerCycleStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class PowerCycleStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public PowerCycleStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.PowerCycleStateMachineType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAiAAAAUG93" +
           "ZXJDeWNsZVN0YXRlTWFjaGluZVR5cGVJbnN0YW5jZQEBHQEBAR0BHQEAAP////8FAAAAFWCJCAIAAAAA" +
           "AAwAAABDdXJyZW50U3RhdGUBAQAAAC8BAMgKABX/////AQH/////AQAAABVgiQgCAAAAAAACAAAASWQB" +
           "AQAAAC4ARAAR/////wEB/////wAAAAAEYIAKAQAAAAEAFwAAAE5vdFdhaXRpbmdGb3JQb3dlckN5Y2xl" +
           "AQErAQAvAQAFCSsBAAACAAAAADMBAQEvAQA0AQEBMQEBAAAAFWCpCgIAAAAAAAsAAABTdGF0ZU51bWJl" +
           "cgEBLAEALgBELAEAAAcBAAAAAAf/////AQH/////AAAAAARggAoBAAAAAQAUAAAAV2FpdGluZ0ZvclBv" +
           "d2VyQ3ljbGUBAS0BAC8BAAMJLQEAAAIAAAAANAEBAS8BADMBAQExAQEAAAAVYKkKAgAAAAAACwAAAFN0" +
           "YXRlTnVtYmVyAQEuAQAuAEQuAQAABwIAAAAAB/////8BAf////8AAAAABGCACgEAAAABAC0AAABOb3RX" +
           "YWl0aW5nRm9yUG93ZXJDeWNsZVRvV2FpdGluZ0ZvclBvd2VyQ3ljbGUBAS8BAC8BAAYJLwEAAAMAAAAA" +
           "MwABASsBADQAAQEtAQA2AAEABwkBAAAAFWCpCgIAAAAAABAAAABUcmFuc2l0aW9uTnVtYmVyAQEwAQAu" +
           "AEQwAQAABwwAAAAAB/////8BAf////8AAAAABGCACgEAAAABAC0AAABXYWl0aW5nRm9yUG93ZXJDeWNs" +
           "ZVRvTm90V2FpdGluZ0ZvclBvd2VyQ3ljbGUBATEBAC8BAAYJMQEAAAMAAAAANAABASsBADMAAQEtAQA2" +
           "AAEABwkBAAAAFWCpCgIAAAAAABAAAABUcmFuc2l0aW9uTnVtYmVyAQEyAQAuAEQyAQAABxUAAAAAB///" +
           "//8BAf////8AAAAA";
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

    #region ConfirmationStateMachineTypeState Class
    #if (!OPCUA_EXCLUDE_ConfirmationStateMachineTypeState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ConfirmationStateMachineTypeState : FiniteStateMachineState
    {
        #region Constructors
        /// <remarks />
        public ConfirmationStateMachineTypeState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Opc.Ua.DI.ObjectTypes.ConfirmationStateMachineType, Opc.Ua.DI.Namespaces.OpcUaDI, namespaceUris);
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
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRggAIBAAAAAQAkAAAAQ29u" +
           "ZmlybWF0aW9uU3RhdGVNYWNoaW5lVHlwZUluc3RhbmNlAQEzAQEBMwEzAQAA/////wcAAAAVYIkIAgAA" +
           "AAAADAAAAEN1cnJlbnRTdGF0ZQEBAAAALwEAyAoAFf////8BAf////8BAAAAFWCJCAIAAAAAAAIAAABJ" +
           "ZAEBAAAALgBEABH/////AQH/////AAAAAARhggoEAAAAAQAHAAAAQ29uZmlybQEBQQEALwEBQQFBAQAA" +
           "AQH/////AAAAABVgiQoCAAAAAQATAAAAQ29uZmlybWF0aW9uVGltZW91dAEBQgEALwA/QgEAAAEAIgH/" +
           "////AQH/////AAAAAARggAoBAAAAAQAUAAAATm90V2FpdGluZ0ZvckNvbmZpcm0BAUMBAC8BAAUJQwEA" +
           "AAIAAAAAMwEBAUcBADQBAQFJAQEAAAAVYKkKAgAAAAAACwAAAFN0YXRlTnVtYmVyAQFEAQAuAEREAQAA" +
           "BwEAAAAAB/////8BAf////8AAAAABGCACgEAAAABABEAAABXYWl0aW5nRm9yQ29uZmlybQEBRQEALwEA" +
           "AwlFAQAAAgAAAAA0AQEBRwEAMwEBAUkBAQAAABVgqQoCAAAAAAALAAAAU3RhdGVOdW1iZXIBAUYBAC4A" +
           "REYBAAAHAgAAAAAH/////wEB/////wAAAAAEYIAKAQAAAAEAJwAAAE5vdFdhaXRpbmdGb3JDb25maXJt" +
           "VG9XYWl0aW5nRm9yQ29uZmlybQEBRwEALwEABglHAQAAAwAAAAAzAAEBQwEANAABAUUBADYAAQAHCQEA" +
           "AAAVYKkKAgAAAAAAEAAAAFRyYW5zaXRpb25OdW1iZXIBAUgBAC4AREgBAAAHDAAAAAAH/////wEB////" +
           "/wAAAAAEYIAKAQAAAAEAJwAAAFdhaXRpbmdGb3JDb25maXJtVG9Ob3RXYWl0aW5nRm9yQ29uZmlybQEB" +
           "SQEALwEABglJAQAAAwAAAAA0AAEBQwEAMwABAUUBADYAAQAHCQEAAAAVYKkKAgAAAAAAEAAAAFRyYW5z" +
           "aXRpb25OdW1iZXIBAUoBAC4AREoBAAAHFQAAAAAH/////wEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <remarks />
        public MethodState Confirm
        {
            get
            {
                return m_confirmMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_confirmMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_confirmMethod = value;
            }
        }

        /// <remarks />
        public BaseDataVariableState<double> ConfirmationTimeout
        {
            get
            {
                return m_confirmationTimeout;
            }

            set
            {
                if (!Object.ReferenceEquals(m_confirmationTimeout, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_confirmationTimeout = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <remarks />
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_confirmMethod != null)
            {
                children.Add(m_confirmMethod);
            }

            if (m_confirmationTimeout != null)
            {
                children.Add(m_confirmationTimeout);
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
                case Opc.Ua.DI.BrowseNames.Confirm:
                {
                    if (createOrReplace)
                    {
                        if (Confirm == null)
                        {
                            if (replacement == null)
                            {
                                Confirm = new MethodState(this);
                            }
                            else
                            {
                                Confirm = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Confirm;
                    break;
                }

                case Opc.Ua.DI.BrowseNames.ConfirmationTimeout:
                {
                    if (createOrReplace)
                    {
                        if (ConfirmationTimeout == null)
                        {
                            if (replacement == null)
                            {
                                ConfirmationTimeout = new BaseDataVariableState<double>(this);
                            }
                            else
                            {
                                ConfirmationTimeout = (BaseDataVariableState<double>)replacement;
                            }
                        }
                    }

                    instance = ConfirmationTimeout;
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
        private MethodState m_confirmMethod;
        private BaseDataVariableState<double> m_confirmationTimeout;
        #endregion
    }
    #endif
    #endregion

    #region InitLockMethodState Class
    #if (!OPCUA_EXCLUDE_InitLockMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class InitLockMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public InitLockMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new InitLockMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQASAAAASW5p" +
           "dExvY2tNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public InitLockMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            string context = (string)_inputArguments[0];

            int initLockStatus = (int)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    context,
                    ref initLockStatus);
            }

            _outputArguments[0] = initLockStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult InitLockMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string context,
        ref int initLockStatus);
    #endif
    #endregion

    #region RenewLockMethodState Class
    #if (!OPCUA_EXCLUDE_RenewLockMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class RenewLockMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public RenewLockMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new RenewLockMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQATAAAAUmVu" +
           "ZXdMb2NrTWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public RenewLockMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int renewLockStatus = (int)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref renewLockStatus);
            }

            _outputArguments[0] = renewLockStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult RenewLockMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref int renewLockStatus);
    #endif
    #endregion

    #region ExitLockMethodState Class
    #if (!OPCUA_EXCLUDE_ExitLockMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ExitLockMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public ExitLockMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new ExitLockMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQASAAAARXhp" +
           "dExvY2tNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public ExitLockMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int exitLockStatus = (int)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref exitLockStatus);
            }

            _outputArguments[0] = exitLockStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult ExitLockMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref int exitLockStatus);
    #endif
    #endregion

    #region BreakLockMethodState Class
    #if (!OPCUA_EXCLUDE_BreakLockMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class BreakLockMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public BreakLockMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new BreakLockMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQATAAAAQnJl" +
           "YWtMb2NrTWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public BreakLockMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int breakLockStatus = (int)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref breakLockStatus);
            }

            _outputArguments[0] = breakLockStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult BreakLockMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref int breakLockStatus);
    #endif
    #endregion

    #region TransferToDeviceMethodState Class
    #if (!OPCUA_EXCLUDE_TransferToDeviceMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TransferToDeviceMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public TransferToDeviceMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new TransferToDeviceMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAaAAAAVHJh" +
           "bnNmZXJUb0RldmljZU1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public TransferToDeviceMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int transferID = (int)_outputArguments[0];
            int initTransferStatus = (int)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref transferID,
                    ref initTransferStatus);
            }

            _outputArguments[0] = transferID;
            _outputArguments[1] = initTransferStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult TransferToDeviceMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref int transferID,
        ref int initTransferStatus);
    #endif
    #endregion

    #region TransferFromDeviceMethodState Class
    #if (!OPCUA_EXCLUDE_TransferFromDeviceMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TransferFromDeviceMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public TransferFromDeviceMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new TransferFromDeviceMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAcAAAAVHJh" +
           "bnNmZXJGcm9tRGV2aWNlTWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public TransferFromDeviceMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int transferID = (int)_outputArguments[0];
            int initTransferStatus = (int)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    ref transferID,
                    ref initTransferStatus);
            }

            _outputArguments[0] = transferID;
            _outputArguments[1] = initTransferStatus;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult TransferFromDeviceMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        ref int transferID,
        ref int initTransferStatus);
    #endif
    #endregion

    #region FetchTransferResultDataMethodState Class
    #if (!OPCUA_EXCLUDE_FetchTransferResultDataMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class FetchTransferResultDataMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public FetchTransferResultDataMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new FetchTransferResultDataMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAhAAAARmV0" +
           "Y2hUcmFuc2ZlclJlc3VsdERhdGFNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public FetchTransferResultDataMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            int transferID = (int)_inputArguments[0];
            int sequenceNumber = (int)_inputArguments[1];
            int maxParameterResultsToReturn = (int)_inputArguments[2];
            bool omitGoodResults = (bool)_inputArguments[3];

            ExtensionObject fetchResultData = (ExtensionObject)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    transferID,
                    sequenceNumber,
                    maxParameterResultsToReturn,
                    omitGoodResults,
                    ref fetchResultData);
            }

            _outputArguments[0] = fetchResultData;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult FetchTransferResultDataMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        int transferID,
        int sequenceNumber,
        int maxParameterResultsToReturn,
        bool omitGoodResults,
        ref ExtensionObject fetchResultData);
    #endif
    #endregion

    #region GetUpdateBehaviorMethodState Class
    #if (!OPCUA_EXCLUDE_GetUpdateBehaviorMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class GetUpdateBehaviorMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public GetUpdateBehaviorMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new GetUpdateBehaviorMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAbAAAAR2V0" +
           "VXBkYXRlQmVoYXZpb3JNZXRob2RUeXBlAQEAAAEBAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public GetUpdateBehaviorMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            string manufacturerUri = (string)_inputArguments[0];
            string softwareRevision = (string)_inputArguments[1];
            string[] patchIdentifiers = (string[])_inputArguments[2];

            uint updateBehavior = (uint)_outputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    manufacturerUri,
                    softwareRevision,
                    patchIdentifiers,
                    ref updateBehavior);
            }

            _outputArguments[0] = updateBehavior;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult GetUpdateBehaviorMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string manufacturerUri,
        string softwareRevision,
        string[] patchIdentifiers,
        ref uint updateBehavior);
    #endif
    #endregion

    #region ValidateFilesMethodState Class
    #if (!OPCUA_EXCLUDE_ValidateFilesMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ValidateFilesMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public ValidateFilesMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new ValidateFilesMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAXAAAAVmFs" +
           "aWRhdGVGaWxlc01ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public ValidateFilesMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            NodeId[] nodeIds = (NodeId[])_inputArguments[0];

            int errorCode = (int)_outputArguments[0];
            LocalizedText errorMessage = (LocalizedText)_outputArguments[1];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    nodeIds,
                    ref errorCode,
                    ref errorMessage);
            }

            _outputArguments[0] = errorCode;
            _outputArguments[1] = errorMessage;

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult ValidateFilesMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        NodeId[] nodeIds,
        ref int errorCode,
        ref LocalizedText errorMessage);
    #endif
    #endregion

    #region InstallSoftwarePackageMethodState Class
    #if (!OPCUA_EXCLUDE_InstallSoftwarePackageMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class InstallSoftwarePackageMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public InstallSoftwarePackageMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new InstallSoftwarePackageMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAgAAAASW5z" +
           "dGFsbFNvZnR3YXJlUGFja2FnZU1ldGhvZFR5cGUBAQAAAQEAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public InstallSoftwarePackageMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            string manufacturerUri = (string)_inputArguments[0];
            string softwareRevision = (string)_inputArguments[1];
            string[] patchIdentifiers = (string[])_inputArguments[2];
            byte[] hash = (byte[])_inputArguments[3];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    manufacturerUri,
                    softwareRevision,
                    patchIdentifiers,
                    hash);
            }

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult InstallSoftwarePackageMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        string manufacturerUri,
        string softwareRevision,
        string[] patchIdentifiers,
        byte[] hash);
    #endif
    #endregion

    #region InstallFilesMethodState Class
    #if (!OPCUA_EXCLUDE_InstallFilesMethodState)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class InstallFilesMethodState : MethodState
    {
        #region Constructors
        /// <remarks />
        public InstallFilesMethodState(NodeState parent) : base(parent)
        {
        }

        /// <remarks />
        public new static NodeState Construct(NodeState parent)
        {
            return new InstallFilesMethodState(parent);
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
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAAB8AAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvREkv/////wRhggAEAAAAAQAWAAAASW5z" +
           "dGFsbEZpbGVzTWV0aG9kVHlwZQEBAAABAQAAAQH/////AAAAAA==";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <remarks />
        public InstallFilesMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <remarks />
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

            NodeId[] nodeIds = (NodeId[])_inputArguments[0];

            if (OnCall != null)
            {
                _result = OnCall(
                    _context,
                    this,
                    _objectId,
                    nodeIds);
            }

            return _result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <remarks />
    /// <exclude />
    public delegate ServiceResult InstallFilesMethodStateMethodCallHandler(
        ISystemContext _context,
        MethodState _method,
        NodeId _objectId,
        NodeId[] nodeIds);
    #endif
    #endregion
}