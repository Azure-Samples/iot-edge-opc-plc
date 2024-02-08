namespace OpcPlc.Helpers;

using Opc.Ua;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

public static class OtelHelper
{
    public static void ConfigureOpenTelemetryTracing(string serviceName, string exportEndpointUri, string exportProtocol, TimeSpan exportInterval)
    {
        _ = Sdk.CreateTracerProviderBuilder()
            .AddAspNetCoreInstrumentation()
            .AddSource(EndpointBase.ActivitySourceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName))
            .AddOtlpExporter(opt => {
                opt.Endpoint = new Uri(exportEndpointUri);
                opt.Protocol = exportProtocol == "protobuf"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
                opt.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = (int)exportInterval.TotalMilliseconds;
            })
            .Build();
    }
}
