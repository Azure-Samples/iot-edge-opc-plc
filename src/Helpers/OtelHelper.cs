namespace OpcPlc.Helpers;

using Opc.Ua;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Extensions.Hosting;

using System;

public static class OtelHelper
{
    public static void ConfigureOpenTelemetry(string serviceName, string exportEndpointUri, string exportProtocol, TimeSpan exportInterval)
    {
        _ = Sdk.CreateTracerProviderBuilder()
            .AddAspNetCoreInstrumentation()
            .AddSource(EndpointBase.ActivitySourceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName))
            .AddOtlpExporter(exporterOptions => {
                exporterOptions.Endpoint = new Uri(exportEndpointUri);
                exporterOptions.Protocol = exportProtocol == "protobuf"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
                exporterOptions.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = (int)exportInterval.TotalMilliseconds;
            })
            .Build();

        _ = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(MetricsConfig.ServiceName).AddTelemetrySdk())
            .AddMeter(MetricsConfig.Meter.Name)
            .AddRuntimeInstrumentation()
            .AddOtlpExporter((exporterOptions, metricsReaderOptions) => {
                exporterOptions.Endpoint = new Uri(exportEndpointUri);
                exporterOptions.Protocol = exportProtocol == "protobuf"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;

                metricsReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;
                metricsReaderOptions.PeriodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions {
                    ExportIntervalMilliseconds = (int?)exportInterval.TotalMilliseconds
                };
            })
            .Build();
    }
}
