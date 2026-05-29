namespace OpcPlc;

using Microsoft.AspNetCore.Http;
using OpcPlc.PluginNodes;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Middleware that serves the Stacklight visualization endpoints
/// (/stacklight, /stacklight.html, /stacklight.svg). Requests that are not handled
/// are passed on to the next middleware in the pipeline.
/// </summary>
public class StacklightMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "GET" && await TryHandleAsync(context).ConfigureAwait(false))
        {
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static async Task<bool> TryHandleAsync(HttpContext context)
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
