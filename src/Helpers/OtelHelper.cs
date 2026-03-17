namespace OpcPlc.Helpers;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using System;
using System.Collections.Generic;

public static class OtelHelper
{
    public static IDisposable ConfigureOpenTelemetry(
        string serviceName,
        string exportEndpointUri,
        string exportProtocol,
        TimeSpan exportInterval,
        string activitySourceName)
    {
        TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddAspNetCoreInstrumentation()
            .AddSource(activitySourceName)
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

        MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(MetricsHelper.ServiceName).AddTelemetrySdk())
            .AddMeter(MetricsHelper.Meter.Name)
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

        return new TelemetryProvidersLifetime(tracerProvider, meterProvider);
    }

    private sealed class TelemetryProvidersLifetime : IDisposable
    {
        private readonly List<IDisposable> _providers;

        public TelemetryProvidersLifetime(params IDisposable[] providers)
        {
            _providers = [.. providers];
        }

        public void Dispose()
        {
            foreach (IDisposable provider in _providers)
            {
                provider.Dispose();
            }
        }
    }
}
