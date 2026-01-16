namespace OpcPlc;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using OpcPlc.Configuration;
using OpcPlc.Extensions;
using OpcPlc.Helpers;
using OpcPlc.Logging;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public class OpcPlcServer
{
    private const int DefaultMinThreads = 20;
    private const int DefaultCompletionPortThreads = 20;

    private string[] _args;
    private CancellationTokenSource _cancellationTokenSource;
    private ImmutableList<IPluginNodes> _pluginNodes;

    public OpcPlcConfiguration Config { get; set; }

    /// <summary>
    /// The LoggerFactory used to create logging objects.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// Logging object.
    /// </summary>
    public ILogger Logger { get; set; }

    /// <summary>
    /// OPC UA server object.
    /// </summary>
    public PlcServer PlcServer { get; set; }

    /// <summary>
    /// Simulation object.
    /// </summary>
    public PlcSimulation PlcSimulationInstance { get; set; }

    /// <summary>
    /// Service returning <see cref="DateTime"/> values and <see cref="Timer"/> instances. Mocked in tests.
    /// </summary>
    public TimeService TimeService { get; set; } = new();

    /// <summary>
    /// A flag indicating when the server is up and ready to accept connections.
    /// </summary>
    public bool Ready { get; set; }

    /// <summary>
    /// Start the PLC server and simulation.
    /// </summary>
    public async Task StartAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // Initialize configuration.
        _args = args;
        Config = new OpcPlcConfiguration();

        InitLogging();
        LoadPluginNodes();
        (PlcSimulationInstance, var extraArgs) = CliOptions.InitConfiguration(args, Config, _pluginNodes);

        // Show usage if requested
        if (Config.ShowHelp)
        {
            Logger.LogInformation(CliOptions.GetUsageHelp(Config.ProgramName));
            return;
        }

        // Validate and parse extra arguments.
        if (extraArgs.Count > 0)
        {
            Logger.LogWarning("Found one or more invalid command line arguments: {InvalidArgs}", string.Join(" ", extraArgs));
            Logger.LogInformation(CliOptions.GetUsageHelp(Config.ProgramName));
        }

        LogLogo();

        ThreadPool.SetMinThreads(DefaultMinThreads, DefaultCompletionPortThreads);
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
        Logger.LogInformation(
            "Min worker threads: {MinWorkerThreads}, min completion port threads: {MinCompletionPortThreads}",
            minWorkerThreads,
            minCompletionPortThreads);

        Logger.LogInformation("Current directory: {CurrentDirectory}", Directory.GetCurrentDirectory());
        Logger.LogInformation("Log file: {LogFileName}", Path.GetFullPath(Config.LogFileName));
        Logger.LogInformation("Log level: {LogLevel}", Config.LogLevelCli);

        // Show OPC PLC version.
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        Logger.LogInformation("{ProgramName} v{Version} from {Date} starting up ...",
            Config.ProgramName,
            $"{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart}",
            File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location));
        Logger.LogDebug("{ProgramName} informational version: v{Version}",
            Config.ProgramName,
            (Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion);

        // Show OPC UA SDK version.
        Logger.LogInformation(
            "OPC UA SDK {Version} from {Date}",
            Utils.GetAssemblyBuildNumber(),
            Utils.GetAssemblyTimestamp());
        Logger.LogDebug(
            "OPC UA SDK informational version: {Version}",
            Utils.GetAssemblySoftwareVersion());

        if (Config.OtlpEndpointUri is not null)
        {
            OtelHelper.ConfigureOpenTelemetry(Config.ProgramName, Config.OtlpEndpointUri, Config.OtlpExportProtocol, Config.OtlpExportInterval);
        }

        using var host = CreateHostBuilder(args);
        if (Config.ShowPublisherConfigJsonIp || Config.ShowPublisherConfigJsonPh)
        {
            StartWebServer(host);
        }

        try
        {
            await StartPlcServerAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "OPC UA server failed unexpectedly");
            throw;
        }

        Logger.LogInformation("OPC UA server exiting ...");
    }

    /// <summary>
    /// Restart the PLC server and simulation.
    /// </summary>
    public async Task RestartAsync()
    {
        Logger.LogInformation("Stopping PLC server and simulation ...");
        PlcServer.Stop();
        PlcSimulationInstance.Stop();

        Logger.LogInformation("Restarting PLC server and simulation ...");
        LogLogo();

        await StartPlcServerAndSimulationAsync().ConfigureAwait(false);
    }



    /// <summary>
    /// Stop the application.
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Load plugin nodes to extend the address space using reflection.
    /// </summary>
    private void LoadPluginNodes()
    {
        var pluginNodesType = typeof(IPluginNodes);

        _pluginNodes = pluginNodesType.Assembly.ExportedTypes
            .Where(t => pluginNodesType.IsAssignableFrom(t) &&
                        !t.IsInterface &&
                        !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t, TimeService, Logger))
            .Cast<IPluginNodes>()
            .ToImmutableList();
    }

    /// <summary>
    /// Start web server to host pn.json.
    /// </summary>
    private void StartWebServer(IHost host)
    {
        try
        {
            host.Start();

            if (Config.ShowPublisherConfigJsonIp)
            {
                Logger.LogInformation("Web server started: {PnJsonUri}", $"http://{GetIpAddress()}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else if (Config.ShowPublisherConfigJsonPh)
            {
                Logger.LogInformation("Web server started: {PnJsonUri}", $"http://{Config.OpcUa.Hostname}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else
            {
                Logger.LogInformation("Web server started on port {WebServerPort}", Config.WebServerPort);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Could not start web server on port {WebServerPort}: {Message}",
                Config.WebServerPort,
                e.Message);
        }
    }

    /// <summary>
    /// Get IP address of first interface, otherwise host name.
    /// </summary>
    private string GetIpAddress()
    {
        string ip = Dns.GetHostName();

        try
        {
            // Ignore System.Net.Internals.SocketExceptionFactory+ExtendedSocketException
            var hostEntry = Dns.GetHostEntry(ip);
            if (hostEntry.AddressList.Length > 0)
            {
                ip = hostEntry.AddressList[0].ToString();
            }
        }
        catch
        {
            // Default to Dns.GetHostName.
        }

        return ip;
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    private async Task StartPlcServerAsync(CancellationToken cancellationToken)
    {
        await StartPlcServerAndSimulationAsync().ConfigureAwait(false);

        if (Config.ShowPublisherConfigJsonIp)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                Config.PnJson,
                $"{GetIpAddress()}:{Config.OpcUa.ServerPort}{Config.OpcUa.ServerPath}",
                !Config.OpcUa.EnableUnsecureTransport,
                _pluginNodes,
                Logger).ConfigureAwait(false);
        }
        else if (Config.ShowPublisherConfigJsonPh)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                Config.PnJson,
                $"{Config.OpcUa.Hostname}:{Config.OpcUa.ServerPort}{Config.OpcUa.ServerPath}",
                !Config.OpcUa.EnableUnsecureTransport,
                _pluginNodes,
                Logger).ConfigureAwait(false);
        }

        Ready = true;
        Logger.LogInformation("PLC simulation started, press Ctrl+C to exit ...");

        // Wait for cancellation.
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, eArgs) => {
            _cancellationTokenSource.Cancel();
            eArgs.Cancel = true;
        };

        await _cancellationTokenSource.Token.WhenCanceled().ConfigureAwait(false);

        PlcSimulationInstance.Stop();
        PlcServer.Stop();
        _cancellationTokenSource.Dispose();
    }

    private async Task StartPlcServerAndSimulationAsync()
    {
        // init OPC configuration and tracing
        var opcUaAppConfigFactory = new OpcUaAppConfigFactory(Config, Logger, LoggerFactory);
        ApplicationConfiguration plcApplicationConfiguration = await opcUaAppConfigFactory.ConfigureAsync().ConfigureAwait(false);

        // start the server.
        Logger.LogInformation("Starting server on endpoint {Endpoint} ...", plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]);
        Logger.LogInformation("Simulation settings are:");
        Logger.LogInformation("One simulation phase consists of {SimulationCycleCount} cycles", PlcSimulationInstance.SimulationCycleCount);
        Logger.LogInformation("One cycle takes {SimulationCycleLength:N0} ms", PlcSimulationInstance.SimulationCycleLength);
        Logger.LogInformation("Reference test simulation: {AddReferenceTestSimulation}",
            PlcSimulationInstance.AddReferenceTestSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Simple events: {AddSimpleEventsSimulation}",
            PlcSimulationInstance.AddSimpleEventsSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Alarms: {AddAlarmSimulation}", PlcSimulationInstance.AddAlarmSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Deterministic alarms: {DeterministicAlarmSimulation}",
            PlcSimulationInstance.DeterministicAlarmSimulationFile != null ? "Enabled" : "Disabled");

        Logger.LogInformation("Anonymous authentication: {AnonymousAuth}", Config.DisableAnonymousAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Reject chain validation with CA certs with unknown revocation status: {RejectValidationUnknownRevocStatus}", Config.OpcUa.DontRejectUnknownRevocationStatus ? "Disabled" : "Enabled");
        Logger.LogInformation("Username/Password authentication: {UsernamePasswordAuth}", Config.DisableUsernamePasswordAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Certificate authentication: {CertAuth}", Config.DisableCertAuth ? "Disabled" : "Enabled");

        // Add simple events, alarms, reference test simulation and deterministic alarms.
        PlcServer = new PlcServer(Config, PlcSimulationInstance, TimeService, _pluginNodes, Logger);
        PlcServer.Start(plcApplicationConfiguration);
        Logger.LogInformation("OPC UA Server started");

        // Add remaining base simulations.
        PlcSimulationInstance.Start(PlcServer);
    }

    /// <summary>
    /// Initialize logging.
    /// </summary>
    private void InitLogging()
    {
        LogLevel logLevel;

        // set the log level
        switch (Config.LogLevelCli)
        {
            case "critical":
                logLevel = LogLevel.Critical;
                break;
            case "error":
                logLevel = LogLevel.Error;
                break;
            case "warn":
                logLevel = LogLevel.Warning;
                break;
            case "info":
                logLevel = LogLevel.Information;
                break;
            case "debug":
                logLevel = LogLevel.Debug;
                break;
            case "trace":
                logLevel = LogLevel.Trace;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Config.LogLevelCli), $"Unknown log level: {Config.LogLevelCli}");
        }

        LoggerFactory = LoggingProvider.CreateDefaultLoggerFactory(logLevel);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
        {
            Config.LogFileName = Environment.GetEnvironmentVariable("_GW_LOGP");
        }
        else
        {
            Config.LogFileName = $"{Dns.GetHostName().Split('.')[0].ToLowerInvariant()}-{GetPort()}-plc.log";
        }

        if (!string.IsNullOrEmpty(Config.LogFileName))
        {
            // configure rolling file sink
            const int MAX_LOGFILE_SIZE = 1024 * 1024;
            const int MAX_RETAINED_LOGFILES = 2;
            LoggerFactory.AddFile(Config.LogFileName, logLevel, levelOverrides: null, isJson: false, fileSizeLimitBytes: MAX_LOGFILE_SIZE, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
        }

        Logger = LoggerFactory.CreateLogger("OpcPlc");
    }

    /// <summary>
    /// Configure web server.
    /// </summary>
    public IHost CreateHostBuilder(string[] args)
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var snapLocation = Environment.GetEnvironmentVariable("SNAP");
        if (!string.IsNullOrWhiteSpace(snapLocation))
        {
            // The application is running as a snap
            contentRoot = snapLocation;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseContentRoot(contentRoot); // Avoid System.InvalidOperationException.
                webBuilder.UseUrls($"http://*:{Config.WebServerPort}");
                webBuilder.UseStartup<Startup>();
            }).Build();

        return host;
    }

    private string GetPort()
    {
        string port = Config.OpcUa.ServerPort.ToString();

        foreach (var arg in _args)
        {
            if (arg.StartsWith("--pn="))
            {
                return arg.Substring("--pn=".Length).Trim();
            }

            if (arg.StartsWith("--portnum="))
            {
                return arg.Substring("--portnum=".Length).Trim();
            }
        }

        return port;
    }

    private void LogLogo()
    {
        Logger.LogInformation(
            @"
 ██████╗ ██████╗  ██████╗    ██████╗ ██╗      ██████╗
██╔═══██╗██╔══██╗██╔════╝    ██╔══██╗██║     ██╔════╝
██║   ██║██████╔╝██║         ██████╔╝██║     ██║
██║   ██║██╔═══╝ ██║         ██╔═══╝ ██║     ██║
╚██████╔╝██║     ╚██████╗    ██║     ███████╗╚██████╗
 ╚═════╝ ╚═╝      ╚═════╝    ╚═╝     ╚══════╝ ╚═════╝
");
    }
}
