namespace OpcPlc;

/// <summary>
/// Defines constants for namespaces used by the application.
/// </summary>
public static partial class Namespaces
{
    /// <summary>
    /// The namespace for the nodes provided by for boiler type.
    /// </summary>
    public const string OpcPlcBoiler = "http://microsoft.com/Opc/OpcPlc/Boiler";

    /// <summary>
    /// The namespace for the nodes provided by the for the boiler instance.
    /// </summary>
    public const string OpcPlcBoilerInstance = "http://microsoft.com/Opc/OpcPlc/BoilerInstance";

    /// <summary>
    /// The namespace for the nodes provided by the plc server.
    /// </summary>
    public const string OpcPlcApplications = "http://microsoft.com/Opc/OpcPlc/";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for simple events
    /// </summary>
    public const string OpcPlcSimpleEvents = "http://microsoft.com/Opc/OpcPlc/SimpleEvents";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for simple events instance
    /// </summary>
    public const string OpcPlcSimpleEventsInstance = "http://microsoft.com/Opc/OpcPlc/SimpleEventsInstance";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for alarm.
    /// </summary>
    public const string OpcPlcAlarms = "http://microsoft.com/Opc/OpcPlc/Alarms";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for alarm instance.
    /// </summary>
    public const string OpcPlcAlarmsInstance = "http://microsoft.com/Opc/OpcPlc/AlarmsInstance";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for simulation and test purposes.
    /// </summary>
    public const string OpcPlcReferenceTest = "http://microsoft.com/Opc/OpcPlc/ReferenceTest";

    /// <summary>
    /// The namespace for the nodes provided by the plc server for alarm instance.
    /// </summary>
    public const string OpcPlcDeterministicAlarmsInstance = "http://microsoft.com/Opc/OpcPlc/DetermAlarmsInstance";

    /// <summary>
    /// The namespace for DI nodes.
    /// </summary>
    public const string DI = "http://opcfoundation.org/UA/DI/";

    /// <summary>
    /// The namespace for IA (Industrial Automation) nodes, including Stacklight.
    /// </summary>
    public const string IA = "http://opcfoundation.org/UA/IA/";

    /// <summary>
    /// The namespace for the OPC UA Machinery companion spec nodes (required by Pumps).
    /// </summary>
    public const string Machinery = "http://opcfoundation.org/UA/Machinery/";

    /// <summary>
    /// The namespace for the OPC UA Pumps companion spec nodes.
    /// </summary>
    public const string Pumps = "http://opcfoundation.org/UA/Pumps/";

    /// <summary>
    /// The namespace for the OPC UA WoT-Con (Web of Things Connectivity) companion spec nodes.
    /// </summary>
    public const string WotCon = "http://opcfoundation.org/UA/WoT-Con/";
}
