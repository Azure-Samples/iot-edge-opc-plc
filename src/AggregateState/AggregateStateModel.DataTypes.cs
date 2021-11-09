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
    #region AggregateStateDataType Class
    #if (!OPCUA_EXCLUDE_AggregateStateDataType)
    /// <summary>
    /// Temperature in °C, pressure in bar.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = AggregateStateModel.Namespaces.AggregateState)]
    public partial class AggregateStateDataType : IEncodeable
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public AggregateStateDataType()
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
            m_temperature = (short)0;
            m_pressure = (float)0;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the Temperature field.
        /// </summary>
        [DataMember(Name = "Temperature", IsRequired = false, Order = 1)]
        public short Temperature
        {
            get { return m_temperature;  }
            set { m_temperature = value; }
        }

        /// <summary>
        /// A description for the Pressure field.
        /// </summary>
        [DataMember(Name = "Pressure", IsRequired = false, Order = 2)]
        public float Pressure
        {
            get { return m_pressure;  }
            set { m_pressure = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId
        {
            get { return DataTypeIds.AggregateStateDataType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId
        {
            get { return ObjectIds.AggregateStateDataType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId
        {
            get { return ObjectIds.AggregateStateDataType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(AggregateStateModel.Namespaces.AggregateState);

            encoder.WriteInt16("Temperature", Temperature);
            encoder.WriteFloat("Pressure", Pressure);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(AggregateStateModel.Namespaces.AggregateState);

            Temperature = decoder.ReadInt16("Temperature");
            Pressure = decoder.ReadFloat("Pressure");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            AggregateStateDataType value = encodeable as AggregateStateDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_temperature, value.m_temperature)) return false;
            if (!Utils.IsEqual(m_pressure, value.m_pressure)) return false;

            return true;
        }

        #if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (AggregateStateDataType)this.MemberwiseClone();
        }
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            AggregateStateDataType clone = (AggregateStateDataType)base.MemberwiseClone();

            clone.m_temperature = (short)Utils.Clone(this.m_temperature);
            clone.m_pressure = (float)Utils.Clone(this.m_pressure);

            return clone;
        }
        #endregion

        #region Private Fields
        private short m_temperature;
        private float m_pressure;
        #endregion
    }

    #region AggregateStateDataTypeCollection Class
    /// <summary>
    /// A collection of AggregateStateDataType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfAggregateStateDataType", Namespace = AggregateStateModel.Namespaces.AggregateState, ItemName = "AggregateStateDataType")]
    #if !NET_STANDARD
    public partial class AggregateStateDataTypeCollection : List<AggregateStateDataType>, ICloneable
    #else
    public partial class AggregateStateDataTypeCollection : List<AggregateStateDataType>
    #endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public AggregateStateDataTypeCollection() {}

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public AggregateStateDataTypeCollection(int capacity) : base(capacity) {}

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public AggregateStateDataTypeCollection(IEnumerable<AggregateStateDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator AggregateStateDataTypeCollection(AggregateStateDataType[] values)
        {
            if (values != null)
            {
                return new AggregateStateDataTypeCollection(values);
            }

            return new AggregateStateDataTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator AggregateStateDataType[](AggregateStateDataTypeCollection values)
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
            return (AggregateStateDataTypeCollection)this.MemberwiseClone();
        }
        #endregion
        #endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            AggregateStateDataTypeCollection clone = new AggregateStateDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((AggregateStateDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion
}