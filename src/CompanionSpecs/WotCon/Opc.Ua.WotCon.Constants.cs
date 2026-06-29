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
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Opc.Ua.WotCon
{
    #region Method Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class Methods
    {
        public const uint WotConNamespaceMetadata_NamespaceFile_Open = 11;

        public const uint WotConNamespaceMetadata_NamespaceFile_Close = 14;

        public const uint WotConNamespaceMetadata_NamespaceFile_Read = 16;

        public const uint WotConNamespaceMetadata_NamespaceFile_Write = 19;

        public const uint WotConNamespaceMetadata_NamespaceFile_GetPosition = 21;

        public const uint WotConNamespaceMetadata_NamespaceFile_SetPosition = 24;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open = 152;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close = 155;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read = 157;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write = 160;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition = 162;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition = 165;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate = 167;

        public const uint WoTAssetConnectionManagementType_CreateAsset = 26;

        public const uint WoTAssetConnectionManagementType_DeleteAsset = 29;

        public const uint WoTAssetConnectionManagementType_DiscoverAssets = 41;

        public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint = 49;

        public const uint WoTAssetConnectionManagementType_ConnectionTest = 75;

        public const uint WoTAssetConnectionManagement_CreateAsset = 32;

        public const uint WoTAssetConnectionManagement_DeleteAsset = 35;

        public const uint IWoTAssetType_WoTFile_Open = 51;

        public const uint IWoTAssetType_WoTFile_Close = 54;

        public const uint IWoTAssetType_WoTFile_Read = 56;

        public const uint IWoTAssetType_WoTFile_Write = 59;

        public const uint IWoTAssetType_WoTFile_GetPosition = 61;

        public const uint IWoTAssetType_WoTFile_SetPosition = 64;

        public const uint IWoTAssetType_WoTFile_CloseAndUpdate = 106;

        public const uint WoTAssetFileType_CloseAndUpdate = 111;
    }
    #endregion

    #region Object Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class Objects
    {
        public const uint WotConNamespaceMetadata = 67;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder = 2;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile = 144;

        public const uint WoTAssetConnectionManagementType_Configuration = 78;

        public const uint WoTAssetConnectionManagement = 31;

        public const uint IWoTAssetType_WoTFile = 43;
    }
    #endregion

    #region ObjectType Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class ObjectTypes
    {
        public const uint WoTAssetConnectionManagementType = 1;

        public const uint WoTAssetConfigurationType = 105;

        public const uint IWoTAssetType = 42;

        public const uint WoTAssetFileType = 110;
    }
    #endregion

    #region ReferenceType Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class ReferenceTypes
    {
        public const uint HasWoTComponent = 142;
    }
    #endregion

    #region Variable Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class Variables
    {
        public const uint WotConNamespaceMetadata_NamespaceUri = 68;

        public const uint WotConNamespaceMetadata_NamespaceVersion = 69;

        public const uint WotConNamespaceMetadata_NamespacePublicationDate = 70;

        public const uint WotConNamespaceMetadata_IsNamespaceSubset = 71;

        public const uint WotConNamespaceMetadata_StaticNodeIdTypes = 72;

        public const uint WotConNamespaceMetadata_StaticNumericNodeIdRange = 73;

        public const uint WotConNamespaceMetadata_StaticStringNodeIdPattern = 74;

        public const uint WotConNamespaceMetadata_NamespaceFile_Size = 4;

        public const uint WotConNamespaceMetadata_NamespaceFile_Writable = 5;

        public const uint WotConNamespaceMetadata_NamespaceFile_UserWritable = 6;

        public const uint WotConNamespaceMetadata_NamespaceFile_OpenCount = 7;

        public const uint WotConNamespaceMetadata_NamespaceFile_Open_InputArguments = 12;

        public const uint WotConNamespaceMetadata_NamespaceFile_Open_OutputArguments = 13;

        public const uint WotConNamespaceMetadata_NamespaceFile_Close_InputArguments = 15;

        public const uint WotConNamespaceMetadata_NamespaceFile_Read_InputArguments = 17;

        public const uint WotConNamespaceMetadata_NamespaceFile_Read_OutputArguments = 18;

        public const uint WotConNamespaceMetadata_NamespaceFile_Write_InputArguments = 20;

        public const uint WotConNamespaceMetadata_NamespaceFile_GetPosition_InputArguments = 22;

        public const uint WotConNamespaceMetadata_NamespaceFile_GetPosition_OutputArguments = 23;

        public const uint WotConNamespaceMetadata_NamespaceFile_SetPosition_InputArguments = 25;

        public const uint WotConNamespaceMetadata_DefaultRolePermissions = 99;

        public const uint WotConNamespaceMetadata_DefaultUserRolePermissions = 100;

        public const uint WotConNamespaceMetadata_DefaultAccessRestrictions = 101;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Size = 145;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Writable = 146;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_UserWritable = 147;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_OpenCount = 148;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_InputArguments = 153;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_OutputArguments = 154;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close_InputArguments = 156;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_InputArguments = 158;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_OutputArguments = 159;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write_InputArguments = 161;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_InputArguments = 163;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_OutputArguments = 164;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition_InputArguments = 166;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate_InputArguments = 168;

        public const uint WoTAssetConnectionManagementType_WoTAssetName_Placeholder_AssetEndpoint = 169;

        public const uint WoTAssetConnectionManagementType_SupportedWoTBindings = 40;

        public const uint WoTAssetConnectionManagementType_CreateAsset_InputArguments = 27;

        public const uint WoTAssetConnectionManagementType_CreateAsset_OutputArguments = 28;

        public const uint WoTAssetConnectionManagementType_DeleteAsset_InputArguments = 30;

        public const uint WoTAssetConnectionManagementType_DiscoverAssets_OutputArguments = 48;

        public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint_InputArguments = 50;

        public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint_OutputArguments = 170;

        public const uint WoTAssetConnectionManagementType_ConnectionTest_InputArguments = 76;

        public const uint WoTAssetConnectionManagementType_ConnectionTest_OutputArguments = 77;

        public const uint WoTAssetConnectionManagement_CreateAsset_InputArguments = 33;

        public const uint WoTAssetConnectionManagement_CreateAsset_OutputArguments = 34;

        public const uint WoTAssetConnectionManagement_DeleteAsset_InputArguments = 36;

        public const uint WoTAssetConnectionManagement_DiscoverAssets_OutputArguments = 82;

        public const uint WoTAssetConnectionManagement_CreateAssetForEndpoint_InputArguments = 84;

        public const uint WoTAssetConnectionManagement_CreateAssetForEndpoint_OutputArguments = 171;

        public const uint WoTAssetConnectionManagement_ConnectionTest_InputArguments = 86;

        public const uint WoTAssetConnectionManagement_ConnectionTest_OutputArguments = 87;

        public const uint WoTAssetConfigurationType_WoTConfigurationParameterName_Placeholder = 108;

        public const uint WoTAssetConfigurationType_License = 109;

        public const uint IWoTAssetType_WoTFile_Size = 44;

        public const uint IWoTAssetType_WoTFile_Writable = 45;

        public const uint IWoTAssetType_WoTFile_UserWritable = 46;

        public const uint IWoTAssetType_WoTFile_OpenCount = 47;

        public const uint IWoTAssetType_WoTFile_Open_InputArguments = 52;

        public const uint IWoTAssetType_WoTFile_Open_OutputArguments = 53;

        public const uint IWoTAssetType_WoTFile_Close_InputArguments = 55;

        public const uint IWoTAssetType_WoTFile_Read_InputArguments = 57;

        public const uint IWoTAssetType_WoTFile_Read_OutputArguments = 58;

        public const uint IWoTAssetType_WoTFile_Write_InputArguments = 60;

        public const uint IWoTAssetType_WoTFile_GetPosition_InputArguments = 62;

        public const uint IWoTAssetType_WoTFile_GetPosition_OutputArguments = 63;

        public const uint IWoTAssetType_WoTFile_SetPosition_InputArguments = 65;

        public const uint IWoTAssetType_WoTFile_CloseAndUpdate_InputArguments = 107;

        public const uint IWoTAssetType_AssetEndpoint = 122;

        public const uint IWoTAssetType_WoTPropertyName_Placeholder = 66;

        public const uint WoTAssetFileType_CloseAndUpdate_InputArguments = 112;
    }
    #endregion

    #region Method Node Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class MethodIds
    {
        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Open = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_Open, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Close = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_Close, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Read = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_Read, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Write = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_Write, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_GetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_GetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_SetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WotConNamespaceMetadata_NamespaceFile_SetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAsset = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_CreateAsset, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_DeleteAsset = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_DeleteAsset, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_DiscoverAssets = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_DiscoverAssets, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAssetForEndpoint = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_CreateAssetForEndpoint, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_ConnectionTest = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagementType_ConnectionTest, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_CreateAsset = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagement_CreateAsset, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_DeleteAsset = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetConnectionManagement_DeleteAsset, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Open = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_Open, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Close = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_Close, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Read = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_Read, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Write = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_Write, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_GetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_GetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_SetPosition = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_SetPosition, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_CloseAndUpdate = new ExpandedNodeId(Opc.Ua.WotCon.Methods.IWoTAssetType_WoTFile_CloseAndUpdate, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetFileType_CloseAndUpdate = new ExpandedNodeId(Opc.Ua.WotCon.Methods.WoTAssetFileType_CloseAndUpdate, Opc.Ua.WotCon.Namespaces.WotCon);
    }
    #endregion

    #region Object Node Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class ObjectIds
    {
        public static readonly ExpandedNodeId WotConNamespaceMetadata = new ExpandedNodeId(Opc.Ua.WotCon.Objects.WotConNamespaceMetadata, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder = new ExpandedNodeId(Opc.Ua.WotCon.Objects.WoTAssetConnectionManagementType_WoTAssetName_Placeholder, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile = new ExpandedNodeId(Opc.Ua.WotCon.Objects.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_Configuration = new ExpandedNodeId(Opc.Ua.WotCon.Objects.WoTAssetConnectionManagementType_Configuration, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement = new ExpandedNodeId(Opc.Ua.WotCon.Objects.WoTAssetConnectionManagement, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile = new ExpandedNodeId(Opc.Ua.WotCon.Objects.IWoTAssetType_WoTFile, Opc.Ua.WotCon.Namespaces.WotCon);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class ObjectTypeIds
    {
        public static readonly ExpandedNodeId WoTAssetConnectionManagementType = new ExpandedNodeId(Opc.Ua.WotCon.ObjectTypes.WoTAssetConnectionManagementType, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConfigurationType = new ExpandedNodeId(Opc.Ua.WotCon.ObjectTypes.WoTAssetConfigurationType, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType = new ExpandedNodeId(Opc.Ua.WotCon.ObjectTypes.IWoTAssetType, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetFileType = new ExpandedNodeId(Opc.Ua.WotCon.ObjectTypes.WoTAssetFileType, Opc.Ua.WotCon.Namespaces.WotCon);
    }
    #endregion

    #region ReferenceType Node Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class ReferenceTypeIds
    {
        public static readonly ExpandedNodeId HasWoTComponent = new ExpandedNodeId(Opc.Ua.WotCon.ReferenceTypes.HasWoTComponent, Opc.Ua.WotCon.Namespaces.WotCon);
    }
    #endregion

    #region Variable Node Identifiers
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class VariableIds
    {
        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceUri = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceUri, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceVersion = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceVersion, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespacePublicationDate = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespacePublicationDate, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_IsNamespaceSubset = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_IsNamespaceSubset, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_StaticNodeIdTypes = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_StaticNodeIdTypes, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_StaticNumericNodeIdRange = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_StaticNumericNodeIdRange, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_StaticStringNodeIdPattern = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_StaticStringNodeIdPattern, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Size = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Size, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Writable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Writable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_UserWritable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_UserWritable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_OpenCount = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_OpenCount, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Open_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Open_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Open_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Open_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Close_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Close_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Read_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Read_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Read_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Read_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_Write_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_Write_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_GetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_GetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_GetPosition_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_GetPosition_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_NamespaceFile_SetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_NamespaceFile_SetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_DefaultRolePermissions = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_DefaultRolePermissions, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_DefaultUserRolePermissions = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_DefaultUserRolePermissions, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WotConNamespaceMetadata_DefaultAccessRestrictions = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WotConNamespaceMetadata_DefaultAccessRestrictions, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Size = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Size, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Writable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Writable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_UserWritable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_UserWritable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_OpenCount = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_OpenCount, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Open_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Close_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Read_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_Write_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_GetPosition_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_SetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_WoTFile_CloseAndUpdate_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_WoTAssetName_Placeholder_AssetEndpoint = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_WoTAssetName_Placeholder_AssetEndpoint, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_SupportedWoTBindings = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_SupportedWoTBindings, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAsset_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_CreateAsset_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAsset_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_CreateAsset_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_DeleteAsset_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_DeleteAsset_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_DiscoverAssets_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_DiscoverAssets_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAssetForEndpoint_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_CreateAssetForEndpoint_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_CreateAssetForEndpoint_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_CreateAssetForEndpoint_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_ConnectionTest_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_ConnectionTest_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagementType_ConnectionTest_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagementType_ConnectionTest_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_CreateAsset_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_CreateAsset_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_CreateAsset_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_CreateAsset_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_DeleteAsset_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_DeleteAsset_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_DiscoverAssets_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_DiscoverAssets_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_CreateAssetForEndpoint_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_CreateAssetForEndpoint_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_CreateAssetForEndpoint_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_CreateAssetForEndpoint_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_ConnectionTest_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_ConnectionTest_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConnectionManagement_ConnectionTest_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConnectionManagement_ConnectionTest_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConfigurationType_WoTConfigurationParameterName_Placeholder = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConfigurationType_WoTConfigurationParameterName_Placeholder, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetConfigurationType_License = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetConfigurationType_License, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Size = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Size, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Writable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Writable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_UserWritable = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_UserWritable, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_OpenCount = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_OpenCount, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Open_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Open_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Open_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Open_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Close_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Close_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Read_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Read_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Read_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Read_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_Write_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_Write_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_GetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_GetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_GetPosition_OutputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_GetPosition_OutputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_SetPosition_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_SetPosition_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTFile_CloseAndUpdate_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTFile_CloseAndUpdate_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_AssetEndpoint = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_AssetEndpoint, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId IWoTAssetType_WoTPropertyName_Placeholder = new ExpandedNodeId(Opc.Ua.WotCon.Variables.IWoTAssetType_WoTPropertyName_Placeholder, Opc.Ua.WotCon.Namespaces.WotCon);

        public static readonly ExpandedNodeId WoTAssetFileType_CloseAndUpdate_InputArguments = new ExpandedNodeId(Opc.Ua.WotCon.Variables.WoTAssetFileType_CloseAndUpdate_InputArguments, Opc.Ua.WotCon.Namespaces.WotCon);
    }
    #endregion

    #region BrowseName Declarations
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class BrowseNames
    {
        public const string AssetEndpoint = "AssetEndpoint";

        public const string CloseAndUpdate = "CloseAndUpdate";

        public const string Configuration = "Configuration";

        public const string ConnectionTest = "ConnectionTest";

        public const string CreateAsset = "CreateAsset";

        public const string CreateAssetForEndpoint = "CreateAssetForEndpoint";

        public const string DeleteAsset = "DeleteAsset";

        public const string DiscoverAssets = "DiscoverAssets";

        public const string HasWoTComponent = "HasWoTComponent";

        public const string IWoTAssetType = "IWoTAssetType";

        public const string License = "License";

        public const string SupportedWoTBindings = "SupportedWoTBindings";

        public const string WoTAssetConfigurationType = "WoTAssetConfigurationType";

        public const string WoTAssetConnectionManagement = "WoTAssetConnectionManagement";

        public const string WoTAssetConnectionManagementType = "WoTAssetConnectionManagementType";

        public const string WoTAssetFileType = "WoTAssetFileType";

        public const string WoTAssetName_Placeholder = "<WoTAssetName>";

        public const string WoTConfigurationParameterName_Placeholder = "<WoTConfigurationParameterName>";

        public const string WotConNamespaceMetadata = "http://opcfoundation.org/UA/WoT-Con/";

        public const string WoTFile = "WoTFile";

        public const string WoTPropertyName_Placeholder = "<WoTPropertyName>";
    }
    #endregion

    #region Namespace Declarations
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the WotCon namespace (.NET code namespace is 'Opc.Ua.WotCon').
        /// </summary>
        public const string WotCon = "http://opcfoundation.org/UA/WoT-Con/";

        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";
    }
    #endregion
}