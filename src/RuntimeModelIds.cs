namespace OpcPlc;

using Opc.Ua;

internal static class RuntimeModelIds
{
    internal static class SimpleEvents
    {
        public static readonly ExpandedNodeId CycleStepDataType = new(1, Namespaces.OpcPlcSimpleEvents);
        public static readonly ExpandedNodeId SystemCycleStartedEventType = new(14, Namespaces.OpcPlcSimpleEvents);
    }

    internal static class Boiler1
    {
        internal static class DataTypeIds
        {
            public static readonly ExpandedNodeId BoilerDataType = new(15032, Namespaces.OpcPlcBoiler);
            public static readonly ExpandedNodeId BoilerTemperatureType = new(15001, Namespaces.OpcPlcBoiler);
        }

        internal static class VariableIds
        {
            public static readonly ExpandedNodeId Boiler1_BoilerStatus = new(15013, Namespaces.OpcPlcBoiler);
        }
    }

    internal static class Boiler2
    {
        internal static class Objects
        {
            public const uint Boilers = 5;
        }

        internal static class Methods
        {
            public const uint Boilers_Boiler__2_MethodSet_Switch = 7019;
        }

        internal static class Variables
        {
            public const uint Boilers_Boiler__2_AssetId = 6195;
            public const uint Boilers_Boiler__2_DeviceHealth = 6198;
            public const uint Boilers_Boiler__2_DeviceManual = 6199;
            public const uint Boilers_Boiler__2_ParameterSet_BaseTemperature = 6210;
            public const uint Boilers_Boiler__2_ParameterSet_CurrentTemperature = 6211;
            public const uint Boilers_Boiler__2_ParameterSet_HeaterState = 6212;
            public const uint Boilers_Boiler__2_ParameterSet_MaintenanceInterval = 6213;
            public const uint Boilers_Boiler__2_ParameterSet_Overheated = 6214;
            public const uint Boilers_Boiler__2_ParameterSet_OverheatedThresholdTemperature = 6215;
            public const uint Boilers_Boiler__2_ParameterSet_TargetTemperature = 6217;
            public const uint Boilers_Boiler__2_ParameterSet_TemperatureChangeSpeed = 6218;
            public const uint Boilers_Boiler__2_ParameterSet_OverheatInterval = 6350;
        }
    }

    internal static class Di
    {
        internal static class Objects
        {
            public const uint DeviceSet = 5001;
        }

        internal static class ObjectTypes
        {
            public const uint FunctionalGroupType = 1005;
            public const uint DeviceHealthDiagnosticAlarmType = 15143;
        }

        internal static class BrowseNames
        {
            public const string DeviceHealth = "DeviceHealth";
            public const string Identification = "Identification";
            public const string Manufacturer = "Manufacturer";
            public const string Model = "Model";
            public const string SerialNumber = "SerialNumber";
        }

        internal enum DeviceHealth
        {
            NORMAL = 0,
            FAILURE = 1,
            CHECK_FUNCTION = 2,
            OFF_SPEC = 3,
            MAINTENANCE_REQUIRED = 4
        }
    }

    internal static class WotCon
    {
        internal static class Objects
        {
            public const uint WoTAssetConnectionManagement = 31;
            public const uint WoTAssetConnectionManagementType_Configuration = 78;
        }

        internal static class ObjectTypes
        {
            public const uint IWoTAssetType = 42;
            public const uint WoTAssetConfigurationType = 105;
            public const uint WoTAssetFileType = 110;
        }

        internal static class Methods
        {
            public const uint WoTAssetConnectionManagementType_CreateAsset = 26;
            public const uint WoTAssetConnectionManagementType_DeleteAsset = 29;
            public const uint WoTAssetConnectionManagement_CreateAsset = 32;
            public const uint WoTAssetConnectionManagement_DeleteAsset = 35;
            public const uint WoTAssetConnectionManagementType_DiscoverAssets = 41;
            public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint = 49;
            public const uint WoTAssetConnectionManagementType_ConnectionTest = 75;
            public const uint WoTAssetFileType_CloseAndUpdate = 111;
        }

        internal static class Variables
        {
            public const uint WoTAssetConnectionManagement_CreateAsset_InputArguments = 33;
            public const uint WoTAssetConnectionManagement_CreateAsset_OutputArguments = 34;
            public const uint WoTAssetConnectionManagement_DeleteAsset_InputArguments = 36;
            public const uint WoTAssetConnectionManagementType_SupportedWoTBindings = 40;
            public const uint WoTAssetConnectionManagementType_DiscoverAssets_OutputArguments = 48;
            public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint_InputArguments = 50;
            public const uint WoTAssetConnectionManagementType_ConnectionTest_InputArguments = 76;
            public const uint WoTAssetConnectionManagementType_ConnectionTest_OutputArguments = 77;
            public const uint WoTAssetFileType_CloseAndUpdate_InputArguments = 112;
            public const uint WoTAssetConnectionManagementType_CreateAssetForEndpoint_OutputArguments = 170;
        }

        internal static class ReferenceTypes
        {
            public const uint HasWoTComponent = 142;
        }

        internal static class BrowseNames
        {
            public const string WoTFile = "WoTFile";
            public const string AssetEndpoint = "AssetEndpoint";
            public const string DiscoverAssets = "DiscoverAssets";
            public const string CreateAssetForEndpoint = "CreateAssetForEndpoint";
            public const string ConnectionTest = "ConnectionTest";
            public const string SupportedWoTBindings = "SupportedWoTBindings";
            public const string Configuration = "Configuration";
            public const string License = "License";
        }
    }
}