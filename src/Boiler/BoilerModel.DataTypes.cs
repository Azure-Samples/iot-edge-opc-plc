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

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace BoilerModel
{
    #region BoilerDataType Class
    #if (!OPCUA_EXCLUDE_BoilerDataType)
    /// <summary>
    /// Temperature in °C, pressure in Pa and heater state.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = BoilerModel.Namespaces.Boiler)]
    public partial class BoilerDataType : IEncodeable
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public BoilerDataType()
        {
            Initialize();
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_temperature = new BoilerTemperatureType();
            m_pressure = (int)0;
            m_heaterState = BoilerHeaterStateType.Off;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "Temperature", IsRequired = false, Order = 1)]
        public BoilerTemperatureType Temperature
        {
            get
            {
                return m_temperature;
            }

            set
            {
                m_temperature = value;

                if (value == null)
                {
                    m_temperature = new BoilerTemperatureType();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Pressure", IsRequired = false, Order = 2)]
        public int Pressure
        {
            get { return m_pressure;  }
            set { m_pressure = value; }
        }

        /// <remarks />
        [DataMember(Name = "HeaterState", IsRequired = false, Order = 3)]
        public BoilerHeaterStateType HeaterState
        {
            get { return m_heaterState;  }
            set { m_heaterState = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId
        {
            get { return DataTypeIds.BoilerDataType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId
        {
            get { return ObjectIds.BoilerDataType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId
        {
            get { return ObjectIds.BoilerDataType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(BoilerModel.Namespaces.Boiler);

            encoder.WriteEncodeable("Temperature", Temperature, typeof(BoilerTemperatureType));
            encoder.WriteInt32("Pressure", Pressure);
            encoder.WriteEnumerated("HeaterState", HeaterState);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(BoilerModel.Namespaces.Boiler);

            Temperature = (BoilerTemperatureType)decoder.ReadEncodeable("Temperature", typeof(BoilerTemperatureType));
            Pressure = decoder.ReadInt32("Pressure");
            HeaterState = (BoilerHeaterStateType)decoder.ReadEnumerated("HeaterState", typeof(BoilerHeaterStateType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            BoilerDataType value = encodeable as BoilerDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_temperature, value.m_temperature)) return false;
            if (!Utils.IsEqual(m_pressure, value.m_pressure)) return false;
            if (!Utils.IsEqual(m_heaterState, value.m_heaterState)) return false;

            return true;
        }

        #if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (BoilerDataType)this.MemberwiseClone();
        }
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            BoilerDataType clone = (BoilerDataType)base.MemberwiseClone();

            clone.m_temperature = (BoilerTemperatureType)Utils.Clone(this.m_temperature);
            clone.m_pressure = (int)Utils.Clone(this.m_pressure);
            clone.m_heaterState = (BoilerHeaterStateType)Utils.Clone(this.m_heaterState);

            return clone;
        }
        #endregion

        #region Private Fields
        private BoilerTemperatureType m_temperature;
        private int m_pressure;
        private BoilerHeaterStateType m_heaterState;
        #endregion
    }

    #region BoilerDataTypeCollection Class
    /// <summary>
    /// A collection of BoilerDataType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfBoilerDataType", Namespace = BoilerModel.Namespaces.Boiler, ItemName = "BoilerDataType")]
    #if !NET_STANDARD
    public partial class BoilerDataTypeCollection : List<BoilerDataType>, ICloneable
    #else
    public partial class BoilerDataTypeCollection : List<BoilerDataType>
    #endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public BoilerDataTypeCollection() {}

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public BoilerDataTypeCollection(int capacity) : base(capacity) {}

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public BoilerDataTypeCollection(IEnumerable<BoilerDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator BoilerDataTypeCollection(BoilerDataType[] values)
        {
            if (values != null)
            {
                return new BoilerDataTypeCollection(values);
            }

            return new BoilerDataTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator BoilerDataType[](BoilerDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (BoilerDataTypeCollection)this.MemberwiseClone();
        }
        #endregion
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            BoilerDataTypeCollection clone = new BoilerDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((BoilerDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region BoilerTemperatureType Class
    #if (!OPCUA_EXCLUDE_BoilerTemperatureType)
    /// <summary>
    /// Temperature in °C next to the heater at the bottom, and away from the heater at the top.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = BoilerModel.Namespaces.Boiler)]
    public partial class BoilerTemperatureType : IEncodeable
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public BoilerTemperatureType()
        {
            Initialize();
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_top = (int)0;
            m_bottom = (int)0;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "Top", IsRequired = false, Order = 1)]
        public int Top
        {
            get { return m_top;  }
            set { m_top = value; }
        }

        /// <remarks />
        [DataMember(Name = "Bottom", IsRequired = false, Order = 2)]
        public int Bottom
        {
            get { return m_bottom;  }
            set { m_bottom = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId
        {
            get { return DataTypeIds.BoilerTemperatureType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId
        {
            get { return ObjectIds.BoilerTemperatureType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId
        {
            get { return ObjectIds.BoilerTemperatureType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(BoilerModel.Namespaces.Boiler);

            encoder.WriteInt32("Top", Top);
            encoder.WriteInt32("Bottom", Bottom);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(BoilerModel.Namespaces.Boiler);

            Top = decoder.ReadInt32("Top");
            Bottom = decoder.ReadInt32("Bottom");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            BoilerTemperatureType value = encodeable as BoilerTemperatureType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_top, value.m_top)) return false;
            if (!Utils.IsEqual(m_bottom, value.m_bottom)) return false;

            return true;
        }

        #if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (BoilerTemperatureType)this.MemberwiseClone();
        }
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            BoilerTemperatureType clone = (BoilerTemperatureType)base.MemberwiseClone();

            clone.m_top = (int)Utils.Clone(this.m_top);
            clone.m_bottom = (int)Utils.Clone(this.m_bottom);

            return clone;
        }
        #endregion

        #region Private Fields
        private int m_top;
        private int m_bottom;
        #endregion
    }

    #region BoilerTemperatureTypeCollection Class
    /// <summary>
    /// A collection of BoilerTemperatureType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfBoilerTemperatureType", Namespace = BoilerModel.Namespaces.Boiler, ItemName = "BoilerTemperatureType")]
    #if !NET_STANDARD
    public partial class BoilerTemperatureTypeCollection : List<BoilerTemperatureType>, ICloneable
    #else
    public partial class BoilerTemperatureTypeCollection : List<BoilerTemperatureType>
    #endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public BoilerTemperatureTypeCollection() {}

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public BoilerTemperatureTypeCollection(int capacity) : base(capacity) {}

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public BoilerTemperatureTypeCollection(IEnumerable<BoilerTemperatureType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator BoilerTemperatureTypeCollection(BoilerTemperatureType[] values)
        {
            if (values != null)
            {
                return new BoilerTemperatureTypeCollection(values);
            }

            return new BoilerTemperatureTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator BoilerTemperatureType[](BoilerTemperatureTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (BoilerTemperatureTypeCollection)this.MemberwiseClone();
        }
        #endregion
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            BoilerTemperatureTypeCollection clone = new BoilerTemperatureTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((BoilerTemperatureType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region BoilerHeaterStateType Enumeration
    #if (!OPCUA_EXCLUDE_BoilerHeaterStateType)
    /// <summary>
    /// Heater working state.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = BoilerModel.Namespaces.Boiler)]
    public enum BoilerHeaterStateType
    {
        /// <remarks />
        [EnumMember(Value = "Off_0")]
        Off = 0,

        /// <remarks />
        [EnumMember(Value = "On_1")]
        On = 1,
    }

    #region BoilerHeaterStateTypeCollection Class
    /// <summary>
    /// A collection of BoilerHeaterStateType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfBoilerHeaterStateType", Namespace = BoilerModel.Namespaces.Boiler, ItemName = "BoilerHeaterStateType")]
    #if !NET_STANDARD
    public partial class BoilerHeaterStateTypeCollection : List<BoilerHeaterStateType>, ICloneable
    #else
    public partial class BoilerHeaterStateTypeCollection : List<BoilerHeaterStateType>
    #endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public BoilerHeaterStateTypeCollection() {}

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public BoilerHeaterStateTypeCollection(int capacity) : base(capacity) {}

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public BoilerHeaterStateTypeCollection(IEnumerable<BoilerHeaterStateType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator BoilerHeaterStateTypeCollection(BoilerHeaterStateType[] values)
        {
            if (values != null)
            {
                return new BoilerHeaterStateTypeCollection(values);
            }

            return new BoilerHeaterStateTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator BoilerHeaterStateType[](BoilerHeaterStateTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (BoilerHeaterStateTypeCollection)this.MemberwiseClone();
        }
        #endregion
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            BoilerHeaterStateTypeCollection clone = new BoilerHeaterStateTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((BoilerHeaterStateType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion
}