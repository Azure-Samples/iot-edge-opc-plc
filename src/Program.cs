namespace OpcPlc;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Opc.Ua;
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

public static class Program
{
    private static string[] _args;
    private static CancellationTokenSource _cancellationTokenSource;

    public static Configuration Config { get; set; }

    /// <summary>
    /// The LoggerFactory used to create logging objects.
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// Logging object.
    /// </summary>
    public static ILogger Logger { get; set; }

    /// <summary>
    /// Nodes to extend the address space.
    /// </summary>
    public static ImmutableList<IPluginNodes> PluginNodes { get; set; }

    /// <summary>
    /// OPC UA server object.
    /// </summary>
    public static PlcServer PlcServer { get; set; }

    /// <summary>
    /// Simulation object.
    /// </summary>
    public static PlcSimulation PlcSimulationInstance { get; set; }

    /// <summary>
    /// Service returning <see cref="DateTime"/> values and <see cref="Timer"/> instances. Mocked in tests.
    /// </summary>
    public static TimeService TimeService { get; set; } = new();

    /// <summary>
    /// A flag indicating when the server is up and ready to accept connections.
    /// </summary>
    public static bool Ready { get; set; }

    /// <summary>
    /// Synchronous main method of the app.
    /// </summary>
    public static void Main(string[] args)
    {
        // Start OPC UA server.
        StartAsync(args).Wait();
    }

    /// <summary>
    /// Start the PLC server and simulation.
    /// </summary>
    public static async Task StartAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // Initialize configuration.
        _args = args;
        LoadPluginNodes();
        (Config, PlcSimulationInstance, var extraArgs) = CliOptions.InitConfiguration(args, PluginNodes);

        InitLogging();

        // Show usage if requested
        if (Config.ShowHelp)
        {
            Logger.LogInformation(CliOptions.GetUsageHelp(Config.ProgramName));
            return;
        }

        // Validate and parse extra arguments
        if (extraArgs.Count > 0)
        {
            Logger.LogWarning($"Found one or more invalid command line arguments: {string.Join(" ", extraArgs)}");
            Logger.LogInformation(CliOptions.GetUsageHelp(Config.ProgramName));
        }

        LogLogo();

        Logger.LogInformation("Current directory: {currentDirectory}", Directory.GetCurrentDirectory());
        Logger.LogInformation("Log file: {logFileName}", Path.GetFullPath(Config.LogFileName));
        Logger.LogInformation("Log level: {logLevel}", Config.LogLevelCli);

        // Show version.
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        Logger.LogInformation("{ProgramName} {version} starting up ...",
            Config.ProgramName,
            $"v{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart} (SDK {Utils.GetAssemblyBuildNumber()})");
        Logger.LogDebug("Informational version: {version}",
            $"v{(Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion} (SDK {Utils.GetAssemblySoftwareVersion()} from {Utils.GetAssemblyTimestamp()})");
        Logger.LogDebug("Build date: {date}",
            $"{File.GetCreationTime(Assembly.GetExecutingAssembly().Location)}");

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
    public static async Task RestartAsync()
    {
        Logger.LogInformation("Stopping PLC server and simulation ...");
        PlcServer.Stop();
        PlcSimulationInstance.Stop();

        Logger.LogInformation("Restarting PLC server and simulation ...");
        LogLogo();

        (Config, PlcSimulationInstance, _) = CliOptions.InitConfiguration(_args, PluginNodes);
        InitLogging();
        await StartPlcServerAndSimulationAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stop the application.
    /// </summary>
    public static void Stop()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Load plugin nodes using reflection.
    /// </summary>
    private static void LoadPluginNodes()
    {
        var pluginNodesType = typeof(IPluginNodes);

        PluginNodes = pluginNodesType.Assembly.ExportedTypes
            .Where(t => pluginNodesType.IsAssignableFrom(t) &&
                        !t.IsInterface &&
                        !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t))
            .Cast<IPluginNodes>()
            .ToImmutableList();
    }

