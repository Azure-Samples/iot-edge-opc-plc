namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Mono.Options;
using Opc.Ua;
using OpcPlc.Certs;
using OpcPlc.Helpers;
using OpcPlc.Logging;
using OpcPlc.PluginNodes.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public static class CliOptions
{
    private static Mono.Options.OptionSet _options;

    public static (Configuration Config, PlcSimulation PlcSimulationInstance, List<string> ExtraArgs) InitConfiguration(string[] args, ImmutableList<IPluginNodes> pluginNodes)
    {
        var config = new Configuration();
        var loggerFactory = LoggingProvider.CreateDefaultLoggerFactory(LogLevel.Information);
        config.OpcUa = new OpcApplicationConfiguration(config, loggerFactory);
        var plcSimulation = new PlcSimulation(pluginNodes);

        _options = new Mono.Options.OptionSet {
            // log configuration
            { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './{config.LogFileName}'", (string s) => config.LogFileName = s },
            { "lt|logflushtimespan=", $"the timespan in seconds when the logfile should be flushed.\nDefault: {config.LogFileFlushTimeSpanSec} sec", (int i) => {
                    if (i > 0)
                    {
                        config.LogFileFlushTimeSpanSec = TimeSpan.FromSeconds(i);
                    }
                    else
                    {
                        throw new OptionException("The logflushtimespan must be a positive number.", "logflushtimespan");
                    }
                }
            },
            { "ll|loglevel=", "the loglevel to use (allowed: critical, error, warn, info, debug, trace).\nDefault: info", (string s) => {
                    var logLevels = new List<string> {"critical", "error", "warn", "info", "debug", "trace"};
                    if (logLevels.Contains(s.ToLowerInvariant()))
                    {
                        config.LogLevelCli = s.ToLowerInvariant();
                    }
                    else
                    {
                        throw new OptionException($"The loglevel must be one of: {string.Join(", ", logLevels)}", "loglevel");
                    }
                }
            },

            // simulation configuration
            { "sc|simulationcyclecount=", $"count of cycles in one simulation phase.\nDefault: {plcSimulation.SimulationCycleCount} cycles", (int i) => plcSimulation.SimulationCycleCount = i },
            { "ct|cycletime=", $"length of one cycle time in milliseconds.\nDefault: {plcSimulation.SimulationCycleLength} msec", (int i) => plcSimulation.SimulationCycleLength = i },

            // events
            { "ei|eventinstances=", $"number of event instances.\nDefault: {plcSimulation.EventInstanceCount}", (uint i) => plcSimulation.EventInstanceCount = i },
            { "er|eventrate=", $"rate in milliseconds to send events.\nDefault: {plcSimulation.EventInstanceRate}", (uint i) => plcSimulation.EventInstanceRate = i },

            // OPC configuration
            { "pn|portnum=", $"the server port of the OPC server endpoint.\nDefault: {config.OpcUa.ServerPort}", (ushort i) => config.OpcUa.ServerPort = i },
            { "op|path=", $"the endpoint URL path part of the OPC server endpoint.\nDefault: '{config.OpcUa.ServerPath}'", (string s) => config.OpcUa.ServerPath = s },
            { "ph|plchostname=", $"the fully-qualified hostname of the PLC.\nDefault: {config.OpcUa.Hostname}", (string s) => config.OpcUa.Hostname = s },
            { "ol|opcmaxstringlen=", $"the max length of a string OPC can transmit/receive.\nDefault: {config.OpcUa.OpcMaxStringLength}", (int i) => {
                    if (i > 0)
                    {
                        config.OpcUa.OpcMaxStringLength = i;
                    }
                    else
                    {
                        throw new OptionException("The max OPC string length must be larger than 0.", "opcmaxstringlen");
                    }
                }
            },
            { "lr|ldsreginterval=", $"the LDS(-ME) registration interval in ms. If 0, then the registration is disabled.\nDefault: {config.OpcUa.LdsRegistrationInterval}", (int i) => {
                    if (i >= 0)
                    {
                        config.OpcUa.LdsRegistrationInterval = i;
                    }
                    else
                    {
                        throw new OptionException("The ldsreginterval must be larger or equal 0.", "ldsreginterval");
                    }
                }
            },
            { "aa|autoaccept", $"all certs are trusted when a connection is established.\nDefault: {config.OpcUa.AutoAcceptCerts}", (string s) => config.OpcUa.AutoAcceptCerts = s != null },

            { "drurs|dontrejectunknownrevocationstatus", $"Don't reject chain validation with CA certs with unknown revocation status, e.g. when the CRL is not available or the OCSP provider is offline.\nDefault: {config.OpcUa.DontRejectUnknownRevocationStatus}", (string s) => config.OpcUa.DontRejectUnknownRevocationStatus = s != null },

            { "ut|unsecuretransport", $"enables the unsecured transport.\nDefault: {config.OpcUa.EnableUnsecureTransport}", (string s) => config.OpcUa.EnableUnsecureTransport = s != null },

            { "to|trustowncert", $"the own certificate is put into the trusted certificate store automatically.\nDefault: {config.OpcUa.TrustMyself}", (string s) => config.OpcUa.TrustMyself = s != null },

            { "msec|maxsessioncount=", $"maximum number of parallel sessions.\nDefault: {config.OpcUa.MaxSessionCount}", (int i) => config.OpcUa.MaxSessionCount = i },

            { "msuc|maxsubscriptioncount=", $"maximum number of subscriptions.\nDefault: {config.OpcUa.MaxSubscriptionCount}", (int i) => config.OpcUa.MaxSubscriptionCount = i },
            { "mqrc|maxqueuedrequestcount=", $"maximum number of requests that will be queued waiting for a thread.\nDefault: {config.OpcUa.MaxQueuedRequestCount}", (int i) => config.OpcUa.MaxQueuedRequestCount = i },

            // cert store options
            { "at|appcertstoretype=", $"the own application cert store type.\n(allowed values: Directory, X509Store, FlatDirectory)\nDefault: '{config.OpcUa.OpcOwnCertStoreType}'", (string s) => {
                    switch (s)
                    {
                        case CertificateStoreType.X509Store:
                            config.OpcUa.OpcOwnCertStoreType = CertificateStoreType.X509Store;
                            config.OpcUa.OpcOwnCertStorePath = config.OpcUa.OpcOwnCertX509StorePathDefault;
                            break;
                        case CertificateStoreType.Directory:
                            config.OpcUa.OpcOwnCertStoreType = CertificateStoreType.Directory;
                            config.OpcUa.OpcOwnCertStorePath = config.OpcUa.OpcOwnCertDirectoryStorePathDefault;
                            break;
                        case FlatDirectoryCertificateStore.StoreTypeName:
                            config.OpcUa.OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
                            config.OpcUa.OpcOwnCertStorePath = config.OpcUa.OpcOwnCertDirectoryStorePathDefault;
                            break;
                        default:
                            throw new OptionException();
                    }
                }
            },

            { "ap|appcertstorepath=", "the path where the own application cert should be stored.\nDefault (depends on store type):\n" +
                    $"X509Store: '{config.OpcUa.OpcOwnCertX509StorePathDefault}'\n" +
                    $"Directory: '{config.OpcUa.OpcOwnCertDirectoryStorePathDefault}'\n" +
                    $"FlatDirectory: '{config.OpcUa.OpcOwnCertDirectoryStorePathDefault}'",
                    (string s) => config.OpcUa.OpcOwnCertStorePath = s
            },

            { "tp|trustedcertstorepath=", $"the path of the trusted cert store.\nDefault '{config.OpcUa.OpcTrustedCertDirectoryStorePathDefault}'", (string s) => config.OpcUa.OpcTrustedCertStorePath = s },

            { "rp|rejectedcertstorepath=", $"the path of the rejected cert store.\nDefault '{config.OpcUa.OpcRejectedCertDirectoryStorePathDefault}'", (string s) => config.OpcUa.OpcRejectedCertStorePath = s },

            { "ip|issuercertstorepath=", $"the path of the trusted issuer cert store.\nDefault '{config.OpcUa.OpcIssuerCertDirectoryStorePathDefault}'", (string s) => config.OpcUa.OpcIssuerCertStorePath = s },

            { "csr", $"show data to create a certificate signing request.\nDefault '{config.OpcUa.ShowCreateSigningRequestInfo}'", (string s) => config.OpcUa.ShowCreateSigningRequestInfo = s != null },

            { "ab|applicationcertbase64=", "update/set this application's certificate with the certificate passed in as base64 string.", (string s) => config.OpcUa.NewCertificateBase64String = s },
            { "af|applicationcertfile=", "update/set this application's certificate with the specified file.", (string s) =>
                {
                    if (File.Exists(s))
                    {
                        config.OpcUa.NewCertificateFileName = s;
                    }
                    else
                    {
                        throw new OptionException("The file '{s}' does not exist.", "applicationcertfile");
                    }
                }
            },

            { "pb|privatekeybase64=", "initial provisioning of the application certificate (with a PEM or PFX format) requires a private key passed in as base64 string.", (string s) => config.OpcUa.PrivateKeyBase64String = s },
            { "pk|privatekeyfile=", "initial provisioning of the application certificate (with a PEM or PFX format) requires a private key passed in as file.", (string s) =>
                {
                    if (File.Exists(s))
                    {
                        config.OpcUa.PrivateKeyFileName = s;
                    }
                    else
                    {
                        throw new OptionException("The file '{s}' does not exist.", "privatekeyfile");
                    }
                }
            },

            { "cp|certpassword=", "the optional password for the PEM or PFX or the installed application certificate.", (string s) => config.OpcUa.CertificatePassword = s },

            { "tb|addtrustedcertbase64=", "adds the certificate to the application's trusted cert store passed in as base64 string (comma separated values).", (string s) => config.OpcUa.TrustedCertificateBase64Strings = ParseListOfStrings(s) },
            { "tf|addtrustedcertfile=", "adds the certificate file(s) to the application's trusted cert store passed in as base64 string (multiple comma separated filenames supported).", (string s) => config.OpcUa.TrustedCertificateFileNames = CliHelper.ParseListOfFileNames(s, "addtrustedcertfile") },

            { "ib|addissuercertbase64=", "adds the specified issuer certificate to the application's trusted issuer cert store passed in as base64 string (comma separated values).", (string s) => config.OpcUa.IssuerCertificateBase64Strings = ParseListOfStrings(s) },
            { "if|addissuercertfile=", "adds the specified issuer certificate file(s) to the application's trusted issuer cert store (multiple comma separated filenames supported).", (string s) => config.OpcUa.IssuerCertificateFileNames = CliHelper.ParseListOfFileNames(s, "addissuercertfile") },

            { "rb|updatecrlbase64=", "update the CRL passed in as base64 string to the corresponding cert store (trusted or trusted issuer).", (string s) => config.OpcUa.CrlBase64String = s },
            { "uc|updatecrlfile=", "update the CRL passed in as file to the corresponding cert store (trusted or trusted issuer).", (string s) =>
                {
                    if (File.Exists(s))
                    {
                        config.OpcUa.CrlFileName = s;
                    }
                    else
                    {
                        throw new OptionException("The file '{s}' does not exist.", "updatecrlfile");
                    }
                }
            },

            { "rc|removecert=", "remove cert(s) with the given thumbprint(s) (comma separated values).", (string s) => config.OpcUa.ThumbprintsToRemove = ParseListOfStrings(s)
            },

            {"daa|disableanonymousauth", $"flag to disable anonymous authentication.\nDefault: {config.DisableAnonymousAuth}", (string s) => config.DisableAnonymousAuth = s != null },
            {"dua|disableusernamepasswordauth", $"flag to disable username/password authentication.\nDefault: {config.DisableUsernamePasswordAuth}", (string s) => config.DisableUsernamePasswordAuth = s != null },
            {"dca|disablecertauth", $"flag to disable certificate authentication.\nDefault: {config.DisableCertAuth}", (string s) => config.DisableCertAuth = s != null },

            // user management
            { "au|adminuser=", $"the username of the admin user.\nDefault: {config.AdminUser}", (string s) => config.AdminUser = s ?? config.AdminUser},
            { "ac|adminpassword=", $"the password of the administrator.\nDefault: {config.AdminPassword}", (string s) => config.AdminPassword = s ?? config.AdminPassword},
            { "du|defaultuser=", $"the username of the default user.\nDefault: {config.DefaultUser}", (string s) => config.DefaultUser = s ?? config.DefaultUser},
            { "dc|defaultpassword=", $"the password of the default user.\nDefault: {config.DefaultPassword}", (string s) => config.DefaultPassword = s ?? config.DefaultPassword},

            // Special nodes
            { "alm|alarms", $"add alarm simulation to address space.\nDefault: {plcSimulation.AddAlarmSimulation}", (string s) => plcSimulation.AddAlarmSimulation = s != null },
            { "ses|simpleevents", $"add simple events simulation to address space.\nDefault: {plcSimulation.AddSimpleEventsSimulation}", (string s) => plcSimulation.AddSimpleEventsSimulation = s != null },
            { "dalm|deterministicalarms=", $"add deterministic alarm simulation to address space.\nProvide a script file for controlling deterministic alarms.", (string s) => plcSimulation.DeterministicAlarmSimulationFile = s },

            // misc
            { "sp|showpnjson", $"show OPC Publisher configuration file using IP address as EndpointUrl.\nDefault: {config.ShowPublisherConfigJsonIp}", (string s) => config.ShowPublisherConfigJsonIp = s != null },
            { "sph|showpnjsonph", $"show OPC Publisher configuration file using plchostname as EndpointUrl.\nDefault: {config.ShowPublisherConfigJsonPh}", (string s) => config.ShowPublisherConfigJsonPh = s != null },
            { "spf|showpnfname=", $"filename of the OPC Publisher configuration file to write when using options sp/sph.\nDefault: {config.PnJson}", (string s) => config.PnJson = s },
            { "wp|webport=", $"web server port for hosting OPC Publisher configuration file.\nDefault: {config.WebServerPort}", (uint i) => config.WebServerPort = i },
            { "cdn|certdnsnames=", "add additional DNS names or IP addresses to this application's certificate (comma separated values; no spaces allowed).\nDefault: DNS hostname", (string s) => config.OpcUa.DnsNames = ParseListOfStrings(s) },

            { "h|help", "show this message and exit", (string s) => config.ShowHelp = s != null },
        };

        // Add options from plugin nodes list.
        foreach (var plugin in pluginNodes)
        {
            plugin.AddOptions(_options);
        }

        // Parse the command line
        List<string> extraArgs = _options.Parse(args);

        return (config, plcSimulation, extraArgs);
    }

    /// <summary>
    /// Get usage help message.
    /// </summary>
    public static string GetUsageHelp(string programName)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine($"{programName} v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
        sb.AppendLine($"Informational version: v{(Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion}");
        sb.AppendLine();
        sb.AppendLine($"Usage: dotnet {Assembly.GetEntryAssembly().GetName().Name}.dll [<options>]");
        sb.AppendLine();
        sb.AppendLine("OPC UA PLC for different data simulation scenarios.");
        sb.AppendLine("To exit the application, press CTRL-C while it's running.");
        sb.AppendLine();
        sb.AppendLine("Use the following format to specify a list of strings:");
        sb.AppendLine("\"<string 1>,<string 2>,...,<string n>\"");
        sb.AppendLine("or if one string contains commas:");
        sb.AppendLine("\"\"<string 1>\",\"<string 2>\",...,\"<string n>\"\"");
        sb.AppendLine();

        // Append the options.
        sb.AppendLine("Options:");
        using var stringWriter = new StringWriter(sb);
        _options.WriteOptionDescriptions(stringWriter);

        return sb.ToString();
    }

    /// <summary>
    /// Helper to build a list of byte arrays out of a comma separated list of base64 strings (optional in double quotes).
    /// </summary>
    private static List<string> ParseListOfStrings(string list)
    {
        var strings = new List<string>();
        if (list[0] == '"' && (list.Count(c => c.Equals('"')) % 2 == 0))
        {
            while (list.Contains('"'))
            {
                int first = 0;
                int next = 0;
                first = list.IndexOf('"', next);
                next = list.IndexOf('"', ++first);
                strings.Add(list[first..next]);
                list = list.Substring(++next);
            }
        }
        else if (list.Contains(','))
        {
            strings = list.Split(',').ToList();
            strings = strings.ConvertAll(st => st.Trim());
            strings = strings.Select(st => st.Trim()).ToList();
        }
        else
        {
            strings.Add(list);
        }
        return strings;
    }
}
