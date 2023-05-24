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
using Opc.Ua.DI;

namespace BoilerModel2
{
    #region Method Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Methods
    {
        /// <remarks />
        public const uint Boiler2Type_Lock_InitLock = 6166;

        /// <remarks />
        public const uint Boiler2Type_Lock_RenewLock = 6169;

        /// <remarks />
        public const uint Boiler2Type_Lock_ExitLock = 6171;

        /// <remarks />
        public const uint Boiler2Type_Lock_BreakLock = 6173;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_InitLock = 6166;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_RenewLock = 6169;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_ExitLock = 6171;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_BreakLock = 6173;

        /// <remarks />
        public const uint Boiler2Type_MethodSet_Switch = 7000;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_InitLock = 6166;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_RenewLock = 6169;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_ExitLock = 6171;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_BreakLock = 6173;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Disable = 7025;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Enable = 7026;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment = 7024;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge = 7027;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Disable = 7021;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Enable = 7022;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment = 7020;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge = 7023;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Disable = 7033;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Enable = 7034;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment = 7032;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge = 7035;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Disable = 7029;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Enable = 7030;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment = 7028;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge = 7031;

        /// <remarks />
        public const uint Boilers_Boiler__2_MethodSet_Switch = 7019;
    }
    #endregion

    #region Object Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler = 5000;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet = 5014;

        /// <remarks />
        public const uint Boiler2Type_MethodSet = 5013;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_NetworkAddress = 6592;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealthAlarms = 5002;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealthAlarms_FailureAlarm = 5003;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealthAlarms_CheckFunctionAlarm = 5004;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealthAlarms_OffSpecAlarm = 5005;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealthAlarms_MaintenanceRequiredAlarm = 5006;

        /// <remarks />
        public const uint Boilers = 1;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet = 5020;