    /// <summary>
    /// Start web server to host pn.json.
    /// </summary>
    private static void StartWebServer(IHost host)
    {
        try
        {
            host.Start();

            if (Config.ShowPublisherConfigJsonIp)
            {
                Logger.LogInformation("Web server started: {pnJsonUri}", $"http://{GetIpAddress()}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else if (Config.ShowPublisherConfigJsonPh)
            {
                Logger.LogInformation("Web server started: {pnJsonUri}", $"http://{Config.OpcUa.Hostname}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else
            {
                Logger.LogInformation("Web server started on port {webServerPort}", Config.WebServerPort);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Could not start web server on port {webServerPort}: {message}",
                Config.WebServerPort,
                e.Message);
        }
    }

    /// <summary>
    /// Get IP address of first interface, otherwise host name.
    /// </summary>
    private static string GetIpAddress()
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
        catch { }

        return ip;
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    private static async Task StartPlcServerAsync(CancellationToken cancellationToken)
    {
        await StartPlcServerAndSimulationAsync().ConfigureAwait(false);

        if (Config.ShowPublisherConfigJsonIp)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                Config.PnJson,
                $"{GetIpAddress()}:{Config.OpcUa.ServerPort}{Config.OpcUa.ServerPath}",
                !Config.OpcUa.EnableUnsecureTransport,
                PluginNodes,
                Logger).ConfigureAwait(false);
        }
        else if (Config.ShowPublisherConfigJsonPh)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                Config.PnJson,
                $"{Config.OpcUa.Hostname}:{Config.OpcUa.ServerPort}{Config.OpcUa.ServerPath}",
                !Config.OpcUa.EnableUnsecureTransport,
                PluginNodes,
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
    }

    private static async Task StartPlcServerAndSimulationAsync()
    {
        // init OPC configuration and tracing
        ApplicationConfiguration plcApplicationConfiguration = await Config.OpcUa.ConfigureAsync().ConfigureAwait(false);

        // start the server.
        Logger.LogInformation("Starting server on endpoint {endpoint} ...", plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]);
        Logger.LogInformation("Simulation settings are:");
        Logger.LogInformation("One simulation phase consists of {SimulationCycleCount} cycles", PlcSimulationInstance.SimulationCycleCount);
        Logger.LogInformation("One cycle takes {SimulationCycleLength} ms", PlcSimulationInstance.SimulationCycleLength);
        Logger.LogInformation("Reference test simulation: {addReferenceTestSimulation}",
            PlcSimulationInstance.AddReferenceTestSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Simple events: {addSimpleEventsSimulation}",
            PlcSimulationInstance.AddSimpleEventsSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Alarms: {addAlarmSimulation}", PlcSimulationInstance.AddAlarmSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Deterministic alarms: {deterministicAlarmSimulation}",
            PlcSimulationInstance.DeterministicAlarmSimulationFile != null ? "Enabled" : "Disabled");

        Logger.LogInformation("Anonymous authentication: {anonymousAuth}", Config.DisableAnonymousAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Reject chain validation with CA certs with unknown revocation status: {rejectValidationUnknownRevocStatus}", Config.OpcUa.DontRejectUnknownRevocationStatus ? "Disabled" : "Enabled");
        Logger.LogInformation("Username/Password authentication: {usernamePasswordAuth}", Config.DisableUsernamePasswordAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Certificate authentication: {certAuth}", Config.DisableCertAuth ? "Disabled" : "Enabled");

        // Add simple events, alarms, reference test simulation and deterministic alarms.
        PlcServer = new PlcServer(TimeService);
        PlcServer.Start(plcApplicationConfiguration);
        Logger.LogInformation("OPC UA Server started");

        // Add remaining base simulations.
        PlcSimulationInstance.Start(PlcServer);
    }

    /// <summary>
    /// Initialize logging.
    /// </summary>
    private static void InitLogging()
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
            Config.LogFileName = $"{Dns.GetHostName().Split('.')[0].ToLowerInvariant()}-{Config.OpcUa.ServerPort}-plc.log";
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
    public static IHost CreateHostBuilder(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory()); // Avoid System.InvalidOperationException.
                webBuilder.UseUrls($"http://*:{Config.WebServerPort}");
                webBuilder.UseStartup<Startup>();
            }).Build();

        return host;
    }

    private static void LogLogo()
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
