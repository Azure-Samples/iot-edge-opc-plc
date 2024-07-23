// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Logging;

using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System;

/// <summary>
/// Provides utility for creating logger factory.
/// </summary>
public static class LoggingProvider
{
    /// <summary>
    /// Create ILoggerFactory object with default configuration.
    /// </summary>
    public static ILoggerFactory CreateDefaultLoggerFactory(LogLevel level, string serviceName, string exportEndpointUri, string exportProtocol, TimeSpan exportInterval)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options => options.FormatterName = nameof(SyslogFormatter))
            .SetMinimumLevel(level)
            .AddConsoleFormatter<
                SyslogFormatter,
                SyslogFormatterOptions>();
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                options.AddOtlpExporter(exporterOptions =>
                {
                    exporterOptions.Endpoint = new Uri(exportEndpointUri);
                    exporterOptions.Protocol = exportProtocol == "protobuf" ? OtlpExportProtocol.HttpProtobuf : OtlpExportProtocol.Grpc;
                    exporterOptions.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = (int)exportInterval.TotalMilliseconds;
                });
            });
        });

        return loggerFactory;
    }
}
