namespace OpcPlc;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Opc.Ua.Server.Hosting;
using OpcPlc.Configuration;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal sealed class PlcServerHost : IOpcUaServerFactory, IServerStartupTask, IAsyncDisposable
{
    private readonly OpcPlcConfiguration _config;
    private readonly PlcSimulation _simulation;
    private readonly TimeService _timeService;
    private readonly ImmutableList<IPluginNodes> _plugins;
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PlcServerHost(OpcPlcConfiguration config, PlcSimulation simulation, TimeService timeService,
        ImmutableList<IPluginNodes> plugins, ILogger logger, ILoggerFactory loggerFactory, ITelemetryContext telemetry)
    {
        _config = config;
        _simulation = simulation;
        _timeService = timeService;
        _plugins = plugins;
        _logger = logger;
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddSingleton(loggerFactory);
        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton<IHostLifetime, EmbeddedHostLifetime>();
        builder.Services.AddOpcUa().AddServer(_ => { }).AddDefaultIdentityAuthenticators(options =>
        {
            options.EnableAnonymous = false;
            options.EnableUserNamePassword = false;
            options.EnableX509 = false;
            options.EnableJwt = false;
        });
        builder.Services.AddSingleton(new OpcUaServerIdentityAuthenticatorRegistration(
            (_, _) => Server.CreateUserIdentityAuthenticators()));
        builder.Services.AddSingleton<IOpcUaApplicationConfigurationProvider>(_ =>
            new PlcApplicationConfigurationProvider(new OpcUaAppConfigFactory(config, logger, loggerFactory, telemetry)));
        builder.Services.AddSingleton<IOpcUaServerFactory>(this);
        builder.Services.AddSingleton<IServerStartupTask>(this);
        _host = builder.Build();
    }

    public PlcServer Server { get; private set; }

    public Task Completion { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _host.StartAsync(cancellationToken).ConfigureAwait(false);
        Task[] tasks = _host.Services.GetServices<IHostedService>().OfType<BackgroundService>()
            .Select(service => service.ExecuteTask).Where(task => task is not null).ToArray();
        Completion = ObserveCompletionAsync(tasks);
        Task completed = await Task.WhenAny(_started.Task, Completion).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (completed == Completion)
        {
            await Completion.ConfigureAwait(false);
            throw new InvalidOperationException("The OPC UA host stopped before the PLC server was ready.");
        }
        await _started.Task.ConfigureAwait(false);
    }

    public StandardServer CreateServer(ITelemetryContext telemetry, TimeProvider timeProvider)
    {
        return Server = new PlcServer(_config, _simulation, _timeService, _plugins, _logger, telemetry);
    }

    public ValueTask OnServerStartedAsync(IServerContext server, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _started.TrySetResult();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await ((IAsyncDisposable)_host).DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task ObserveCompletionAsync(Task[] tasks)
    {
        if (tasks.Length == 0)
        {
            throw new InvalidOperationException("The OPC UA hosted service was not registered.");
        }
        Task completed = await Task.WhenAny(tasks).ConfigureAwait(false);
        await completed.ConfigureAwait(false);
    }

    private sealed class EmbeddedHostLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}