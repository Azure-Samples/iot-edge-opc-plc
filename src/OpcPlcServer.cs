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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public partial class OpcPlcServer
{
    private const int DefaultMinThreads = 20;
    private const int DefaultCompletionPortThreads = 20;

    private string[] _args;
    private CancellationTokenSource _cancellationTokenSource;
    private ImmutableList<IPluginNodes> _pluginNodes;
    private OpcTelemetryContext _telemetryContext;
    private IDisposable _otelProviders;

    public OpcPlcConfiguration Config { get; set; }

    /// <summary>
    /// The LoggerFactory used to create logging objects.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; }

    private ILogger _logger;

    /// <summary>
    /// Logging object.
    /// </summary>
#pragma warning disable S2292 // Trivial properties should be auto-implemented
    // Source generator is used to inject logging into this class,
    // so we need a backing field for the Logger property.
    public ILogger Logger
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => _logger;
        set => _logger = value;
    }

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
        try
        {
            // Initialize configuration.
            _args = args;
            Config = new OpcPlcConfiguration();
            string version = OpcTelemetryContext.ResolveOpcPlcVersion();
            InitLogging(Config.ProgramName, version);

            LoadPluginNodes();
            (PlcSimulationInstance, var extraArgs) = CliOptions.InitConfiguration(args, Config, _pluginNodes);

            // Show usage if requested
            if (Config.ShowHelp)
            {
                LogUsageHelp(CliOptions.GetUsageHelp(Config.ProgramName));
                return;
            }

            // Validate and parse extra arguments.
            if (extraArgs.Count > 0)
            {
                LogInvalidArgs(string.Join(" ", extraArgs));
                LogUsageHelp(CliOptions.GetUsageHelp(Config.ProgramName));
            }

            LogLogo();

            ThreadPool.SetMinThreads(DefaultMinThreads, DefaultCompletionPortThreads);
            ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
            LogMinWorkerThreads(minWorkerThreads, minCompletionPortThreads);

            LogCurrentDirectory(Directory.GetCurrentDirectory());
            LogLogFile(Path.GetFullPath(Config.LogFileName));
            LogLogLevel(Config.LogLevelCli);

            // Show OPC PLC version.
            LogStartingUp(
                Config.ProgramName,
                version,
                File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location));
            LogInformationalVersion(
                Config.ProgramName,
                (Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion);

            // Show OPC UA SDK version.
            LogOpcUaSdkVersion(
                Utils.GetAssemblyBuildNumber(),
                Utils.GetAssemblyTimestamp());
            LogOpcUaSdkInformationalVersion(
                Utils.GetAssemblySoftwareVersion());

            if (Config.OtlpEndpointUri is not null)
            {
                _otelProviders = OtelHelper.ConfigureOpenTelemetry(
                    Config.ProgramName,
                    Config.OtlpEndpointUri,
                    Config.OtlpExportProtocol,
                    Config.OtlpExportInterval,
                    _telemetryContext.ActivitySource.Name);
            }

            using var host = CreateHostBuilder(args);
            if (Config.ShowPublisherConfigJsonIp || Config.ShowPublisherConfigJsonPh)
            {
                StartWebServer(host);
            }

            await StartPlcServerAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogServerFailedUnexpectedly(ex);
            throw;
        }
        finally
        {
            _otelProviders?.Dispose();
            _telemetryContext?.Dispose();
            LoggerFactory?.Dispose();
        }

        LogServerExiting();
    }

    /// <summary>
    /// Restart the PLC server and simulation.
    /// </summary>
    public async Task RestartAsync()
    {
        LogStoppingPlcServer();
        await PlcServer.StopAsync(CancellationToken.None).ConfigureAwait(false);
        PlcSimulationInstance.Stop();

        LogRestartingPlcServer();
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
                LogWebServerStartedUri($"http://{GetIpAddress()}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else if (Config.ShowPublisherConfigJsonPh)
            {
                LogWebServerStartedUri($"http://{Config.OpcUa.Hostname}:{Config.WebServerPort}/{Config.PnJson}");
            }
            else
            {
                LogWebServerStartedPort(Config.WebServerPort);
            }
        }
        catch (Exception e)
        {
            LogCouldNotStartWebServer(e, Config.WebServerPort, e.Message);
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
        LogPlcSimulationStarted();

        // Wait for cancellation.
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, eArgs) => {
            _cancellationTokenSource.Cancel();
            eArgs.Cancel = true;
        };

        await _cancellationTokenSource.Token.WhenCanceled().ConfigureAwait(false);

        PlcSimulationInstance.Stop();
        await PlcServer.StopAsync(cancellationToken).ConfigureAwait(false);
        _cancellationTokenSource.Dispose();
    }

    private async Task StartPlcServerAndSimulationAsync()
    {
        // init OPC configuration and tracing
        var opcUaAppConfigFactory = new OpcUaAppConfigFactory(Config, Logger, LoggerFactory, _telemetryContext);
        ApplicationConfiguration plcApplicationConfiguration = await opcUaAppConfigFactory.ConfigureAsync().ConfigureAwait(false);

        // start the server.
        LogStartingServerOnEndpoint(plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]);
        LogSimulationSettings();
        LogSimulationCycleCount(PlcSimulationInstance.SimulationCycleCount);
        LogSimulationCycleLength(PlcSimulationInstance.SimulationCycleLength);
        LogReferenceTestSimulation(
            PlcSimulationInstance.AddReferenceTestSimulation ? "Enabled" : "Disabled");
        LogSimpleEvents(
            PlcSimulationInstance.AddSimpleEventsSimulation ? "Enabled" : "Disabled");
        LogAlarms(PlcSimulationInstance.AddAlarmSimulation ? "Enabled" : "Disabled");
        LogDeterministicAlarms(
            PlcSimulationInstance.DeterministicAlarmSimulationFile != null ? "Enabled" : "Disabled");

        LogAnonymousAuth(Config.DisableAnonymousAuth ? "Disabled" : "Enabled");
        LogRejectUnknownRevocationStatus(Config.OpcUa.DontRejectUnknownRevocationStatus ? "Disabled" : "Enabled");
        LogUsernamePasswordAuth(Config.DisableUsernamePasswordAuth ? "Disabled" : "Enabled");
        LogCertAuth(Config.DisableCertAuth ? "Disabled" : "Enabled");

        // Add simple events, alarms, reference test simulation and deterministic alarms.
        PlcServer = new PlcServer(Config, PlcSimulationInstance, TimeService, _pluginNodes, Logger, _telemetryContext);
        await PlcServer.StartAsync(plcApplicationConfiguration).ConfigureAwait(false);
        LogOpcUaServerStarted();

        // Add remaining base simulations.
        PlcSimulationInstance.Start(PlcServer);
    }

    /// <summary>
    /// Initialize logging.
    /// </summary>
    private void InitLogging(string name, string version)
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

        _telemetryContext = new OpcTelemetryContext(LoggerFactory, name, version);
        Logger = _telemetryContext.LoggerFactory.CreateLogger(name);
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
        LogOpcPlcLogo(
            @"
 ██████╗ ██████╗  ██████╗    ██████╗ ██╗      ██████╗
██╔═══██╗██╔══██╗██╔════╝    ██╔══██╗██║     ██╔════╝
██║   ██║██████╔╝██║         ██████╔╝██║     ██║
██║   ██║██╔═══╝ ██║         ██╔═══╝ ██║     ██║
╚██████╔╝██║     ╚██████╗    ██║     ███████╗╚██████╗
 ╚═════╝ ╚═╝      ╚═════╝    ╚═╝     ╚══════╝ ╚═════╝
");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{UsageHelp}")]
    partial void LogUsageHelp(string usageHelp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Found one or more invalid command line arguments: {InvalidArgs}")]
    partial void LogInvalidArgs(string invalidArgs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Min worker threads: {MinWorkerThreads}, min completion port threads: {MinCompletionPortThreads}")]
    partial void LogMinWorkerThreads(int minWorkerThreads, int minCompletionPortThreads);

    [LoggerMessage(Level = LogLevel.Information, Message = "Current directory: {CurrentDirectory}")]
    partial void LogCurrentDirectory(string currentDirectory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Log file: {LogFileName}")]
    partial void LogLogFile(string logFileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Log level: {LogLevel}")]
    partial void LogLogLevel(string logLevel);

    [LoggerMessage(Level = LogLevel.Information, Message = "{ProgramName} v{Version} from {Date} starting up ...")]
    partial void LogStartingUp(string programName, string version, DateTime date);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ProgramName} informational version: v{Version}")]
    partial void LogInformationalVersion(string programName, string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "OPC UA SDK {Version} from {Date}")]
    partial void LogOpcUaSdkVersion(string version, DateTime date);

    [LoggerMessage(Level = LogLevel.Debug, Message = "OPC UA SDK informational version: {Version}")]
    partial void LogOpcUaSdkInformationalVersion(string version);

    [LoggerMessage(Level = LogLevel.Critical, Message = "OPC UA server failed unexpectedly")]
    partial void LogServerFailedUnexpectedly(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "OPC UA server exiting ...")]
    partial void LogServerExiting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping PLC server and simulation ...")]
    partial void LogStoppingPlcServer();

    [LoggerMessage(Level = LogLevel.Information, Message = "Restarting PLC server and simulation ...")]
    partial void LogRestartingPlcServer();

    [LoggerMessage(Level = LogLevel.Information, Message = "Web server started: {PnJsonUri}")]
    partial void LogWebServerStartedUri(string pnJsonUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Web server started on port {WebServerPort}")]
    partial void LogWebServerStartedPort(uint webServerPort);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not start web server on port {WebServerPort}: {Message}")]
    partial void LogCouldNotStartWebServer(Exception exception, uint webServerPort, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "PLC simulation started, press Ctrl+C to exit ...")]
    partial void LogPlcSimulationStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting server on endpoint {Endpoint} ...")]
    partial void LogStartingServerOnEndpoint(string endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulation settings are:")]
    partial void LogSimulationSettings();

    [LoggerMessage(Level = LogLevel.Information, Message = "One simulation phase consists of {SimulationCycleCount} cycles")]
    partial void LogSimulationCycleCount(int simulationCycleCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "One cycle takes {SimulationCycleLength:N0} ms")]
    partial void LogSimulationCycleLength(int simulationCycleLength);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reference test simulation: {AddReferenceTestSimulation}")]
    partial void LogReferenceTestSimulation(string addReferenceTestSimulation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Simple events: {AddSimpleEventsSimulation}")]
    partial void LogSimpleEvents(string addSimpleEventsSimulation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Alarms: {AddAlarmSimulation}")]
    partial void LogAlarms(string addAlarmSimulation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deterministic alarms: {DeterministicAlarmSimulation}")]
    partial void LogDeterministicAlarms(string deterministicAlarmSimulation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Anonymous authentication: {AnonymousAuth}")]
    partial void LogAnonymousAuth(string anonymousAuth);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reject chain validation with CA certs with unknown revocation status: {RejectValidationUnknownRevocStatus}")]
    partial void LogRejectUnknownRevocationStatus(string rejectValidationUnknownRevocStatus);

    [LoggerMessage(Level = LogLevel.Information, Message = "Username/Password authentication: {UsernamePasswordAuth}")]
    partial void LogUsernamePasswordAuth(string usernamePasswordAuth);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certificate authentication: {CertAuth}")]
    partial void LogCertAuth(string certAuth);

    [LoggerMessage(Level = LogLevel.Information, Message = "OPC UA Server started")]
    partial void LogOpcUaServerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "{Logo}")]
    partial void LogOpcPlcLogo(string logo);
}
