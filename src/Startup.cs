namespace OpcPlc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
#pragma warning disable IDE0060 // Remove unused parameter
    public void ConfigureServices(IServiceCollection services)
#pragma warning restore IDE0060 // Remove unused parameter
    {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, OpcPlcServer server)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Use(async (context, next) => {
            if (HttpMethods.IsGet(context.Request.Method) &&
                (context.Request.Path == "/ready" || context.Request.Path == "/healthz"))
            {
                bool ready = context.Request.Path == "/healthz" || server.Ready;
                context.Response.StatusCode = ready
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "text/plain";
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.WriteAsync(ready ? "Healthy" : "Starting or stopping",
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            await next(context).ConfigureAwait(false);
        });

        // Serves stacklight.html from wwwroot (content type, 404 etc. handled automatically).
        app.UseStaticFiles();

        // Serves the dynamic /stacklight JSON state endpoint.
        app.UseMiddleware<StacklightMiddleware>();

        app.Run(async context => {
            if (context.Request.Method == "GET" &&
                (server.Config.ShowPublisherConfigJsonIp || server.Config.ShowPublisherConfigJsonPh) &&
                context.Request.Path == (server.Config.PnJson[0] != '/' ? "/" : string.Empty) + server.Config.PnJson &&
                File.Exists(server.Config.PnJson))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(await File.ReadAllTextAsync(server.Config.PnJson).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        });
    }
}
