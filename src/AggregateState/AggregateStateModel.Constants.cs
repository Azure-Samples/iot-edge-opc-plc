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
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace AggregateStateModel
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
        /// The identifier for the AggregateStateDataType DataType.
        /// </summary>
        public const uint AggregateStateDataType = 15001;
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
        /// The identifier for the AggregateState1 Object.
        /// </summary>
        public const uint AggregateState1 = 15004;

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint AggregateStateDataType_Encoding_DefaultBinary = 15006;

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint AggregateStateDataType_Encoding_DefaultXml = 15014;

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint AggregateStateDataType_Encoding_DefaultJson = 15022;
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
        /// The identifier for the AggregateStateType ObjectType.
        /// </summary>
        public const uint AggregateStateType = 15002;
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
        /// The identifier for the AggregateStateType_AggregateState Variable.
        /// </summary>
        public const uint AggregateStateType_AggregateState = 15003;

        /// <summary>
        /// The identifier for the AggregateState1_AggregateState Variable.
        /// </summary>
        public const uint AggregateState1_AggregateState = 15005;

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema Variable.
        /// </summary>
        public const uint AggregateState_BinarySchema = 15007;

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint AggregateState_BinarySchema_NamespaceUri = 15009;

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint AggregateState_BinarySchema_Deprecated = 15010;

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_AggregateStateDataType Variable.
        /// </summary>
        public const uint AggregateState_BinarySchema_AggregateStateDataType = 15011;

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema Variable.
        /// </summary>
        public const uint AggregateState_XmlSchema = 15015;

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint AggregateState_XmlSchema_NamespaceUri = 15017;

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint AggregateState_XmlSchema_Deprecated = 15018;

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_AggregateStateDataType Variable.
        /// </summary>
        public const uint AggregateState_XmlSchema_AggregateStateDataType = 15019;
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
        /// The identifier for the AggregateStateDataType DataType.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateDataType = new ExpandedNodeId(AggregateStateModel.DataTypes.AggregateStateDataType, AggregateStateModel.Namespaces.AggregateState);
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
        /// The identifier for the AggregateState1 Object.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState1 = new ExpandedNodeId(AggregateStateModel.Objects.AggregateState1, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateDataType_Encoding_DefaultBinary = new ExpandedNodeId(AggregateStateModel.Objects.AggregateStateDataType_Encoding_DefaultBinary, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateDataType_Encoding_DefaultXml = new ExpandedNodeId(AggregateStateModel.Objects.AggregateStateDataType_Encoding_DefaultXml, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateStateDataType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateDataType_Encoding_DefaultJson = new ExpandedNodeId(AggregateStateModel.Objects.AggregateStateDataType_Encoding_DefaultJson, AggregateStateModel.Namespaces.AggregateState);
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
        /// The identifier for the AggregateStateType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateType = new ExpandedNodeId(AggregateStateModel.ObjectTypes.AggregateStateType, AggregateStateModel.Namespaces.AggregateState);
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
        /// The identifier for the AggregateStateType_AggregateState Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateStateType_AggregateState = new ExpandedNodeId(AggregateStateModel.Variables.AggregateStateType_AggregateState, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState1_AggregateState Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState1_AggregateState = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState1_AggregateState, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_BinarySchema = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_BinarySchema, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_BinarySchema_NamespaceUri = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_BinarySchema_NamespaceUri, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_BinarySchema_Deprecated = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_BinarySchema_Deprecated, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_BinarySchema_AggregateStateDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_BinarySchema_AggregateStateDataType = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_BinarySchema_AggregateStateDataType, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_XmlSchema = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_XmlSchema, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_XmlSchema_NamespaceUri = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_XmlSchema_NamespaceUri, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_XmlSchema_Deprecated = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_XmlSchema_Deprecated, AggregateStateModel.Namespaces.AggregateState);

        /// <summary>
        /// The identifier for the AggregateState_XmlSchema_AggregateStateDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId AggregateState_XmlSchema_AggregateStateDataType = new ExpandedNodeId(AggregateStateModel.Variables.AggregateState_XmlSchema_AggregateStateDataType, AggregateStateModel.Namespaces.AggregateState);
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
        /// The BrowseName for the AggregateState component.
        /// </summary>
        public const string AggregateState = "AggregateState";

        /// <summary>
        /// The BrowseName for the AggregateState_BinarySchema component.
        /// </summary>
        public const string AggregateState_BinarySchema = "AggregateStateModel";

        /// <summary>
        /// The BrowseName for the AggregateState_XmlSchema component.
        /// </summary>
        public const string AggregateState_XmlSchema = "AggregateStateModel";

        /// <summary>
        /// The BrowseName for the AggregateState1 component.
        /// </summary>
        public const string AggregateState1 = "AggregateState #1";

        /// <summary>
        /// The BrowseName for the AggregateStateDataType component.
        /// </summary>
        public const string AggregateStateDataType = "AggregateStateDataType";

        /// <summary>
        /// The BrowseName for the AggregateStateType component.
        /// </summary>
        public const string AggregateStateType = "AggregateStateType";
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
        /// The URI for the AggregateState namespace (.NET code namespace is 'AggregateStateModel').
        /// </summary>
        public const string AggregateState = "http://microsoft.com/Opc/OpcPlc/AggregateState";
    }
    #endregion
}