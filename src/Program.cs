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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static OpcPlc.OpcApplicationConfiguration;
using static OpcPlc.PlcSimulation;

public static class Program
{
    private static CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Name of the application.
    /// </summary>
    public const string ProgramName = "OpcPlc";

    /// <summary>
    /// The LoggerFactory used to create logging objects.
    /// </summary>
    public static ILoggerFactory LoggerFactory = null;

    /// <summary>
    /// Logging object.
    /// </summary>
    public static ILogger Logger = null;

    /// <summary>
    /// Nodes to extend the address space.
    /// </summary>
    public static ImmutableList<IPluginNodes> PluginNodes;

    /// <summary>
    /// OPC UA server object.
    /// </summary>
    public static PlcServer PlcServer = null;

    /// <summary>
    /// Simulation object.
    /// </summary>
    public static PlcSimulation PlcSimulation = null;

    /// <summary>
    /// Service returning <see cref="DateTime"/> values and <see cref="Timer"/> instances. Mocked in tests.
    /// </summary>
    public static TimeService TimeService = new();

    /// <summary>
    /// A flag indicating when the server is up and ready to accept connections.
    /// </summary>
    public static volatile bool Ready = false;

    public static bool DisableAnonymousAuth { get; set; } = false;

    public static bool DisableUsernamePasswordAuth { get; set; } = false;

    public static bool DisableCertAuth { get; set; } = false;

    /// <summary>
    /// Admin user.
    /// </summary>
    public static string AdminUser { get; set; } = "sysadmin";

    /// <summary>
    /// Admin user password.
    /// </summary>
    public static string AdminPassword { get; set; } = "demo";

    /// <summary>
    /// Default user.
    /// </summary>
    public static string DefaultUser { get; set; } = "user1";

    /// <summary>
    /// Default user password.
    /// </summary>
    public static string DefaultPassword { get; set; } = "password";

    /// <summary>
    /// Show OPC Publisher configuration file using IP address as EndpointUrl.
    /// </summary>
    public static bool ShowPublisherConfigJsonIp { get; set; }

    /// <summary>
    /// Show OPC Publisher configuration file using plchostname as EndpointUrl.
    /// </summary>
    public static bool ShowPublisherConfigJsonPh { get; set; }

    /// <summary>
    /// Web server port for hosting OPC Publisher file.
    /// </summary>
    public static uint WebServerPort { get; set; } = 8080;

    /// <summary>
    /// Show usage help.
    /// </summary>
    public static bool ShowHelp { get; set; }

    public static string PnJson = "pn.json";

    /// <summary>
    /// Logging configuration.
    /// </summary>
    public static string LogFileName = $"hostname-port-plc.log"; // Set in InitLogging().
    public static string LogLevelCli = "info";
    public static TimeSpan LogFileFlushTimeSpanSec = TimeSpan.FromSeconds(30);

    public enum NodeType
    {
        UInt,
        Double,
        Bool,
        UIntArray,
    }

    /// <summary>
    /// Synchronous main method of the app.
    /// </summary>
    public static void Main(string[] args)
    {
        // Start OPC UA server.
        MainAsync(args).Wait();
    }

    /// <summary>
    /// Asynchronous part of the main method of the app.
    /// </summary>
    public static async Task MainAsync(string[] args, CancellationToken cancellationToken = default)
    {
        LoadPluginNodes();

        Mono.Options.OptionSet options = CliOptions.InitCommandLineOptions();

        // Parse the command line
        List<string> extraArgs = options.Parse(args);

        InitLogging();

        // Show usage if requested
        if (ShowHelp)
        {
            CliOptions.PrintUsage(options);
            return;
        }

        // Validate and parse extra arguments
        if (extraArgs.Count > 0)
        {
            Logger.LogWarning($"Found one or more invalid command line arguments: {string.Join(" ", extraArgs)}");
            CliOptions.PrintUsage(options);
        }

        LogLogo();

        Logger.LogInformation("Current directory: {currentDirectory}", Directory.GetCurrentDirectory());
        Logger.LogInformation("Log file: {logFileName}", Path.GetFullPath(LogFileName));
        Logger.LogInformation("Log level: {logLevel}", LogLevelCli);

        // Show version.
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        Logger.LogInformation("{ProgramName} {version} starting up ...",
            ProgramName,
            $"v{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart} (SDK {Utils.GetAssemblyBuildNumber()})");
        Logger.LogDebug("Informational version: {version}",
            $"v{(Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion} (SDK {Utils.GetAssemblySoftwareVersion()} from {Utils.GetAssemblyTimestamp()})");
        Logger.LogDebug("Build date: {date}",
            $"{File.GetCreationTime(Assembly.GetExecutingAssembly().Location)}");

        using var host = CreateHostBuilder(args);
        if (ShowPublisherConfigJsonIp || ShowPublisherConfigJsonPh)
        {
            StartWebServer(host);
        }

        try
        {
            await ConsoleServerAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "OPC UA server failed unexpectedly");
            throw;
        }

