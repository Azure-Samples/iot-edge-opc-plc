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
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace BoilerModel
{
    #region DataType Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes
    {
        /// <remarks />
        public const uint BoilerDataType = 15032;

        /// <remarks />
        public const uint BoilerTemperatureType = 15001;

        /// <remarks />
        public const uint BoilerHeaterStateType = 15014;
    }
    #endregion

    #region Object Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <remarks />
        public const uint Boiler1 = 15070;

        /// <remarks />
        public const uint BoilerDataType_Encoding_DefaultBinary = 15072;

        /// <remarks />
        public const uint BoilerTemperatureType_Encoding_DefaultBinary = 15004;

        /// <remarks />
        public const uint BoilerDataType_Encoding_DefaultXml = 15084;

        /// <remarks />
        public const uint BoilerTemperatureType_Encoding_DefaultXml = 15008;

        /// <remarks />
        public const uint BoilerDataType_Encoding_DefaultJson = 15096;

        /// <remarks />
        public const uint BoilerTemperatureType_Encoding_DefaultJson = 15012;
    }
    #endregion

    #region ObjectType Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <remarks />
        public const uint BoilerType = 15068;
    }
    #endregion

    #region Variable Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <remarks />
        public const uint BoilerHeaterStateType_EnumStrings = 15015;

        /// <remarks />
        public const uint BoilerType_BoilerStatus = 15003;

        /// <remarks />
        public const uint Boiler1_BoilerStatus = 15013;

        /// <remarks />
        public const uint Boiler_BinarySchema = 15074;

        /// <remarks />
        public const uint Boiler_BinarySchema_NamespaceUri = 15076;

        /// <remarks />
        public const uint Boiler_BinarySchema_Deprecated = 15077;

        /// <remarks />
        public const uint Boiler_BinarySchema_BoilerDataType = 15078;

        /// <remarks />
        public const uint Boiler_BinarySchema_BoilerTemperatureType = 15005;

        /// <remarks />
        public const uint Boiler_XmlSchema = 15086;

        /// <remarks />
        public const uint Boiler_XmlSchema_NamespaceUri = 15088;

        /// <remarks />
        public const uint Boiler_XmlSchema_Deprecated = 15089;

        /// <remarks />
        public const uint Boiler_XmlSchema_BoilerDataType = 15090;

        /// <remarks />
        public const uint Boiler_XmlSchema_BoilerTemperatureType = 15009;
    }
    #endregion

    #region DataType Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId BoilerDataType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerTemperatureType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerTemperatureType, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerHeaterStateType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerHeaterStateType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region Object Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId Boiler1 = new ExpandedNodeId(BoilerModel.Objects.Boiler1, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultBinary = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultBinary, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultBinary = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultBinary, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultXml = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultXml, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultXml = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultXml, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultJson = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultJson, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultJson = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultJson, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId BoilerType = new ExpandedNodeId(BoilerModel.ObjectTypes.BoilerType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region Variable Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId BoilerHeaterStateType_EnumStrings = new ExpandedNodeId(BoilerModel.Variables.BoilerHeaterStateType_EnumStrings, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId BoilerType_BoilerStatus = new ExpandedNodeId(BoilerModel.Variables.BoilerType_BoilerStatus, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler1_BoilerStatus = new ExpandedNodeId(BoilerModel.Variables.Boiler1_BoilerStatus, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_BinarySchema = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_BinarySchema_NamespaceUri = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_NamespaceUri, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_BinarySchema_Deprecated = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_Deprecated, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_BinarySchema_BoilerDataType = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_BinarySchema_BoilerTemperatureType = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_BoilerTemperatureType, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_XmlSchema = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_XmlSchema_NamespaceUri = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_NamespaceUri, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_XmlSchema_Deprecated = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_Deprecated, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_XmlSchema_BoilerDataType = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler_XmlSchema_BoilerTemperatureType = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_BoilerTemperatureType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region BrowseName Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <remarks />
        public const string Boiler_BinarySchema = "BoilerModel";

        /// <remarks />
        public const string Boiler_XmlSchema = "BoilerModel";

        /// <remarks />
        public const string Boiler1 = "Boiler #1";

        /// <remarks />
        public const string BoilerDataType = "BoilerDataType";

        /// <remarks />
        public const string BoilerHeaterStateType = "BoilerHeaterStateType";

        /// <remarks />
        public const string BoilerStatus = "BoilerStatus";

        /// <remarks />
        public const string BoilerTemperatureType = "BoilerTemperatureType";

        /// <remarks />
        public const string BoilerType = "BoilerType";
    }
    #endregion

    #region Namespace Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the Boiler namespace (.NET code namespace is 'BoilerModel').
        /// </summary>
        public const string Boiler = "http://microsoft.com/Opc/OpcPlc/Boiler";
    }
    #endregion
}