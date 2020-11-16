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
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace BoilerModel
{
    #region DataType Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes
    {
        /// <summary>
        /// The identifier for the BoilerDataType DataType.
        /// </summary>
        public const uint BoilerDataType = 15032;

        /// <summary>
        /// The identifier for the BoilerTemperatureType DataType.
        /// </summary>
        public const uint BoilerTemperatureType = 15001;

        /// <summary>
        /// The identifier for the BoilerHeaterStateType DataType.
        /// </summary>
        public const uint BoilerHeaterStateType = 15014;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <summary>
        /// The identifier for the Boiler1 Object.
        /// </summary>
        public const uint Boiler1 = 15070;

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint BoilerDataType_Encoding_DefaultBinary = 15072;

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint BoilerTemperatureType_Encoding_DefaultBinary = 15004;

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint BoilerDataType_Encoding_DefaultXml = 15084;

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint BoilerTemperatureType_Encoding_DefaultXml = 15008;

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint BoilerDataType_Encoding_DefaultJson = 15096;

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint BoilerTemperatureType_Encoding_DefaultJson = 15012;
    }
    #endregion

    #region ObjectType Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <summary>
        /// The identifier for the BoilerType ObjectType.
        /// </summary>
        public const uint BoilerType = 15068;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <summary>
        /// The identifier for the BoilerHeaterStateType_EnumStrings Variable.
        /// </summary>
        public const uint BoilerHeaterStateType_EnumStrings = 15015;

        /// <summary>
        /// The identifier for the BoilerType_BoilerStatus Variable.
        /// </summary>
        public const uint BoilerType_BoilerStatus = 15003;

        /// <summary>
        /// The identifier for the Boiler1_BoilerStatus Variable.
        /// </summary>
        public const uint Boiler1_BoilerStatus = 15013;

        /// <summary>
        /// The identifier for the Boiler_BinarySchema Variable.
        /// </summary>
        public const uint Boiler_BinarySchema = 15074;

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint Boiler_BinarySchema_NamespaceUri = 15076;

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint Boiler_BinarySchema_Deprecated = 15077;

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_BoilerDataType Variable.
        /// </summary>
        public const uint Boiler_BinarySchema_BoilerDataType = 15078;

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_BoilerTemperatureType Variable.
        /// </summary>
        public const uint Boiler_BinarySchema_BoilerTemperatureType = 15005;

        /// <summary>
        /// The identifier for the Boiler_XmlSchema Variable.
        /// </summary>
        public const uint Boiler_XmlSchema = 15086;

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint Boiler_XmlSchema_NamespaceUri = 15088;

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint Boiler_XmlSchema_Deprecated = 15089;

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_BoilerDataType Variable.
        /// </summary>
        public const uint Boiler_XmlSchema_BoilerDataType = 15090;

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_BoilerTemperatureType Variable.
        /// </summary>
        public const uint Boiler_XmlSchema_BoilerTemperatureType = 15009;
    }
    #endregion

    #region DataType Node Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds
    {
        /// <summary>
        /// The identifier for the BoilerDataType DataType.
        /// </summary>
        public static readonly ExpandedNodeId BoilerDataType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerTemperatureType DataType.
        /// </summary>
        public static readonly ExpandedNodeId BoilerTemperatureType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerTemperatureType, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerHeaterStateType DataType.
        /// </summary>
        public static readonly ExpandedNodeId BoilerHeaterStateType = new ExpandedNodeId(BoilerModel.DataTypes.BoilerHeaterStateType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <summary>
        /// The identifier for the Boiler1 Object.
        /// </summary>
        public static readonly ExpandedNodeId Boiler1 = new ExpandedNodeId(BoilerModel.Objects.Boiler1, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultBinary = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultBinary, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultBinary = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultBinary, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultXml = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultXml, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultXml = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultXml, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerDataType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerDataType_Encoding_DefaultJson = new ExpandedNodeId(BoilerModel.Objects.BoilerDataType_Encoding_DefaultJson, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerTemperatureType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId BoilerTemperatureType_Encoding_DefaultJson = new ExpandedNodeId(BoilerModel.Objects.BoilerTemperatureType_Encoding_DefaultJson, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <summary>
        /// The identifier for the BoilerType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId BoilerType = new ExpandedNodeId(BoilerModel.ObjectTypes.BoilerType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <summary>
        /// The identifier for the BoilerHeaterStateType_EnumStrings Variable.
        /// </summary>
        public static readonly ExpandedNodeId BoilerHeaterStateType_EnumStrings = new ExpandedNodeId(BoilerModel.Variables.BoilerHeaterStateType_EnumStrings, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the BoilerType_BoilerStatus Variable.
        /// </summary>
        public static readonly ExpandedNodeId BoilerType_BoilerStatus = new ExpandedNodeId(BoilerModel.Variables.BoilerType_BoilerStatus, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler1_BoilerStatus Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler1_BoilerStatus = new ExpandedNodeId(BoilerModel.Variables.Boiler1_BoilerStatus, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_BinarySchema = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_BinarySchema_NamespaceUri = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_NamespaceUri, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_BinarySchema_Deprecated = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_Deprecated, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_BoilerDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_BinarySchema_BoilerDataType = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_BinarySchema_BoilerTemperatureType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_BinarySchema_BoilerTemperatureType = new ExpandedNodeId(BoilerModel.Variables.Boiler_BinarySchema_BoilerTemperatureType, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_XmlSchema = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_XmlSchema_NamespaceUri = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_NamespaceUri, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_XmlSchema_Deprecated = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_Deprecated, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_BoilerDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_XmlSchema_BoilerDataType = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_BoilerDataType, BoilerModel.Namespaces.Boiler);

        /// <summary>
        /// The identifier for the Boiler_XmlSchema_BoilerTemperatureType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Boiler_XmlSchema_BoilerTemperatureType = new ExpandedNodeId(BoilerModel.Variables.Boiler_XmlSchema_BoilerTemperatureType, BoilerModel.Namespaces.Boiler);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <summary>
        /// The BrowseName for the Boiler_BinarySchema component.
        /// </summary>
        public const string Boiler_BinarySchema = "BoilerModel";

        /// <summary>
        /// The BrowseName for the Boiler_XmlSchema component.
        /// </summary>
        public const string Boiler_XmlSchema = "BoilerModel";

        /// <summary>
        /// The BrowseName for the Boiler1 component.
        /// </summary>
        public const string Boiler1 = "Boiler #1";

        /// <summary>
        /// The BrowseName for the BoilerDataType component.
        /// </summary>
        public const string BoilerDataType = "BoilerDataType";

        /// <summary>
        /// The BrowseName for the BoilerHeaterStateType component.
        /// </summary>
        public const string BoilerHeaterStateType = "BoilerHeaterStateType";

        /// <summary>
        /// The BrowseName for the BoilerStatus component.
        /// </summary>
        public const string BoilerStatus = "BoilerStatus";

        /// <summary>
        /// The BrowseName for the BoilerTemperatureType component.
        /// </summary>
        public const string BoilerTemperatureType = "BoilerTemperatureType";

        /// <summary>
        /// The BrowseName for the BoilerType component.
        /// </summary>
        public const string BoilerType = "BoilerType";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
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