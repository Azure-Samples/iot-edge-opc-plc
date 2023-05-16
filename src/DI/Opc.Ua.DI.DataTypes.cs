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
    #region DeviceHealthEnumeration Enumeration
    #if (!OPCUA_EXCLUDE_DeviceHealthEnumeration)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public enum DeviceHealthEnumeration
    {
        /// <remarks />
        [EnumMember(Value = "NORMAL_0")]
        NORMAL = 0,

        /// <remarks />
        [EnumMember(Value = "FAILURE_1")]
        FAILURE = 1,

        /// <remarks />
        [EnumMember(Value = "CHECK_FUNCTION_2")]
        CHECK_FUNCTION = 2,

        /// <remarks />
        [EnumMember(Value = "OFF_SPEC_3")]
        OFF_SPEC = 3,

        /// <remarks />
        [EnumMember(Value = "MAINTENANCE_REQUIRED_4")]
        MAINTENANCE_REQUIRED = 4,
    }

    #region DeviceHealthEnumerationCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfDeviceHealthEnumeration", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "DeviceHealthEnumeration")]
    public partial class DeviceHealthEnumerationCollection : List<DeviceHealthEnumeration>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public DeviceHealthEnumerationCollection() {}

        /// <remarks />
        public DeviceHealthEnumerationCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public DeviceHealthEnumerationCollection(IEnumerable<DeviceHealthEnumeration> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator DeviceHealthEnumerationCollection(DeviceHealthEnumeration[] values)
        {
            if (values != null)
            {
                return new DeviceHealthEnumerationCollection(values);
            }

            return new DeviceHealthEnumerationCollection();
        }

        /// <remarks />
        public static explicit operator DeviceHealthEnumeration[](DeviceHealthEnumerationCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (DeviceHealthEnumerationCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            DeviceHealthEnumerationCollection clone = new DeviceHealthEnumerationCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((DeviceHealthEnumeration)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region FetchResultDataType Class
    #if (!OPCUA_EXCLUDE_FetchResultDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public partial class FetchResultDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public FetchResultDataType()
        {
            Initialize();
        }
            
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }
            
        private void Initialize()
        {
        }
        #endregion

        #region Public Properties
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.FetchResultDataType; 

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.FetchResultDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.FetchResultDataType_Encoding_DefaultXml;
                    
        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.FetchResultDataType_Encoding_DefaultJson; 

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);


            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);


            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            FetchResultDataType value = encodeable as FetchResultDataType;

            if (value == null)
            {
                return false;
            }


            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (FetchResultDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            FetchResultDataType clone = (FetchResultDataType)base.MemberwiseClone();


            return clone;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    #region FetchResultDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfFetchResultDataType", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "FetchResultDataType")]
    public partial class FetchResultDataTypeCollection : List<FetchResultDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public FetchResultDataTypeCollection() {}

        /// <remarks />
        public FetchResultDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public FetchResultDataTypeCollection(IEnumerable<FetchResultDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator FetchResultDataTypeCollection(FetchResultDataType[] values)
        {
            if (values != null)
            {
                return new FetchResultDataTypeCollection(values);
            }

            return new FetchResultDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator FetchResultDataType[](FetchResultDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (FetchResultDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            FetchResultDataTypeCollection clone = new FetchResultDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((FetchResultDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region TransferResultErrorDataType Class
    #if (!OPCUA_EXCLUDE_TransferResultErrorDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public partial class TransferResultErrorDataType : Opc.Ua.DI.FetchResultDataType
    {
        #region Constructors
        /// <remarks />
        public TransferResultErrorDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            m_status = (int)0;
            m_diagnostics = null;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "Status", IsRequired = false, Order = 1)]
        public int Status
        {
            get { return m_status;  }
            set { m_status = value; }
        }

        /// <remarks />
        [DataMember(Name = "Diagnostics", IsRequired = false, Order = 2)]
        public DiagnosticInfo Diagnostics
        {
            get { return m_diagnostics;  }
            set { m_diagnostics = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public override ExpandedNodeId TypeId => DataTypeIds.TransferResultErrorDataType; 

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public override ExpandedNodeId BinaryEncodingId => ObjectIds.TransferResultErrorDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public override ExpandedNodeId XmlEncodingId => ObjectIds.TransferResultErrorDataType_Encoding_DefaultXml;
            
        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public override ExpandedNodeId JsonEncodingId => ObjectIds.TransferResultErrorDataType_Encoding_DefaultJson; 

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public override void Encode(IEncoder encoder)
        {
            base.Encode(encoder);

            encoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            encoder.WriteInt32("Status", Status);
            encoder.WriteDiagnosticInfo("Diagnostics", Diagnostics);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public override void Decode(IDecoder decoder)
        {
            base.Decode(decoder);

            decoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            Status = decoder.ReadInt32("Status");
            Diagnostics = decoder.ReadDiagnosticInfo("Diagnostics");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public override bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            TransferResultErrorDataType value = encodeable as TransferResultErrorDataType;

            if (value == null)
            {
                return false;
            }

            if (!base.IsEqual(encodeable)) return false;
            if (!Utils.IsEqual(m_status, value.m_status)) return false;
            if (!Utils.IsEqual(m_diagnostics, value.m_diagnostics)) return false;

            return base.IsEqual(encodeable);
        }    

        /// <summary cref="ICloneable.Clone" />
        public override object Clone()
        {
            return (TransferResultErrorDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            TransferResultErrorDataType clone = (TransferResultErrorDataType)base.MemberwiseClone();

            clone.m_status = (int)Utils.Clone(this.m_status);
            clone.m_diagnostics = (DiagnosticInfo)Utils.Clone(this.m_diagnostics);

            return clone;
        }
        #endregion

        #region Private Fields
        private int m_status;
        private DiagnosticInfo m_diagnostics;
        #endregion
    }

    #region TransferResultErrorDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfTransferResultErrorDataType", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "TransferResultErrorDataType")]
    public partial class TransferResultErrorDataTypeCollection : List<TransferResultErrorDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public TransferResultErrorDataTypeCollection() {}

        /// <remarks />
        public TransferResultErrorDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public TransferResultErrorDataTypeCollection(IEnumerable<TransferResultErrorDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator TransferResultErrorDataTypeCollection(TransferResultErrorDataType[] values)
        {
            if (values != null)
            {
                return new TransferResultErrorDataTypeCollection(values);
            }

            return new TransferResultErrorDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator TransferResultErrorDataType[](TransferResultErrorDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (TransferResultErrorDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            TransferResultErrorDataTypeCollection clone = new TransferResultErrorDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((TransferResultErrorDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region TransferResultDataDataType Class
    #if (!OPCUA_EXCLUDE_TransferResultDataDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public partial class TransferResultDataDataType : Opc.Ua.DI.FetchResultDataType
    {
        #region Constructors
        /// <remarks />
        public TransferResultDataDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            m_sequenceNumber = (int)0;
            m_endOfResults = true;
            m_parameterDefs = new ParameterResultDataTypeCollection();
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "SequenceNumber", IsRequired = false, Order = 1)]
        public int SequenceNumber
        {
            get { return m_sequenceNumber;  }
            set { m_sequenceNumber = value; }
        }

        /// <remarks />
        [DataMember(Name = "EndOfResults", IsRequired = false, Order = 2)]
        public bool EndOfResults
        {
            get { return m_endOfResults;  }
            set { m_endOfResults = value; }
        }

        /// <remarks />
        [DataMember(Name = "ParameterDefs", IsRequired = false, Order = 3)]
        public ParameterResultDataTypeCollection ParameterDefs
        {
            get
            {
                return m_parameterDefs;
            }

            set
            {
                m_parameterDefs = value;

                if (value == null)
                {
                    m_parameterDefs = new ParameterResultDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public override ExpandedNodeId TypeId => DataTypeIds.TransferResultDataDataType; 

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public override ExpandedNodeId BinaryEncodingId => ObjectIds.TransferResultDataDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public override ExpandedNodeId XmlEncodingId => ObjectIds.TransferResultDataDataType_Encoding_DefaultXml;
            
        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public override ExpandedNodeId JsonEncodingId => ObjectIds.TransferResultDataDataType_Encoding_DefaultJson; 

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public override void Encode(IEncoder encoder)
        {
            base.Encode(encoder);

            encoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            encoder.WriteInt32("SequenceNumber", SequenceNumber);
            encoder.WriteBoolean("EndOfResults", EndOfResults);
            encoder.WriteEncodeableArray("ParameterDefs", ParameterDefs.ToArray(), typeof(ParameterResultDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public override void Decode(IDecoder decoder)
        {
            base.Decode(decoder);

            decoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            SequenceNumber = decoder.ReadInt32("SequenceNumber");
            EndOfResults = decoder.ReadBoolean("EndOfResults");
            ParameterDefs = (ParameterResultDataTypeCollection)decoder.ReadEncodeableArray("ParameterDefs", typeof(ParameterResultDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public override bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            TransferResultDataDataType value = encodeable as TransferResultDataDataType;

            if (value == null)
            {
                return false;
            }

            if (!base.IsEqual(encodeable)) return false;
            if (!Utils.IsEqual(m_sequenceNumber, value.m_sequenceNumber)) return false;
            if (!Utils.IsEqual(m_endOfResults, value.m_endOfResults)) return false;
            if (!Utils.IsEqual(m_parameterDefs, value.m_parameterDefs)) return false;

            return base.IsEqual(encodeable);
        }    

        /// <summary cref="ICloneable.Clone" />
        public override object Clone()
        {
            return (TransferResultDataDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            TransferResultDataDataType clone = (TransferResultDataDataType)base.MemberwiseClone();

            clone.m_sequenceNumber = (int)Utils.Clone(this.m_sequenceNumber);
            clone.m_endOfResults = (bool)Utils.Clone(this.m_endOfResults);
            clone.m_parameterDefs = (ParameterResultDataTypeCollection)Utils.Clone(this.m_parameterDefs);

            return clone;
        }
        #endregion

        #region Private Fields
        private int m_sequenceNumber;
        private bool m_endOfResults;
        private ParameterResultDataTypeCollection m_parameterDefs;
        #endregion
    }

    #region TransferResultDataDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfTransferResultDataDataType", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "TransferResultDataDataType")]
    public partial class TransferResultDataDataTypeCollection : List<TransferResultDataDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public TransferResultDataDataTypeCollection() {}

        /// <remarks />
        public TransferResultDataDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public TransferResultDataDataTypeCollection(IEnumerable<TransferResultDataDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator TransferResultDataDataTypeCollection(TransferResultDataDataType[] values)
        {
            if (values != null)
            {
                return new TransferResultDataDataTypeCollection(values);
            }

            return new TransferResultDataDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator TransferResultDataDataType[](TransferResultDataDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (TransferResultDataDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            TransferResultDataDataTypeCollection clone = new TransferResultDataDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((TransferResultDataDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ParameterResultDataType Class
    #if (!OPCUA_EXCLUDE_ParameterResultDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public partial class ParameterResultDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ParameterResultDataType()
        {
            Initialize();
        }
            
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }
            
        private void Initialize()
        {
            m_nodePath = new QualifiedNameCollection();
            m_statusCode = StatusCodes.Good;
            m_diagnostics = null;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "NodePath", IsRequired = false, Order = 1)]
        public QualifiedNameCollection NodePath
        {
            get
            {
                return m_nodePath;
            }

            set
            {
                m_nodePath = value;

                if (value == null)
                {
                    m_nodePath = new QualifiedNameCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "StatusCode", IsRequired = false, Order = 2)]
        public StatusCode StatusCode
        {
            get { return m_statusCode;  }
            set { m_statusCode = value; }
        }

        /// <remarks />
        [DataMember(Name = "Diagnostics", IsRequired = false, Order = 3)]
        public DiagnosticInfo Diagnostics
        {
            get { return m_diagnostics;  }
            set { m_diagnostics = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ParameterResultDataType; 

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ParameterResultDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ParameterResultDataType_Encoding_DefaultXml;
                    
        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ParameterResultDataType_Encoding_DefaultJson; 

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            encoder.WriteQualifiedNameArray("NodePath", NodePath);
            encoder.WriteStatusCode("StatusCode", StatusCode);
            encoder.WriteDiagnosticInfo("Diagnostics", Diagnostics);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(Opc.Ua.DI.Namespaces.OpcUaDI);

            NodePath = decoder.ReadQualifiedNameArray("NodePath");
            StatusCode = decoder.ReadStatusCode("StatusCode");
            Diagnostics = decoder.ReadDiagnosticInfo("Diagnostics");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ParameterResultDataType value = encodeable as ParameterResultDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_nodePath, value.m_nodePath)) return false;
            if (!Utils.IsEqual(m_statusCode, value.m_statusCode)) return false;
            if (!Utils.IsEqual(m_diagnostics, value.m_diagnostics)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ParameterResultDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ParameterResultDataType clone = (ParameterResultDataType)base.MemberwiseClone();

            clone.m_nodePath = (QualifiedNameCollection)Utils.Clone(this.m_nodePath);
            clone.m_statusCode = (StatusCode)Utils.Clone(this.m_statusCode);
            clone.m_diagnostics = (DiagnosticInfo)Utils.Clone(this.m_diagnostics);

            return clone;
        }
        #endregion

        #region Private Fields
        private QualifiedNameCollection m_nodePath;
        private StatusCode m_statusCode;
        private DiagnosticInfo m_diagnostics;
        #endregion
    }

    #region ParameterResultDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfParameterResultDataType", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "ParameterResultDataType")]
    public partial class ParameterResultDataTypeCollection : List<ParameterResultDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ParameterResultDataTypeCollection() {}

        /// <remarks />
        public ParameterResultDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ParameterResultDataTypeCollection(IEnumerable<ParameterResultDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ParameterResultDataTypeCollection(ParameterResultDataType[] values)
        {
            if (values != null)
            {
                return new ParameterResultDataTypeCollection(values);
            }

            return new ParameterResultDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ParameterResultDataType[](ParameterResultDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ParameterResultDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ParameterResultDataTypeCollection clone = new ParameterResultDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ParameterResultDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region SoftwareVersionFileType Enumeration
    #if (!OPCUA_EXCLUDE_SoftwareVersionFileType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)]
    public enum SoftwareVersionFileType
    {
        /// <remarks />
        [EnumMember(Value = "Current_0")]
        Current = 0,

        /// <remarks />
        [EnumMember(Value = "Pending_1")]
        Pending = 1,

        /// <remarks />
        [EnumMember(Value = "Fallback_2")]
        Fallback = 2,
    }

    #region SoftwareVersionFileTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfSoftwareVersionFileType", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "SoftwareVersionFileType")]
    public partial class SoftwareVersionFileTypeCollection : List<SoftwareVersionFileType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public SoftwareVersionFileTypeCollection() {}

        /// <remarks />
        public SoftwareVersionFileTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public SoftwareVersionFileTypeCollection(IEnumerable<SoftwareVersionFileType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator SoftwareVersionFileTypeCollection(SoftwareVersionFileType[] values)
        {
            if (values != null)
            {
                return new SoftwareVersionFileTypeCollection(values);
            }

            return new SoftwareVersionFileTypeCollection();
        }

        /// <remarks />
        public static explicit operator SoftwareVersionFileType[](SoftwareVersionFileTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (SoftwareVersionFileTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            SoftwareVersionFileTypeCollection clone = new SoftwareVersionFileTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((SoftwareVersionFileType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region UpdateBehavior Enumeration
    #if (!OPCUA_EXCLUDE_UpdateBehavior)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Opc.Ua.DI.Namespaces.OpcUaDI)][Flags]
    public enum UpdateBehavior : UInt32
    {
        /// <remarks />
        [EnumMember(Value = "None_0")]
        None = 0,

        /// <remarks />
        [EnumMember(Value = "KeepsParameters_1")]
        KeepsParameters = 1,

        /// <remarks />
        [EnumMember(Value = "WillDisconnect_2")]
        WillDisconnect = 2,

        /// <remarks />
        [EnumMember(Value = "RequiresPowerCycle_4")]
        RequiresPowerCycle = 4,

        /// <remarks />
        [EnumMember(Value = "WillReboot_8")]
        WillReboot = 8,

        /// <remarks />
        [EnumMember(Value = "NeedsPreparation_16")]
        NeedsPreparation = 16,
    }

    #region UpdateBehaviorCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfUpdateBehavior", Namespace = Opc.Ua.DI.Namespaces.OpcUaDI, ItemName = "UpdateBehavior")]
    public partial class UpdateBehaviorCollection : List<UpdateBehavior>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public UpdateBehaviorCollection() {}

        /// <remarks />
        public UpdateBehaviorCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public UpdateBehaviorCollection(IEnumerable<UpdateBehavior> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator UpdateBehaviorCollection(UpdateBehavior[] values)
        {
            if (values != null)
            {
                return new UpdateBehaviorCollection(values);
            }

            return new UpdateBehaviorCollection();
        }

        /// <remarks />
        public static explicit operator UpdateBehavior[](UpdateBehaviorCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (UpdateBehaviorCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            UpdateBehaviorCollection clone = new UpdateBehaviorCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((UpdateBehavior)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion
}