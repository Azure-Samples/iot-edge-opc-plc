namespace OpcPlc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpcPlc.PluginNodes;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public class Startup
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.Run(async context => {
            if (context.Request.Method == "GET" && await HandleStacklightRequestAsync(context).ConfigureAwait(false))
            {
                return;
            }
            else if (context.Request.Method == "GET" &&
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

    private static async Task<bool> HandleStacklightRequestAsync(HttpContext context)
    {
        if (context.Request.Path == "/stacklight")
        {
            var stacklightPlugin = Program.OpcPlcServer.PluginNodes?
                .OfType<StacklightPluginNodes>()
                .FirstOrDefault();

            var state = stacklightPlugin?.GetState();
            if (state is not null)
            {
                context.Response.ContentType = "application/json";
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                await JsonSerializer.SerializeAsync(context.Response.Body, state, _jsonOptions).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Stacklight not enabled").ConfigureAwait(false);
            }

            return true;
        }

        if (context.Request.Path == "/stacklight.html")
        {
            var htmlPath = "wwwroot/stacklight.html";
            if (File.Exists(htmlPath))
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(await File.ReadAllTextAsync(htmlPath).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
            }

            return true;
        }

        if (context.Request.Path == "/stacklight.svg")
        {
            var svgPath = "wwwroot/stacklight.svg";
            if (File.Exists(svgPath))
            {
                context.Response.ContentType = "image/svg+xml";
                await context.Response.WriteAsync(await File.ReadAllTextAsync(svgPath).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
            }

            return true;
        }

        return false;
    }
}
