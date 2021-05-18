namespace OpcPlc
{
    using Mono.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Opc.Ua;
    using Serilog;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using static OpcPlc.OpcApplicationConfiguration;
    using static OpcPlc.PlcSimulation;
    using System.Net;
    using Microsoft.Extensions.Hosting;

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
        /// OPC UA server object.
        /// </summary>
        public static PlcServer PlcServer = null;

        /// <summary>
        /// Simulation object.
        /// </summary>
        public static PlcSimulation PlcSimulation = null;
        
        /// <summary>
        /// A flag indicating when the server is up and ready to accept connections.
        /// </summary>
        public static volatile bool Ready = false;

        /// <summary>
        /// Shutdown token.
        /// </summary>
        public static CancellationToken ShutdownToken;

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
        /// User node configuration file name.
        /// </summary>
        public static string NodesFileName { get; set; }

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

            InitLogging();

            // Start OPC UA server
            MainAsync(args).Wait();
        }

        /// <summary>
        /// Asynchronous part of the main method of the app.
        /// </summary>
        public static async Task MainAsync(string[] args)
        {
            Mono.Options.OptionSet options = InitCommandLineOptions();

            List<string> extraArgs;
            try
            {
                // parse the command line
                extraArgs = options.Parse(args);
            }
            catch (OptionException e)
            {
                // show message
                Logger.Fatal(e, "Error in command line options");
                Logger.Error($"Command line arguments: {string.Join(" ", args)}");
                // show usage
                Usage(options);
                return;
            }

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
                await ConsoleServerAsync(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "OPC UA server failed unexpectedly.");
            }

            Logger.Information("OPC UA server exiting...");
        }

        private static Mono.Options.OptionSet InitCommandLineOptions()
        {
            return new Mono.Options.OptionSet {
                // log configuration
                { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './{_logFileName}'", (string l) => _logFileName = l },
                { "lt|logflushtimespan=", $"the timespan in seconds when the logfile should be flushed.\nDefault: {_logFileFlushTimeSpanSec} sec", (int s) => {
                        if (s > 0)
                        {
                            _logFileFlushTimeSpanSec = TimeSpan.FromSeconds(s);
                        }
                        else
                        {
                            throw new OptionException("The logflushtimespan must be a positive number.", "logflushtimespan");
                        }
                    }
                },
                { "ll|loglevel=", "the loglevel to use (allowed: fatal, error, warn, info, debug, verbose).\nDefault: info", (string l) => {
                        var logLevels = new List<string> {"fatal", "error", "warn", "info", "debug", "verbose"};
                        if (logLevels.Contains(l.ToLowerInvariant()))
                        {
                            _logLevel = l.ToLowerInvariant();
                        }
                        else
                        {
                            throw new OptionException("The loglevel must be one of: fatal, error, warn, info, debug, verbose", "loglevel");
                        }
                    }
                },

                // simulation configuration
                { "sc|simulationcyclecount=", $"count of cycles in one simulation phase\nDefault:  {SimulationCycleCount} cycles", (int i) => SimulationCycleCount = i },
                { "ct|cycletime=", $"length of one cycle time in milliseconds\nDefault:  {SimulationCycleLength} msec", (int i) => SimulationCycleLength = i },
                { "ns|nospikes", $"do not generate spike data\nDefault: {!GenerateSpikes}", a => GenerateSpikes = a == null },
                { "nd|nodips", $"do not generate dip data\nDefault: {!GenerateDips}", a => GenerateDips = a == null },
                { "np|nopostrend", $"do not generate positive trend data\nDefault: {!GeneratePosTrend}", a => GeneratePosTrend = a == null },
                { "nn|nonegtrend", $"do not generate negative trend data\nDefault: {!GenerateNegTrend}", a => GenerateNegTrend = a == null },
                { "nv|nodatavalues", $"do not generate data values\nDefault: {!GenerateData}", a => GenerateData = a == null },

                // Slow and fast nodes.
                { "sn|slownodes=", $"number of slow nodes\nDefault: {SlowNodeCount}", (uint i) => SlowNodeCount = i },
                { "sr|slowrate=", $"rate in seconds to change slow nodes\nDefault: {SlowNodeRate}", (uint i) => SlowNodeRate = i },
                { "st|slowtype=", $"data type of slow nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {SlowNodeType}", a => SlowNodeType = ParseNodeType(a) },
                { "ssi|slownodesamplinginterval=", $"rate in milliseconds to sample slow nodes\nDefault: {SlowNodeSamplingInterval}", (uint i) => SlowNodeSamplingInterval = i },
                { "fn|fastnodes=", $"number of fast nodes\nDefault: {FastNodeCount}", (uint i) => FastNodeCount = i },
                { "fr|fastrate=", $"rate in seconds to change fast nodes\nDefault: {FastNodeRate}", (uint i) => FastNodeRate = i },
                { "ft|fasttype=", $"data type of fast nodes ({string.Join("|", Enum.GetNames(typeof(NodeType)))})\nDefault: {FastNodeType}", a => FastNodeType = ParseNodeType(a) },
                { "fsi|fastnodesamplinginterval=", $"rate in milliseconds to sample fast nodes\nDefault: {FastNodeSamplingInterval}", (uint i) => FastNodeSamplingInterval = i },

                // user defined nodes configuration
                { "nf|nodesfile=", "the filename which contains the list of nodes to be created in the OPC UA address space.", (string l) => NodesFileName = l },

                // opc configuration
                { "pn|portnum=", $"the server port of the OPC server endpoint.\nDefault: {ServerPort}", (ushort p) => ServerPort = p },
                { "op|path=", $"the enpoint URL path part of the OPC server endpoint.\nDefault: '{ServerPath}'", (string a) => ServerPath = a },
                { "ph|plchostname=", $"the fullqualified hostname of the plc.\nDefault: {Hostname}", (string a) => Hostname = a },
                { "ol|opcmaxstringlen=", $"the max length of a string opc can transmit/receive.\nDefault: {OpcMaxStringLength}", (int i) => {
                        if (i > 0)
                        {
                            OpcMaxStringLength = i;
                        }
                        else
                        {
                            throw new OptionException("The max opc string length must be larger than 0.", "opcmaxstringlen");
                        }
                    }
                },
                { "lr|ldsreginterval=", $"the LDS(-ME) registration interval in ms. If 0, then the registration is disabled.\nDefault: {LdsRegistrationInterval}", (int i) => {
                        if (i >= 0)
                        {
                            LdsRegistrationInterval = i;
                        }
                        else
                        {
                            throw new OptionException("The ldsreginterval must be larger or equal 0.", "ldsreginterval");
                        }
                    }
                },
                { "aa|autoaccept", $"all certs are trusted when a connection is established.\nDefault: {AutoAcceptCerts}", a => AutoAcceptCerts = a != null },

                { "ut|unsecuretransport", $"enables the unsecured transport.\nDefault: {EnableUnsecureTransport}", u => EnableUnsecureTransport = u != null },

                { "to|trustowncert", $"the own certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", t => TrustMyself = t != null },

                // cert store options
                { "at|appcertstoretype=", $"the own application cert store type. \n(allowed values: Directory, X509Store)\nDefault: '{OpcOwnCertStoreType}'", (string s) => {
                        if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) || s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                        {
                            OpcOwnCertStoreType = s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ? CertificateStoreType.X509Store : CertificateStoreType.Directory;
                            OpcOwnCertStorePath = s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ? OpcOwnCertX509StorePathDefault : OpcOwnCertDirectoryStorePathDefault;
                        }
                        else
                        {
                            throw new OptionException();
                        }
                    }
                },

                { "ap|appcertstorepath=", "the path where the own application cert should be stored\nDefault (depends on store type):\n" +
                        $"X509Store: '{OpcOwnCertX509StorePathDefault}'\n" +
                        $"Directory: '{OpcOwnCertDirectoryStorePathDefault}'", (string s) => OpcOwnCertStorePath = s
                },

                { "tp|trustedcertstorepath=", $"the path of the trusted cert store\nDefault '{OpcTrustedCertDirectoryStorePathDefault}'", (string s) => OpcTrustedCertStorePath = s
                },

                { "rp|rejectedcertstorepath=", $"the path of the rejected cert store\nDefault '{OpcRejectedCertDirectoryStorePathDefault}'", (string s) => OpcRejectedCertStorePath = s
                },

                { "ip|issuercertstorepath=", $"the path of the trusted issuer cert store\nDefault '{OpcIssuerCertDirectoryStorePathDefault}'", (string s) => OpcIssuerCertStorePath = s
                },

                { "csr", $"show data to create a certificate signing request\nDefault '{ShowCreateSigningRequestInfo}'", c => ShowCreateSigningRequestInfo = c != null
                },

                { "ab|applicationcertbase64=", "update/set this applications certificate with the certificate passed in as bas64 string", (string s) => NewCertificateBase64String = s
                },
                { "af|applicationcertfile=", "update/set this applications certificate with the certificate file specified", (string s) =>
                    {
                        if (File.Exists(s))
                        {
                            NewCertificateFileName = s;
                        }
                        else
                        {
                            throw new OptionException("The file '{s}' does not exist.", "applicationcertfile");
                        }
                    }
                },

                { "pb|privatekeybase64=", "initial provisioning of the application certificate (with a PEM or PFX fomat) requires a private key passed in as base64 string", (string s) => PrivateKeyBase64String = s
                },
                { "pk|privatekeyfile=", "initial provisioning of the application certificate (with a PEM or PFX fomat) requires a private key passed in as file", (string s) =>
                    {
                        if (File.Exists(s))
                        {
                            PrivateKeyFileName = s;
                        }
                        else
                        {
                            throw new OptionException("The file '{s}' does not exist.", "privatekeyfile");
                        }
                    }
                },

                { "cp|certpassword=", "the optional password for the PEM or PFX or the installed application certificate", (string s) => CertificatePassword = s
                },

                { "tb|addtrustedcertbase64=", "adds the certificate to the applications trusted cert store passed in as base64 string (multiple strings supported)", (string s) => TrustedCertificateBase64Strings = ParseListOfStrings(s)
                },
                { "tf|addtrustedcertfile=", "adds the certificate file(s) to the applications trusted cert store passed in as base64 string (multiple filenames supported)", (string s) => TrustedCertificateFileNames = ParseListOfFileNames(s, "addtrustedcertfile")
                },

                { "ib|addissuercertbase64=", "adds the specified issuer certificate to the applications trusted issuer cert store passed in as base64 string (multiple strings supported)", (string s) => IssuerCertificateBase64Strings = ParseListOfStrings(s)
                },
                { "if|addissuercertfile=", "adds the specified issuer certificate file(s) to the applications trusted issuer cert store (multiple filenames supported)", (string s) => IssuerCertificateFileNames = ParseListOfFileNames(s, "addissuercertfile")
                },

                { "rb|updatecrlbase64=", "update the CRL passed in as base64 string to the corresponding cert store (trusted or trusted issuer)", (string s) => CrlBase64String = s
                },
                { "uc|updatecrlfile=", "update the CRL passed in as file to the corresponding cert store (trusted or trusted issuer)", (string s) =>
                    {
                        if (File.Exists(s))
                        {
                            CrlFileName = s;
                        }
                        else
                        {
                            throw new OptionException("The file '{s}' does not exist.", "updatecrlfile");
                        }
                    }
                },

                { "rc|removecert=", "remove cert(s) with the given thumbprint(s) (multiple thumbprints supported)", (string s) => ThumbprintsToRemove = ParseListOfStrings(s)
                },

                {"daa|disableanonymousauth", $"flag to disable anonymous authentication. \nDefault: {DisableAnonymousAuth}", d => DisableAnonymousAuth = d != null },
                {"dua|disableusernamepasswordauth", $"flag to disable username/password authentication. \nDefault: {DisableUsernamePasswordAuth}", d=> DisableUsernamePasswordAuth = d != null },
                {"dca|disablecertauth", $"flag to disable certificate authentication. \nDefault: {DisableCertAuth}", d => DisableCertAuth = d != null },

                // user management
                { "au|adminuser=", $"the username of the admin user.\nDefault: {AdminUser}", (string p) => AdminUser = p ?? AdminUser},
                { "ac|adminpassword=", $"the password of the administrator.\nDefault: {AdminPassword}", (string p) => AdminPassword = p ?? AdminPassword},
                { "du|defaultuser=", $"the username of the default user.\nDefault: {DefaultUser}", (string p) => DefaultUser = p ?? DefaultUser},
                { "dc|defaultpassword=", $"the password of the default user.\nDefault: {DefaultPassword}", (string p) => DefaultPassword = p ?? DefaultPassword},

                // Special nodes
                { "ctb|complextypeboiler", $"add complex type (boiler) to address space.\nDefault: {AddComplexTypeBoiler}", h => AddComplexTypeBoiler = h != null },
                { "scn|specialcharname", $"add node with special characters in name.\nDefault: {AddSpecialCharName}", h => AddSpecialCharName = h != null },
                { "lid|longid", $"add node with ID of 3950 chars.\nDefault: {AddLongId}", h => AddLongId = h != null },
                { "lsn|longstringnodes", $"add nodes with string values of 10/50/100/200 kB.\nDefault: {AddLongStringNodes}", h => AddLongStringNodes = h != null },
                { "alm|alarms", $"add alarm simulation to address space.\nDefault: {AddAlarmSimulation}", h => AddAlarmSimulation = h != null },
                { "ses|simpleevents", $"add simple events simulation to address space.\nDefault: {AddSimpleEventsSimulation}", h => AddSimpleEventsSimulation = h != null },
                { "ref|referencetest", $"add reference test simulation node manager to address space.\nDefault: {AddReferenceTestSimulation}", h => AddReferenceTestSimulation = h != null },

                // misc
                { "sp|showpnjson", $"show OPC Publisher configuration file using IP address as EndpointUrl.\nDefault: {ShowPublisherConfigJsonIp}", h => ShowPublisherConfigJsonIp = h != null },
                { "sph|showpnjsonph", $"show OPC Publisher configuration file using plchostname as EndpointUrl.\nDefault: {ShowPublisherConfigJsonPh}", h => ShowPublisherConfigJsonPh = h != null },
                { "spf|showpnfname=", $"filename of the OPC Publisher configuration file to write when using options sp/sph.\nDefault: {PnJson}", (string f) => PnJson = f },
                { "wp|webport=", $"web server port for hosting OPC Publisher configuration file.\nDefault: {WebServerPort}", (uint i) => WebServerPort = i },
                { "h|help", "show this message and exit", h => ShowHelp = h != null },
            };
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
            catch
            {
            }

            return ip;
        }

        /// <summary>
        /// Show and save pn.json
        /// </summary>
        private static async Task DumpPublisherConfigJsonAsync(string serverPath)
        {
            const string NSS = "ns=2;s=";
            var sb = new StringBuilder();

            sb.AppendLine(Environment.NewLine + "[");
            sb.AppendLine("  {");
            sb.AppendLine($"    \"EndpointUrl\": \"opc.tcp://{serverPath}\",");
            sb.AppendLine("    \"UseSecurity\": false,");
            sb.AppendLine("    \"OpcNodes\": [");

            if (GenerateData) sb.AppendLine($"      {{ \"Id\": \"{NSS}AlternatingBoolean\" }},");
            if (GenerateDips) sb.AppendLine($"      {{ \"Id\": \"{NSS}DipData\" }},");
            if (GenerateNegTrend) sb.AppendLine($"      {{ \"Id\": \"{NSS}NegativeTrendData\" }},");
            if (GeneratePosTrend) sb.AppendLine($"      {{ \"Id\": \"{NSS}PositiveTrendData\" }},");
            if (GenerateData) sb.AppendLine($"      {{ \"Id\": \"{NSS}RandomSignedInt32\" }},");
            if (GenerateData) sb.AppendLine($"      {{ \"Id\": \"{NSS}RandomUnsignedInt32\" }},");
            if (GenerateSpikes) sb.AppendLine($"      {{ \"Id\": \"{NSS}SpikeData\" }},");
            if (GenerateData) sb.AppendLine($"      {{ \"Id\": \"{NSS}StepUp\" }},");

            const string SpecialChars = @"\""!§$%&/()=?`´\\+~*'#_-:.;,<>|@^°€µ{[]}";
            if (AddSpecialCharName) sb.AppendLine($"      {{ \"Id\": \"{NSS}Special_{SpecialChars}\" }},");
            if (AddLongStringNodes)
            {
                sb.AppendLine($"      {{ \"Id\": \"{NSS}LongString10kB\" }},");
                sb.AppendLine($"      {{ \"Id\": \"{NSS}LongString50kB\" }},");
                sb.AppendLine($"      {{ \"Id\": \"{NSS}LongString100kB\" }},");
                sb.AppendLine($"      {{ \"Id\": \"{NSS}LongString200kB\" }},");
            }

            string slowPublishingInterval = SlowNodeRate > 1
                ? $", \"OpcPublishingInterval\": {SlowNodeRate * 1000}" // ms
                : "";
            string slowSamplingInterval = SlowNodeSamplingInterval > 0
                ? $", \"OpcSamplingInterval\": {SlowNodeSamplingInterval}" // ms
                : "";
            for (int i = 0; i < SlowNodeCount; i++)
            {
                sb.AppendLine($"      {{ \"Id\": \"{NSS}Slow{SlowNodeType}{i + 1}\"{slowPublishingInterval}{slowSamplingInterval} }},");
            }

            string fastPublishingInterval = FastNodeRate > 1
               ? $", \"OpcPublishingInterval\": {FastNodeRate * 1000}" // ms
               : "";
            string fastSamplingInterval = FastNodeSamplingInterval > 0
                ? $", \"OpcSamplingInterval\": {FastNodeSamplingInterval}" // ms
                : "";
            for (int i = 0; i < FastNodeCount; i++)
            {
                sb.AppendLine($"      {{ \"Id\": \"{NSS}Fast{FastNodeType}{i + 1}\"{fastPublishingInterval}{fastSamplingInterval} }},");
            }

            int trimLen = Environment.NewLine.Length + 1;
            sb.Remove(sb.Length - trimLen, trimLen); // Trim trailing ,\n.

            sb.AppendLine(Environment.NewLine + "    ]");
            sb.AppendLine("  }");
            sb.AppendLine("]");

            string pnJson = sb.ToString();
            Logger.Information($"OPC Publisher configuration file: {PnJson}" + pnJson);

            await File.WriteAllTextAsync(PnJson, pnJson.Trim()).ConfigureAwait(false);
        }

        /// <summary>
        /// Parse node data type, default to Int.
        /// </summary>
        private static NodeType ParseNodeType(string type)
        {
            return Enum.TryParse(type, ignoreCase: true, out NodeType nodeType)
                ? nodeType
                : NodeType.UInt;
        }

        /// <summary>
        /// Run the server.
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        private static async Task ConsoleServerAsync(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var quitEvent = new ManualResetEvent(false);
            var shutdownTokenSource = new CancellationTokenSource();
            ShutdownToken = shutdownTokenSource.Token;

            // init OPC configuration and tracing
            var plcOpcApplicationConfiguration = new OpcApplicationConfiguration();
            ApplicationConfiguration plcApplicationConfiguration = await plcOpcApplicationConfiguration.ConfigureAsync().ConfigureAwait(false);

            // allow canceling the connection process
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
            }

            // start the server.
            Logger.Information($"Starting server on endpoint {plcApplicationConfiguration.ServerConfiguration.BaseAddresses[0]} ...");
            Logger.Information("Simulation settings are:");
            Logger.Information($"One simulation phase consists of {SimulationCycleCount} cycles");
            Logger.Information($"One cycle takes {SimulationCycleLength} milliseconds");
            Logger.Information($"Spike generation is {(GenerateSpikes ? "enabled" : "disabled")}");
            Logger.Information($"Data generation is {(GenerateData ? "enabled" : "disabled")}");
            Logger.Information($"Complex type (boiler) is {(AddComplexTypeBoiler ? "enabled" : "disabled")}");
            Logger.Information($"Reference Test Simulation is {(AddReferenceTestSimulation ? "enabled" : "disabled")}");

            Logger.Information($"Anonymous authentication: {(DisableAnonymousAuth ? "disabled" : "enabled")}");
            Logger.Information($"Username/Password authentication: {(DisableUsernamePasswordAuth ? "disabled" : "enabled")}");
            Logger.Information($"Certificate authentication: {(DisableCertAuth ? "disabled" : "enabled")}");

            PlcServer = new PlcServer();
            PlcServer.Start(plcApplicationConfiguration);
            Logger.Information("OPC UA Server started.");

            PlcSimulation = new PlcSimulation(PlcServer);
            PlcSimulation.Start();

            if (ShowPublisherConfigJsonIp)
            {
                await DumpPublisherConfigJsonAsync($"{GetIpAddress()}:{ServerPort}{ServerPath}").ConfigureAwait(false);
            }
            else if (ShowPublisherConfigJsonPh) {
                await DumpPublisherConfigJsonAsync($"{Hostname}:{ServerPort}{ServerPath}").ConfigureAwait(false);
            }

            Ready = true;
            Logger.Information("PLC Simulation started. Press CTRL-C to exit.");

            // wait for Ctrl-C
            quitEvent.WaitOne(Timeout.Infinite);
            PlcSimulation.Stop();
            shutdownTokenSource.Cancel();
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
            switch (_logLevel)
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
                _logFileName = Environment.GetEnvironmentVariable("_GW_LOGP");
            }

            if (!string.IsNullOrEmpty(_logFileName))
            {
                // configure rolling file sink
                const int MAX_LOGFILE_SIZE = 1024 * 1024;
                const int MAX_RETAINED_LOGFILES = 2;
                loggerConfiguration.WriteTo.File(_logFileName, fileSizeLimitBytes: MAX_LOGFILE_SIZE, flushToDiskInterval: _logFileFlushTimeSpanSec, rollOnFileSizeLimit: true, retainedFileCountLimit: MAX_RETAINED_LOGFILES);
            }

            Logger = loggerConfiguration.CreateLogger();
            Logger.Information($"Current directory is: {Directory.GetCurrentDirectory()}");
            Logger.Information($"Log file is: {Path.GetFullPath(_logFileName)}");
            Logger.Information($"Log level is: {_logLevel}");
            return;
        }

        /// <summary>
        /// Helper to build a list of byte arrays out of a comma separated list of base64 strings (optional in double quotes).
        /// </summary>
        private static List<string> ParseListOfStrings(string s)
        {
            var strings = new List<string>();
            if (s[0] == '"' && (s.Count(c => c.Equals('"')) % 2 == 0))
            {
                while (s.Contains('"'))
                {
                    int first = 0;
                    int next = 0;
                    first = s.IndexOf('"', next);
                    next = s.IndexOf('"', ++first);
                    strings.Add(s[first..next]);
                    s = s.Substring(++next);
                }
            }
            else if (s.Contains(','))
            {
                strings = s.Split(',').ToList();
                strings.ForEach(st => st.Trim());
                strings = strings.Select(st => st.Trim()).ToList();
            }
            else
            {
                strings.Add(s);
            }
            return strings;
        }

        /// <summary>
        /// Helper to build a list of filenames out of a comma separated list of filenames (optional in double quotes).
        /// </summary>
        private static List<string> ParseListOfFileNames(string s, string option)
        {
            var fileNames = new List<string>();
            if (s[0] == '"' && (s.Count(c => c.Equals('"')) % 2 == 0))
            {
                while (s.Contains('"'))
                {
                    int first = 0;
                    int next = 0;
                    first = s.IndexOf('"', next);
                    next = s.IndexOf('"', ++first);
                    string fileName = s[first..next];
                    if (File.Exists(fileName))
                    {
                        fileNames.Add(fileName);
                    }
                    else
                    {
                        throw new OptionException($"The file '{fileName}' does not exist.", option);
                    }
                    s = s.Substring(++next);
                }
            }
            else if (s.Contains(','))
            {
                List<string> parsedFileNames = s.Split(',').ToList();
                parsedFileNames = parsedFileNames.Select(st => st.Trim()).ToList();
                foreach (var fileName in parsedFileNames)
                {
                    if (File.Exists(fileName))
                    {
                        fileNames.Add(fileName);
                    }
                    else
                    {
                        throw new OptionException($"The file '{fileName}' does not exist.", option);
                    }
                }
            }
            else
            {
                if (File.Exists(s))
                {
                    fileNames.Add(s);
                }
                else
                {
                    throw new OptionException($"The file '{s}' does not exist.", option);
                }
            }
            return fileNames;
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

        private static string _logFileName = $"{Dns.GetHostName().Split('.')[0].ToLowerInvariant()}-plc.log";
        private static string _logLevel = "info";
        private static TimeSpan _logFileFlushTimeSpanSec = TimeSpan.FromSeconds(30);
    }
}
