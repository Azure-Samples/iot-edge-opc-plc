namespace OpcPlc
{
    using System.IO;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Serve pn.json
            app.Run(async context =>
            {
                if (context.Request.Method == "GET" && context.Request.Path == (Program.PnJson[0] != '/' ? "/" : "") + Program.PnJson &&
                    File.Exists(Program.PnJson))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(await File.ReadAllTextAsync(Program.PnJson).ConfigureAwait(false)).ConfigureAwait(false);
                } else
                {
                    context.Response.StatusCode = 404;
                }
            });
        }
    }
}