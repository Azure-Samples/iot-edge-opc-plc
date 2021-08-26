namespace OpcPlc
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Opc.Ua;
    using Serilog;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using static OpcPlc.OpcApplicationConfiguration;
    using static OpcPlc.PlcSimulation;
    using System.Net;
    using Microsoft.Extensions.Hosting;
    using OpcPlc.PluginNodes.Models;
    using OpcPlc.Helpers;
    using System.Linq;
    using System.Collections.Immutable;

    public static class Program
    {
        /// <summary>
        /// Name of the application.
        /// </summary>
        public const string ProgramName = "OpcPlc";

        /// <summary>
        /// Logging object.
        /// </summary>
        public static Serilog.Core.Logger Logger = null;

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
        public static TimeService TimeService = new TimeService();

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
        public static string LogFileName = $"{Dns.GetHostName().Split('.')[0].ToLowerInvariant()}-plc.log";
        public static string LogLevel = "info";
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
            InitAppLocation();

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

            // show usage if requested
            if (ShowHelp)
            {
                Usage(options);
                return;
            }

            // validate and parse extra arguments
            if (extraArgs.Count > 0)
            {
                Logger.Error("Error in command line options");
                Logger.Error($"Command line arguments: {string.Join(" ", args)}");
                Usage(options);
                return;
            }

            LogLogo();

            Logger.Information($"Current directory is: {Directory.GetCurrentDirectory()}");
            Logger.Information($"Log file is: {Path.GetFullPath(LogFileName)}");
            Logger.Information($"Log level is: {LogLevel}");

            //show version
            var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Logger.Information($"{ProgramName} V{fileVersion.ProductMajorPart}.{fileVersion.ProductMinorPart}.{fileVersion.ProductBuildPart} starting up...");
            Logger.Debug($"Informational version: V{(Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion}");

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
                Logger.Fatal(ex, "OPC UA server failed unexpectedly.");
            }

            Logger.Information("OPC UA server exiting...");
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
                Logger.Information($"Web server started on port {WebServerPort}");
            }
            catch (Exception)
            {
                Logger.Error($"Could not start web server on port {WebServerPort}");
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
            Logger.Information($"Starting server on endpoint {plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]} ...");
            Logger.Information("Simulation settings are:");
            Logger.Information($"One simulation phase consists of {SimulationCycleCount} cycles");
            Logger.Information($"One cycle takes {SimulationCycleLength} milliseconds");
            Logger.Information($"Complex type (boiler) is {(AddComplexTypeBoiler ? "enabled" : "disabled")}");
            Logger.Information($"Reference Test Simulation is {(AddReferenceTestSimulation ? "enabled" : "disabled")}");
            Logger.Information($"Simple Events is {(AddSimpleEventsSimulation ? "enabled" : "disabled")}");
            Logger.Information($"Alarms is {(AddAlarmSimulation ? "enabled" : "disabled")}");
            Logger.Information($"Deterministic Alarms is {(DeterministicAlarmSimulationFile != null ? "enabled" : "disabled")}");

            Logger.Information($"Anonymous authentication: {(DisableAnonymousAuth ? "disabled" : "enabled")}");
            Logger.Information($"Username/Password authentication: {(DisableUsernamePasswordAuth ? "disabled" : "enabled")}");
            Logger.Information($"Certificate authentication: {(DisableCertAuth ? "disabled" : "enabled")}");

            PlcServer = new PlcServer(TimeService);
            PlcServer.Start(plcApplicationConfiguration);
            Logger.Information("OPC UA Server started.");

            PlcSimulation = new PlcSimulation(PlcServer);
            PlcSimulation.Start();

            if (ShowPublisherConfigJsonIp)
            {
                await PnJsonHelper.DumpPublisherConfigJsonAsync(
                    PnJson,
                    $"{GetIpAddress()}:{ServerPort}{ServerPath}",
                    PluginNodes,
                    Logger).ConfigureAwait(false);
            }
            else if (ShowPublisherConfigJsonPh)
            {
                await PnJsonHelper.DumpPublisherConfigJsonAsync(
                    PnJson,
                    $"{Hostname}:{ServerPort}{ServerPath}",
                    PluginNodes,
                    Logger).ConfigureAwait(false);
            }

            Ready = true;
            Logger.Information("PLC Simulation started. Press CTRL-C to exit.");

            // wait for Ctrl-C

            // allow canceling the connection process
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Console.CancelKeyPress += (_, eArgs) =>
            {
                cancellationTokenSource.Cancel();
                eArgs.Cancel = true;
            };

            while (!cancellationTokenSource.Token.WaitHandle.WaitOne(1000))
            {
            }

            PlcSimulation.Stop();
            PlcServer.Stop();
        }

        /// <summary>
        /// Usage message.
        /// </summary>
        private static void Usage(Mono.Options.OptionSet options)
        {
            // show usage
            Logger.Information("");
            Logger.Information($"{ProgramName} V{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
            Logger.Information($"Informational version: V{(Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion}");
            Logger.Information("");
            Logger.Information("Usage: {0}.exe [<options>]", Assembly.GetEntryAssembly().GetName().Name);
            Logger.Information("");
            Logger.Information("OPC UA PLC for different data simulation scenarios");
            Logger.Information("To exit the application, press CTRL-C while it is running.");
            Logger.Information("");
            Logger.Information("To specify a list of strings, please use the following format:");
            Logger.Information("\"<string 1>,<string 2>,...,<string n>\"");
            Logger.Information("or if one string contains commas:");
            Logger.Information("\"\"<string 1>\",\"<string 2>\",...,\"<string n>\"\"");
            Logger.Information("");

            // output the options
            Logger.Information("Options:");
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            options.WriteOptionDescriptions(stringWriter);
            string[] helpLines = stringBuilder.ToString().Split("\n");
            foreach (var line in helpLines)
            {
                Logger.Information(line);
            }
            return;
        }

        /// <summary>
        /// Initialize logging.
        /// </summary>
        private static void InitLogging()
        {
            var loggerConfiguration = new LoggerConfiguration();

            // set the log level
            switch (LogLevel)
            {
                case "fatal":
                    loggerConfiguration.MinimumLevel.Fatal();
                    OpcTraceToLoggerFatal = 0;
                    break;
                case "error":
                    loggerConfiguration.MinimumLevel.Error();
                    OpcStackTraceMask = OpcTraceToLoggerError = Utils.TraceMasks.Error;
                    break;
                case "warn":
                    loggerConfiguration.MinimumLevel.Warning();
                    OpcTraceToLoggerWarning = 0;
                    break;
                case "info":
                    loggerConfiguration.MinimumLevel.Information();
                    OpcStackTraceMask = OpcTraceToLoggerInformation = 0;
                    break;
                case "debug":
                    loggerConfiguration.MinimumLevel.Debug();
                    OpcStackTraceMask = OpcTraceToLoggerDebug = Utils.TraceMasks.StackTrace | Utils.TraceMasks.Operation |
                        Utils.TraceMasks.StartStop | Utils.TraceMasks.ExternalSystem | Utils.TraceMasks.Security;
                    break;
                case "verbose":
                    loggerConfiguration.MinimumLevel.Verbose();
                    OpcStackTraceMask = OpcTraceToLoggerVerbose = Utils.TraceMasks.All;
                    break;
            }

            // set logging sinks
            loggerConfiguration.WriteTo.Console();

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("_GW_LOGP")))
            {
                LogFileName = Environment.GetEnvironmentVariable("_GW_LOGP");
            }

            if (!string.IsNullOrEmpty(LogFileName))
            {
                // configure rolling file sink
                const int MAX_LOGFILE_SIZE = 1024 * 1024;
                const int MAX_RETAINED_LOGFILES = 2;
                loggerConfiguration.WriteTo.File(LogFileName, fileSizeLimitBytes: MAX_LOGFILE_SIZE, flushToDiskInterval: LogFileFlushTimeSpanSec, rollOnFileSizeLimit: true, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
            }

            Logger = loggerConfiguration.CreateLogger();

            return;
        }

        /// <summary>
        /// Configure web server.
        /// </summary>
        public static IHost CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var x = Directory.GetCurrentDirectory();
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory()); // Avoid System.InvalidOperationException.
                    webBuilder.UseUrls($"http://*:{WebServerPort}");
                    webBuilder.UseStartup<Startup>();
                }).Build();

            return host;
        }

        /// <summary>
        /// Set app folder.
        /// </summary>
        private static void InitAppLocation()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string appFolder = Path.GetDirectoryName(exePath);

            // ASP.NET Core 3.1 uses src as default current directory.
            Directory.SetCurrentDirectory(appFolder);
        }

        private static void LogLogo()
        {
            Logger.Information(
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
}