        /// <remarks />
        public const uint Boilers_Boiler__2_MethodSet = 5019;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms = 5018;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm = 5022;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm = 5021;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm = 5024;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm = 5023;
    }
    #endregion

    #region ObjectType Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <remarks />
        public const uint Boiler2Type = 1000;
    }
    #endregion

    #region Variable Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_NamespaceUri = 6002;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_NamespaceVersion = 6003;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_NamespacePublicationDate = 6001;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_IsNamespaceSubset = 6000;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_StaticNodeIdTypes = 6004;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_StaticNumericNodeIdRange = 6005;

        /// <remarks />
        public const uint http___microsoft_com_Opc_OpcPlc_boiler_StaticStringNodeIdPattern = 6006;

        /// <remarks />
        public const uint Boiler2Type_Lock_Locked = 6468;

        /// <remarks />
        public const uint Boiler2Type_Lock_LockingClient = 6163;

        /// <remarks />
        public const uint Boiler2Type_Lock_LockingUser = 6164;

        /// <remarks />
        public const uint Boiler2Type_Lock_RemainingLockTime = 6165;

        /// <remarks />
        public const uint Boiler2Type_Lock_InitLock_InputArguments = 6167;

        /// <remarks />
        public const uint Boiler2Type_Lock_InitLock_OutputArguments = 6168;

        /// <remarks />
        public const uint Boiler2Type_Lock_RenewLock_OutputArguments = 6170;

        /// <remarks />
        public const uint Boiler2Type_Lock_ExitLock_OutputArguments = 6172;

        /// <remarks />
        public const uint Boiler2Type_Lock_BreakLock_OutputArguments = 6174;

        /// <remarks />
        public const uint Boiler2Type_Manufacturer = 6011;

        /// <remarks />
        public const uint Boiler2Type_ManufacturerUri = 6012;

        /// <remarks />
        public const uint Boiler2Type_Model = 6013;

        /// <remarks />
        public const uint Boiler2Type_HardwareRevision = 6015;

        /// <remarks />
        public const uint Boiler2Type_SoftwareRevision = 6016;

        /// <remarks />
        public const uint Boiler2Type_DeviceRevision = 6017;

        /// <remarks />
        public const uint Boiler2Type_ProductCode = 6014;

        /// <remarks />
        public const uint Boiler2Type_DeviceManual = 6018;

        /// <remarks />
        public const uint Boiler2Type_DeviceClass = 6019;

        /// <remarks />
        public const uint Boiler2Type_SerialNumber = 6020;

        /// <remarks />
        public const uint Boiler2Type_ProductInstanceUri = 6021;

        /// <remarks />
        public const uint Boiler2Type_RevisionCounter = 6022;

        /// <remarks />
        public const uint Boiler2Type_AssetId = 6008;

        /// <remarks />
        public const uint Boiler2Type_ComponentName = 6009;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_Locked = 6468;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_LockingClient = 6163;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_LockingUser = 6164;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_RemainingLockTime = 6165;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_InitLock_InputArguments = 6167;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_InitLock_OutputArguments = 6168;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_RenewLock_OutputArguments = 6170;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_ExitLock_OutputArguments = 6172;

        /// <remarks />
        public const uint Boiler2Type_CPIdentifier_Lock_BreakLock_OutputArguments = 6174;

        /// <remarks />
        public const uint Boiler2Type_DeviceHealth = 6010;

        /// <remarks />
        public const uint Boiler2Type_MethodSet_Switch_InputArguments = 6023;

        /// <remarks />
        public const uint Boiler2Type_MethodSet_Switch_OutputArguments = 6024;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_TemperatureChangeSpeed = 6025;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_BaseTemperature = 6026;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_TargetTemperature = 6027;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_MaintenanceInterval = 6028;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_OverheatedThresholdTemperature = 6029;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_HeaterState = 6030;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_CurrentTemperature = 6031;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_Overheated = 6033;

        /// <remarks />
        public const uint Boiler2Type_ParameterSet_OverheatInterval = 6034;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_Locked = 6468;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_LockingClient = 6163;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_LockingUser = 6164;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_RemainingLockTime = 6165;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_InitLock_InputArguments = 6167;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_InitLock_OutputArguments = 6168;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_RenewLock_OutputArguments = 6170;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_ExitLock_OutputArguments = 6172;

        /// <remarks />
        public const uint Boilers_Boiler__2_Lock_BreakLock_OutputArguments = 6174;

        /// <remarks />
        public const uint Boilers_Boiler__2_Manufacturer = 6202;

        /// <remarks />
        public const uint Boilers_Boiler__2_ManufacturerUri = 6203;

        /// <remarks />
        public const uint Boilers_Boiler__2_Model = 6204;

        /// <remarks />
        public const uint Boilers_Boiler__2_HardwareRevision = 6201;

        /// <remarks />
        public const uint Boilers_Boiler__2_SoftwareRevision = 6209;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceRevision = 6200;

        /// <remarks />
        public const uint Boilers_Boiler__2_ProductCode = 6205;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceManual = 6199;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceClass = 6197;

        /// <remarks />
        public const uint Boilers_Boiler__2_SerialNumber = 6208;

        /// <remarks />
        public const uint Boilers_Boiler__2_ProductInstanceUri = 6206;

        /// <remarks />
        public const uint Boilers_Boiler__2_RevisionCounter = 6207;

        /// <remarks />
        public const uint Boilers_Boiler__2_AssetId = 6195;

        /// <remarks />
        public const uint Boilers_Boiler__2_ComponentName = 6196;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealth = 6198;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventId = 6242;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventType = 6243;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceNode = 6248;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceName = 6247;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Time = 6249;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ReceiveTime = 6245;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Message = 6244;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Severity = 6246;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassId = 6253;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassName = 6254;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionName = 6255;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_BranchId = 6250;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Retain = 6258;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState = 6261;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState_Id = 6328;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality = 6257;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality_SourceTimestamp = 6324;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity = 6256;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity_SourceTimestamp = 6323;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment = 6252;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment_SourceTimestamp = 6322;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ClientUserId = 6251;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment_InputArguments = 6321;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState = 6259;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState_Id = 6325;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge_InputArguments = 6326;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState = 6260;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState_Id = 6327;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_InputNode = 6262;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SuppressedOrShelved = 6263;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_NormalState = 6264;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventId = 6219;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventType = 6220;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceNode = 6225;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceName = 6224;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Time = 6226;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ReceiveTime = 6222;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Message = 6221;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Severity = 6223;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassId = 6230;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassName = 6231;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionName = 6232;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_BranchId = 6227;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Retain = 6235;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState = 6238;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState_Id = 6320;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality = 6234;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality_SourceTimestamp = 6316;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity = 6233;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity_SourceTimestamp = 6315;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment = 6229;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment_SourceTimestamp = 6314;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ClientUserId = 6228;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment_InputArguments = 6313;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState = 6236;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState_Id = 6317;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge_InputArguments = 6318;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState = 6237;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState_Id = 6319;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_InputNode = 6239;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SuppressedOrShelved = 6240;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_NormalState = 6241;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventId = 6288;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventType = 6289;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceNode = 6294;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceName = 6293;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Time = 6295;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ReceiveTime = 6291;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Message = 6290;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Severity = 6292;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassId = 6299;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassName = 6300;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionName = 6301;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_BranchId = 6296;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Retain = 6304;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState = 6307;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState_Id = 6344;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality = 6303;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality_SourceTimestamp = 6340;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity = 6302;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity_SourceTimestamp = 6339;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment = 6298;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment_SourceTimestamp = 6338;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ClientUserId = 6297;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment_InputArguments = 6337;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState = 6305;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState_Id = 6341;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge_InputArguments = 6342;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState = 6306;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState_Id = 6343;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_InputNode = 6308;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SuppressedOrShelved = 6309;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_NormalState = 6310;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventId = 6265;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventType = 6266;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceNode = 6271;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceName = 6270;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Time = 6272;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ReceiveTime = 6268;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Message = 6267;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Severity = 6269;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassId = 6276;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassName = 6277;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionName = 6278;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_BranchId = 6273;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Retain = 6281;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState = 6284;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState_Id = 6336;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality = 6280;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality_SourceTimestamp = 6332;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity = 6279;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity_SourceTimestamp = 6331;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment = 6275;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment_SourceTimestamp = 6330;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ClientUserId = 6274;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment_InputArguments = 6329;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState = 6282;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState_Id = 6333;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge_InputArguments = 6334;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState = 6283;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState_Id = 6335;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_InputNode = 6285;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SuppressedOrShelved = 6286;

        /// <remarks />
        public const uint Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_NormalState = 6287;

        /// <remarks />
        public const uint Boilers_Boiler__2_MethodSet_Switch_InputArguments = 6311;

        /// <remarks />
        public const uint Boilers_Boiler__2_MethodSet_Switch_OutputArguments = 6312;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed = 6218;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_BaseTemperature = 6210;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_TargetTemperature = 6217;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_MaintenanceInterval = 6213;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature = 6215;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_HeaterState = 6212;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_CurrentTemperature = 6211;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_Overheated = 6214;

        /// <remarks />
        public const uint Boilers_Boiler__2_ParameterSet_OverheatInterval = 6350;
    }
    #endregion

    #region Method Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class MethodIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_InitLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_Lock_InitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_RenewLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_Lock_RenewLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_ExitLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_Lock_ExitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_BreakLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_Lock_BreakLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_InitLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_CPIdentifier_Lock_InitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_RenewLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_CPIdentifier_Lock_RenewLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_ExitLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_CPIdentifier_Lock_ExitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_BreakLock = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_CPIdentifier_Lock_BreakLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_MethodSet_Switch = new ExpandedNodeId(BoilerModel2.Methods.Boiler2Type_MethodSet_Switch, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_InitLock = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_Lock_InitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_RenewLock = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_Lock_RenewLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_ExitLock = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_Lock_ExitLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_BreakLock = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_Lock_BreakLock, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Disable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Disable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Enable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Enable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Disable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Disable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Enable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Enable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Disable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Disable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Enable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Enable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Disable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Disable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Enable = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Enable, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_MethodSet_Switch = new ExpandedNodeId(BoilerModel2.Methods.Boilers_Boiler__2_MethodSet_Switch, BoilerModel2.Namespaces.Boiler2);
    }
    #endregion

    #region Object Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler = new ExpandedNodeId(BoilerModel2.Objects.http___microsoft_com_Opc_OpcPlc_boiler, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_ParameterSet, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_MethodSet = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_MethodSet, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_NetworkAddress = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_CPIdentifier_NetworkAddress, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealthAlarms = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_DeviceHealthAlarms, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealthAlarms_FailureAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_DeviceHealthAlarms_FailureAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealthAlarms_CheckFunctionAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_DeviceHealthAlarms_CheckFunctionAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealthAlarms_OffSpecAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_DeviceHealthAlarms_OffSpecAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealthAlarms_MaintenanceRequiredAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boiler2Type_DeviceHealthAlarms_MaintenanceRequiredAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers = new ExpandedNodeId(BoilerModel2.Objects.Boilers, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_ParameterSet, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_MethodSet = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_MethodSet, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_DeviceHealthAlarms, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm = new ExpandedNodeId(BoilerModel2.Objects.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm, BoilerModel2.Namespaces.Boiler2);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type = new ExpandedNodeId(BoilerModel2.ObjectTypes.Boiler2Type, BoilerModel2.Namespaces.Boiler2);
    }
    #endregion

    #region Variable Node Identifiers
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_NamespaceUri = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_NamespaceUri, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_NamespaceVersion = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_NamespaceVersion, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_NamespacePublicationDate = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_NamespacePublicationDate, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_IsNamespaceSubset = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_IsNamespaceSubset, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_StaticNodeIdTypes = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_StaticNodeIdTypes, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_StaticNumericNodeIdRange = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_StaticNumericNodeIdRange, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId http___microsoft_com_Opc_OpcPlc_boiler_StaticStringNodeIdPattern = new ExpandedNodeId(BoilerModel2.Variables.http___microsoft_com_Opc_OpcPlc_boiler_StaticStringNodeIdPattern, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_Locked = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_Locked, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_LockingClient = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_LockingClient, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_LockingUser = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_LockingUser, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_RemainingLockTime = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_RemainingLockTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_InitLock_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_InitLock_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_InitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_InitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_RenewLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_RenewLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_ExitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_ExitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Lock_BreakLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Lock_BreakLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Manufacturer = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Manufacturer, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ManufacturerUri = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ManufacturerUri, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_Model = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_Model, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_HardwareRevision = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_HardwareRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_SoftwareRevision = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_SoftwareRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceRevision = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_DeviceRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ProductCode = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ProductCode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceManual = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_DeviceManual, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceClass = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_DeviceClass, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_SerialNumber = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_SerialNumber, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ProductInstanceUri = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ProductInstanceUri, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_RevisionCounter = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_RevisionCounter, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_AssetId = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_AssetId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ComponentName = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ComponentName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_Locked = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_Locked, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_LockingClient = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_LockingClient, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_LockingUser = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_LockingUser, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_RemainingLockTime = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_RemainingLockTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_InitLock_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_InitLock_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_InitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_InitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_RenewLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_RenewLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_ExitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_ExitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_CPIdentifier_Lock_BreakLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_CPIdentifier_Lock_BreakLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_DeviceHealth = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_DeviceHealth, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_MethodSet_Switch_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_MethodSet_Switch_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_MethodSet_Switch_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_MethodSet_Switch_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_TemperatureChangeSpeed = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_TemperatureChangeSpeed, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_BaseTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_BaseTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_TargetTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_TargetTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_MaintenanceInterval = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_MaintenanceInterval, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_OverheatedThresholdTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_OverheatedThresholdTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_HeaterState = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_HeaterState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_CurrentTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_CurrentTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_Overheated = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_Overheated, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boiler2Type_ParameterSet_OverheatInterval = new ExpandedNodeId(BoilerModel2.Variables.Boiler2Type_ParameterSet_OverheatInterval, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_Locked = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_Locked, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_LockingClient = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_LockingClient, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_LockingUser = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_LockingUser, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_RemainingLockTime = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_RemainingLockTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_InitLock_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_InitLock_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_InitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_InitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_RenewLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_RenewLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_ExitLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_ExitLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Lock_BreakLock_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Lock_BreakLock_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Manufacturer = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Manufacturer, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ManufacturerUri = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ManufacturerUri, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_Model = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_Model, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_HardwareRevision = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_HardwareRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_SoftwareRevision = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_SoftwareRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceRevision = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceRevision, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ProductCode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ProductCode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceManual = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceManual, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceClass = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceClass, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_SerialNumber = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_SerialNumber, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ProductInstanceUri = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ProductInstanceUri, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_RevisionCounter = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_RevisionCounter, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_AssetId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_AssetId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ComponentName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ComponentName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealth = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealth, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventType = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EventType, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SourceName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Time = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Time, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ReceiveTime = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ReceiveTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Message = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Message, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Severity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Severity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionClassName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ConditionName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_BranchId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_BranchId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Retain = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Retain, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_EnabledState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Quality_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_LastSeverity_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Comment_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ClientUserId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ClientUserId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AddComment_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_AckedState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_Acknowledge_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_ActiveState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_InputNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_InputNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SuppressedOrShelved = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_SuppressedOrShelved, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_NormalState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_FailureAlarm_NormalState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventType = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EventType, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SourceName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Time = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Time, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ReceiveTime = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ReceiveTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Message = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Message, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Severity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Severity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionClassName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ConditionName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_BranchId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_BranchId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Retain = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Retain, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_EnabledState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Quality_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_LastSeverity_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Comment_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ClientUserId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ClientUserId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AddComment_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_AckedState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_Acknowledge_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_ActiveState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_InputNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_InputNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SuppressedOrShelved = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_SuppressedOrShelved, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_NormalState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_CheckFunctionAlarm_NormalState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventType = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EventType, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SourceName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Time = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Time, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ReceiveTime = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ReceiveTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Message = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Message, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Severity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Severity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionClassName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ConditionName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_BranchId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_BranchId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Retain = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Retain, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_EnabledState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Quality_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_LastSeverity_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Comment_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ClientUserId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ClientUserId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AddComment_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_AckedState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_Acknowledge_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_ActiveState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_InputNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_InputNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SuppressedOrShelved = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_SuppressedOrShelved, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_NormalState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_OffSpecAlarm_NormalState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventType = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EventType, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SourceName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Time = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Time, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ReceiveTime = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ReceiveTime, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Message = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Message, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Severity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Severity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionClassName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionName = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ConditionName, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_BranchId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_BranchId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Retain = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Retain, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_EnabledState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Quality_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_LastSeverity_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment_SourceTimestamp = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Comment_SourceTimestamp, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ClientUserId = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ClientUserId, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AddComment_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_AckedState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_Acknowledge_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState_Id = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_ActiveState_Id, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_InputNode = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_InputNode, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SuppressedOrShelved = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_SuppressedOrShelved, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_NormalState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_DeviceHealthAlarms_MaintenanceRequiredAlarm_NormalState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_MethodSet_Switch_InputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_MethodSet_Switch_InputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_MethodSet_Switch_OutputArguments = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_MethodSet_Switch_OutputArguments, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_BaseTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_BaseTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_TargetTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_TargetTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_MaintenanceInterval = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_MaintenanceInterval, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_HeaterState = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_HeaterState, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_CurrentTemperature = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_CurrentTemperature, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_Overheated = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_Overheated, BoilerModel2.Namespaces.Boiler2);

        /// <remarks />
        public static readonly ExpandedNodeId Boilers_Boiler__2_ParameterSet_OverheatInterval = new ExpandedNodeId(BoilerModel2.Variables.Boilers_Boiler__2_ParameterSet_OverheatInterval, BoilerModel2.Namespaces.Boiler2);
    }
    #endregion

    #region BrowseName Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <remarks />
        public const string Boiler__2 = "Boiler #2";

        /// <remarks />
        public const string Boiler2Type = "Boiler2Type";

        /// <remarks />
        public const string http___microsoft_com_Opc_OpcPlc_boiler = "http://microsoft.com/Opc/OpcPlc/Boiler";
    }
    #endregion

    #region Namespace Declarations
    /// <remarks />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the Boiler2 namespace (.NET code namespace is 'BoilerModel2').
        /// </summary>
        public const string Boiler2 = "http://microsoft.com/Opc/OpcPlc/Boiler";

        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaDI namespace (.NET code namespace is 'Opc.Ua.DI').
        /// </summary>
        public const string OpcUaDI = "http://opcfoundation.org/UA/DI/";
    }
    #endregion
}