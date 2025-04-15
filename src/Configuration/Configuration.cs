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
    /// Gets or sets OTLP reporting endpoint URI.
    /// </summary>
    public string OtlpEndpointUri { get; set; } // e.g. "http://localhost:4317"

    /// <summary>
    /// Gets or sets the OTLP export interval in seconds.
    /// </summary>
    public TimeSpan OtlpExportInterval { get; set; } = TimeSpan.FromSeconds(60);

    // <summary>
    /// Gets or sets the OTLP export protocol: grpc, protobuf.
    /// </summary>
    public string OtlpExportProtocol { get; set; } = "grpc";

    /// <summary>
    /// Gets or sets how to handle metrics for publish requests.
    /// Allowed values:
    /// disable=Always disabled,
    /// enable=Always enabled,
    /// auto=Auto-disable when sessions > 40 or monitored items > 500.
    /// </summary>
    public string OtlpPublishMetrics { get; set; } = "auto";

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

    /// <summary>
    /// Configure chaos mode
    /// </summary>
    public bool RunInChaosMode { get; set; }
}
