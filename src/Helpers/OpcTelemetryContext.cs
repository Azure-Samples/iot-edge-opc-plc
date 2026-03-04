namespace OpcPlc.Helpers;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

public sealed class OpcTelemetryContext : ITelemetryContext, IDisposable
{
    private readonly string _name;
    private readonly string _version;

    public OpcTelemetryContext(ILoggerFactory loggerFactory, string name, string version)
    {
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _name = string.IsNullOrWhiteSpace(name) ? "OpcPlc" : name;
        _version = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version;

        ActivitySource = new ActivitySource(_name, _version);
    }

    public ILoggerFactory LoggerFactory { get; }

    public ActivitySource ActivitySource { get; }

    public Meter CreateMeter()
    {
        return new Meter(_name, _version);
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
    }

    public static string ResolveOpcPlcVersion()
    {
        var assembly = typeof(OpcTelemetryContext).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
        if (fileVersion.ProductMajorPart > 0 || fileVersion.ProductMinorPart > 0 || fileVersion.ProductBuildPart > 0)
        {
            return $"{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart}";
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
