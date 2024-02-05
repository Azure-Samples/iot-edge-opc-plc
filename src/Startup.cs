namespace OpcPlc;

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Opc.Ua;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        if (Program.OpcPlcServer.Config.OtlpEnabled)
        {
            ConfigureOpenTelemetryTracing();
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Serve pn.json
        app.Run(async context =>
        {
            if (context.Request.Method == "GET" &&
                context.Request.Path == (Program.OpcPlcServer.Config.PnJson[0] != '/' ? "/" : string.Empty) + Program.OpcPlcServer.Config.PnJson &&
                File.Exists(Program.OpcPlcServer.Config.PnJson))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(await File.ReadAllTextAsync(Program.OpcPlcServer.Config.PnJson).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        });
    }

    // This method configures OpenTelemetry tracing.
    public void ConfigureOpenTelemetryTracing()
    {
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddAspNetCoreInstrumentation()
            .AddSource(EndpointBase.ActivitySourceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(Program.OpcPlcServer.Config.ProgramName))
            .AddOtlpExporter(opt => {
                opt.Endpoint = new Uri(Program.OpcPlcServer.Config.OtlpEndpointUri);
                opt.Protocol = OtlpExportProtocol.Grpc;
                opt.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = Program.OpcPlcServer.Config.OtlpExportInterval.Milliseconds;
            })
            .Build();
    }
}