        Logger.LogInformation("OPC UA server exiting...");
    }

    public static void Shutdown()
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

            if (ShowPublisherConfigJsonIp)
            {
                Logger.LogInformation("Web server started: {pnJsonUri}", $"http://{GetIpAddress()}:{WebServerPort}/{PnJson}");
            }
            else if (ShowPublisherConfigJsonPh)
            {
                Logger.LogInformation("Web server started: {pnJsonUri}", $"http://{Hostname}:{WebServerPort}/{PnJson}");
            }
            else
            {
                Logger.LogInformation("Web server started on port {webServerPort}", WebServerPort);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Could not start web server on port {webServerPort}: {message}",
                WebServerPort,
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
    /// Run the server.
    /// </summary>
    private static async Task ConsoleServerAsync(CancellationToken cancellationToken)
    {
        // init OPC configuration and tracing
        var plcOpcApplicationConfiguration = new OpcApplicationConfiguration();
        ApplicationConfiguration plcApplicationConfiguration = await plcOpcApplicationConfiguration.ConfigureAsync().ConfigureAwait(false);

        // start the server.
        Logger.LogInformation("Starting server on endpoint {endpoint} ...", plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]);
        Logger.LogInformation("Simulation settings are:");
        Logger.LogInformation("One simulation phase consists of {SimulationCycleCount} cycles", SimulationCycleCount);
        Logger.LogInformation("One cycle takes {SimulationCycleLength} ms", SimulationCycleLength);
        Logger.LogInformation("Reference test simulation: {addReferenceTestSimulation}",
            AddReferenceTestSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Simple events: {addSimpleEventsSimulation}",
            AddSimpleEventsSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Alarms: {addAlarmSimulation}", AddAlarmSimulation ? "Enabled" : "Disabled");
        Logger.LogInformation("Deterministic alarms: {deterministicAlarmSimulation}",
            DeterministicAlarmSimulationFile != null ? "Enabled" : "Disabled");

        Logger.LogInformation("Anonymous authentication: {anonymousAuth}", DisableAnonymousAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Reject chain validation with CA certs with unknown revocation status: {rejectValidationUnknownRevocStatus}", DontRejectUnknownRevocationStatus ? "Disabled" : "Enabled");
        Logger.LogInformation("Username/Password authentication: {usernamePasswordAuth}", DisableUsernamePasswordAuth ? "Disabled" : "Enabled");
        Logger.LogInformation("Certificate authentication: {certAuth}", DisableCertAuth ? "Disabled" : "Enabled");

        // Add simple events, alarms, reference test simulation and deterministic alarms.
        PlcServer = new PlcServer(TimeService);
        PlcServer.Start(plcApplicationConfiguration);
        Logger.LogInformation("OPC UA Server started");

        // Add remaining base simulations.
        PlcSimulation = new PlcSimulation(PlcServer);
        PlcSimulation.Start();

        if (ShowPublisherConfigJsonIp)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                PnJson,
                $"{GetIpAddress()}:{ServerPort}{ServerPath}",
                PluginNodes,
                Logger).ConfigureAwait(false);
        }
        else if (ShowPublisherConfigJsonPh)
        {
            await PnJsonHelper.PrintPublisherConfigJsonAsync(
                PnJson,
                $"{Hostname}:{ServerPort}{ServerPath}",
                PluginNodes,
                Logger).ConfigureAwait(false);
        }

        Ready = true;
        Logger.LogInformation("PLC simulation started, press Ctrl+C to exit ...");

        // Wait for Ctrl-C to allow canceling the connection process.
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, eArgs) => {
            _cancellationTokenSource.Cancel();
            eArgs.Cancel = true;
        };

        await _cancellationTokenSource.Token.WhenCanceled().ConfigureAwait(false);

        PlcSimulation.Stop();
        PlcServer.Stop();
    }

    /// <summary>
    /// Initialize logging.
    /// </summary>
    private static void InitLogging()
    {
        if (LoggerFactory != null && Logger != null)
        {
            return;
        }

        LogLevel logLevel;

        // set the log level
        switch (LogLevelCli)
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
                throw new ArgumentOutOfRangeException(nameof(LogLevelCli), $"Unknown log level: {LogLevelCli}");
        }

        LoggerFactory = LoggingProvider.CreateDefaultLoggerFactory(logLevel);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
        {
            LogFileName = Environment.GetEnvironmentVariable("_GW_LOGP");
        }
        else
        {
            LogFileName = $"{Dns.GetHostName().Split('.')[0].ToLowerInvariant()}-{ServerPort}-plc.log";
        }

        if (!string.IsNullOrEmpty(LogFileName))
        {
            // configure rolling file sink
            const int MAX_LOGFILE_SIZE = 1024 * 1024;
            const int MAX_RETAINED_LOGFILES = 2;
            LoggerFactory.AddFile(LogFileName, logLevel, levelOverrides: null, isJson: false, fileSizeLimitBytes: MAX_LOGFILE_SIZE, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
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
                webBuilder.UseUrls($"http://*:{WebServerPort}");
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
