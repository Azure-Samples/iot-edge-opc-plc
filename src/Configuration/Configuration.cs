namespace OpcPlc.Configuration;

using OpenTelemetry.Exporter;
using System;

public class OpcPlcConfiguration
{
    /// <summary>
    /// Name of the application.
    /// </summary>
    public readonly string ProgramName = "OpcPlc";

    public bool DisableAnonymousAuth { get; set; }

    public bool DisableUsernamePasswordAuth { get; set; }

    public bool DisableCertAuth { get; set; }

    /// <summary>
    /// Admin user.
    /// </summary>
    public string AdminUser { get; set; } = "sysadmin";

    /// <summary>
    /// Admin user password.
    /// </summary>
    public string AdminPassword { get; set; } = "demo";

    /// <summary>
    /// Default user.
    /// </summary>
    public string DefaultUser { get; set; } = "user1";

    /// <summary>
    /// Default user password.
    /// </summary>
    public string DefaultPassword { get; set; } = "password";

    /// <summary>
    /// Open Telemetry enabled.
    /// </summary>
    public bool OtlpEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets OTLP reporting endpoint URI.
    /// </summary>
    public string OtlpEndpointUri { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets a the export interval in seconds.
    /// </summary>
    public TimeSpan OtlpExportInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Show OPC Publisher configuration file using IP address as EndpointUrl.
    /// </summary>
    public bool ShowPublisherConfigJsonIp { get; set; }

    /// <summary>
    /// Show OPC Publisher configuration file using plchostname as EndpointUrl.
    /// </summary>
    public bool ShowPublisherConfigJsonPh { get; set; }

    /// <summary>
    /// Web server port for hosting OPC Publisher file.
    /// </summary>
    public uint WebServerPort { get; set; } = 8080;

    /// <summary>
    /// Show usage help.
    /// </summary>
    public bool ShowHelp { get; set; }

    public string PnJson { get; set; } = "pn.json";

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public string LogFileName { get; set; } = $"hostname-port-plc.log"; // Set in InitLogging().

    public string LogLevelCli { get; set; } = "info";

    public TimeSpan LogFileFlushTimeSpanSec { get; set; } = TimeSpan.FromSeconds(30);

    public OpcApplicationConfiguration OpcUa { get; set; } = new OpcApplicationConfiguration();
}
