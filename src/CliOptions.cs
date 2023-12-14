namespace OpcPlc;

using Microsoft.Extensions.Logging;
using Mono.Options;
using Opc.Ua;
using OpcPlc.Certs;
using OpcPlc.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static OpcPlc.OpcApplicationConfiguration;
using static OpcPlc.PlcSimulation;

public static class CliOptions
{
    public static Mono.Options.OptionSet InitCommandLineOptions()
    {
        var options = new Mono.Options.OptionSet {
            // log configuration
            { "lf|logfile=", $"the filename of the logfile to use.\nDefault: './{Program.Config.LogFileName}'", (string s) => Program.Config.LogFileName = s },
            { "lt|logflushtimespan=", $"the timespan in seconds when the logfile should be flushed.\nDefault: {Program.Config.LogFileFlushTimeSpanSec} sec", (int i) => {
                    if (i > 0)
                    {
                        Program.Config.LogFileFlushTimeSpanSec = TimeSpan.FromSeconds(i);
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
                        Program.Config.LogLevelCli = s.ToLowerInvariant();
                    }
                    else
                    {
                        throw new OptionException($"The loglevel must be one of: {string.Join(", ", logLevels)}", "loglevel");
                    }
                }
            },

            // simulation configuration
            { "sc|simulationcyclecount=", $"count of cycles in one simulation phase.\nDefault:  {SimulationCycleCount} cycles", (int i) => SimulationCycleCount = i },
            { "ct|cycletime=", $"length of one cycle time in milliseconds.\nDefault:  {SimulationCycleLength} msec", (int i) => SimulationCycleLength = i },

            // events
            { "ei|eventinstances=", $"number of event instances.\nDefault: {EventInstanceCount}", (uint i) => EventInstanceCount = i },
            { "er|eventrate=", $"rate in milliseconds to send events.\nDefault: {EventInstanceRate}", (uint i) => EventInstanceRate = i },

            // OPC configuration
            { "pn|portnum=", $"the server port of the OPC server endpoint.\nDefault: {ServerPort}", (ushort i) => ServerPort = i },
            { "op|path=", $"the endpoint URL path part of the OPC server endpoint.\nDefault: '{ServerPath}'", (string s) => ServerPath = s },
            { "ph|plchostname=", $"the fully-qualified hostname of the PLC.\nDefault: {Hostname}", (string s) => Hostname = s },
            { "ol|opcmaxstringlen=", $"the max length of a string OPC can transmit/receive.\nDefault: {OpcMaxStringLength}", (int i) => {
                    if (i > 0)
                    {
                        OpcMaxStringLength = i;
                    }
                    else
                    {
                        throw new OptionException("The max OPC string length must be larger than 0.", "opcmaxstringlen");
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
            { "aa|autoaccept", $"all certs are trusted when a connection is established.\nDefault: {AutoAcceptCerts}", (string s) => AutoAcceptCerts = s != null },

            { "drurs|dontrejectunknownrevocationstatus", $"Don't reject chain validation with CA certs with unknown revocation status, e.g. when the CRL is not available or the OCSP provider is offline.\nDefault: {DontRejectUnknownRevocationStatus}", (string s) => DontRejectUnknownRevocationStatus = s != null },

            { "ut|unsecuretransport", $"enables the unsecured transport.\nDefault: {EnableUnsecureTransport}", (string s) => EnableUnsecureTransport = s != null },

            { "to|trustowncert", $"the own certificate is put into the trusted certificate store automatically.\nDefault: {TrustMyself}", (string s) => TrustMyself = s != null },

            { "msec|maxsessioncount=", $"maximum number of parallel sessions.\nDefault: {MaxSessionCount}", (int i) => MaxSessionCount = i },

            { "msuc|maxsubscriptioncount=", $"maximum number of subscriptions.\nDefault: {MaxSubscriptionCount}", (int i) => MaxSubscriptionCount = i },
            { "mqrc|maxqueuedrequestcount=", $"maximum number of requests that will be queued waiting for a thread.\nDefault: {MaxQueuedRequestCount}", (int i) => MaxQueuedRequestCount = i },

            // cert store options
            { "at|appcertstoretype=", $"the own application cert store type. \n(allowed values: Directory, X509Store)\nDefault: '{OpcOwnCertStoreType}'", (string s) => {
                    switch (s)
                    {
                        case CertificateStoreType.X509Store:
                            OpcOwnCertStoreType = CertificateStoreType.X509Store;
                            OpcOwnCertStorePath = OpcOwnCertX509StorePathDefault;
                            break;
                        case CertificateStoreType.Directory:
                            OpcOwnCertStoreType = CertificateStoreType.Directory;
                            OpcOwnCertStorePath = OpcOwnCertDirectoryStorePathDefault;
                            break;
                        case FlatDirectoryCertificateStore.StoreTypeName:
                            OpcOwnCertStoreType = FlatDirectoryCertificateStore.StoreTypeName;
                            OpcOwnCertStorePath = OpcOwnCertDirectoryStorePathDefault;
                            break;
                        default:
                            throw new OptionException();
                    }
                }
            },

            { "ap|appcertstorepath=", "the path where the own application cert should be stored\nDefault (depends on store type):\n" +
                    $"X509Store: '{OpcOwnCertX509StorePathDefault}'\n" +
                    $"Directory: '{OpcOwnCertDirectoryStorePathDefault}'" +
                    $"FlatDirectory: '{OpcOwnCertDirectoryStorePathDefault}'",
                    (string s) => OpcOwnCertStorePath = s
            },

            { "tp|trustedcertstorepath=", $"the path of the trusted cert store.\nDefault '{OpcTrustedCertDirectoryStorePathDefault}'", (string s) => OpcTrustedCertStorePath = s },

            { "rp|rejectedcertstorepath=", $"the path of the rejected cert store.\nDefault '{OpcRejectedCertDirectoryStorePathDefault}'", (string s) => OpcRejectedCertStorePath = s },

            { "ip|issuercertstorepath=", $"the path of the trusted issuer cert store.\nDefault '{OpcIssuerCertDirectoryStorePathDefault}'", (string s) => OpcIssuerCertStorePath = s },

            { "csr", $"show data to create a certificate signing request.\nDefault '{ShowCreateSigningRequestInfo}'", (string s) => ShowCreateSigningRequestInfo = s != null },

            { "ab|applicationcertbase64=", "update/set this application's certificate with the certificate passed in as base64 string.", (string s) => NewCertificateBase64String = s },
            { "af|applicationcertfile=", "update/set this application's certificate with the specified file.", (string s) =>
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

            { "pb|privatekeybase64=", "initial provisioning of the application certificate (with a PEM or PFX format) requires a private key passed in as base64 string.", (string s) => PrivateKeyBase64String = s },
            { "pk|privatekeyfile=", "initial provisioning of the application certificate (with a PEM or PFX format) requires a private key passed in as file.", (string s) =>
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

            { "cp|certpassword=", "the optional password for the PEM or PFX or the installed application certificate.", (string s) => CertificatePassword = s },

            { "tb|addtrustedcertbase64=", "adds the certificate to the application's trusted cert store passed in as base64 string (comma separated values).", (string s) => TrustedCertificateBase64Strings = ParseListOfStrings(s) },
            { "tf|addtrustedcertfile=", "adds the certificate file(s) to the application's trusted cert store passed in as base64 string (multiple comma separated filenames supported).", (string s) => TrustedCertificateFileNames = CliHelper.ParseListOfFileNames(s, "addtrustedcertfile") },

            { "ib|addissuercertbase64=", "adds the specified issuer certificate to the application's trusted issuer cert store passed in as base64 string (comma separated values).", (string s) => IssuerCertificateBase64Strings = ParseListOfStrings(s) },
            { "if|addissuercertfile=", "adds the specified issuer certificate file(s) to the application's trusted issuer cert store (multiple comma separated filenames supported).", (string s) => IssuerCertificateFileNames = CliHelper.ParseListOfFileNames(s, "addissuercertfile") },

            { "rb|updatecrlbase64=", "update the CRL passed in as base64 string to the corresponding cert store (trusted or trusted issuer).", (string s) => CrlBase64String = s },
            { "uc|updatecrlfile=", "update the CRL passed in as file to the corresponding cert store (trusted or trusted issuer).", (string s) =>
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

            { "rc|removecert=", "remove cert(s) with the given thumbprint(s) (comma separated values).", (string s) => ThumbprintsToRemove = ParseListOfStrings(s)
            },

            {"daa|disableanonymousauth", $"flag to disable anonymous authentication.\nDefault: {Program.Config.DisableAnonymousAuth}", (string s) => Program.Config.DisableAnonymousAuth = s != null },
            {"dua|disableusernamepasswordauth", $"flag to disable username/password authentication.\nDefault: {Program.Config.DisableUsernamePasswordAuth}", (string s) => Program.Config.DisableUsernamePasswordAuth = s != null },
            {"dca|disablecertauth", $"flag to disable certificate authentication.\nDefault: {Program.Config.DisableCertAuth}", (string s) => Program.Config.DisableCertAuth = s != null },

            // user management
            { "au|adminuser=", $"the username of the admin user.\nDefault: {Program.Config.AdminUser}", (string s) => Program.Config.AdminUser = s ?? Program.Config.AdminUser},
            { "ac|adminpassword=", $"the password of the administrator.\nDefault: {Program.Config.AdminPassword}", (string s) => Program.Config.AdminPassword = s ?? Program.Config.AdminPassword},
            { "du|defaultuser=", $"the username of the default user.\nDefault: {Program.Config.DefaultUser}", (string s) => Program.Config.DefaultUser = s ?? Program.Config.DefaultUser},
            { "dc|defaultpassword=", $"the password of the default user.\nDefault: {Program.Config.DefaultPassword}", (string s) => Program.Config.DefaultPassword = s ?? Program.Config.DefaultPassword},

            // Special nodes
            { "alm|alarms", $"add alarm simulation to address space.\nDefault: {AddAlarmSimulation}", (string s) => AddAlarmSimulation = s != null },
            { "ses|simpleevents", $"add simple events simulation to address space.\nDefault: {AddSimpleEventsSimulation}", (string s) => AddSimpleEventsSimulation = s != null },
            { "dalm|deterministicalarms=", $"add deterministic alarm simulation to address space.\nProvide a script file for controlling deterministic alarms.", (string s) => DeterministicAlarmSimulationFile = s },

            // misc
            { "sp|showpnjson", $"show OPC Publisher configuration file using IP address as EndpointUrl.\nDefault: {Program.Config.ShowPublisherConfigJsonIp}", (string s) => Program.Config.ShowPublisherConfigJsonIp = s != null },
            { "sph|showpnjsonph", $"show OPC Publisher configuration file using plchostname as EndpointUrl.\nDefault: {Program.Config.ShowPublisherConfigJsonPh}", (string s) => Program.Config.ShowPublisherConfigJsonPh = s != null },
            { "spf|showpnfname=", $"filename of the OPC Publisher configuration file to write when using options sp/sph.\nDefault: {Program.Config.PnJson}", (string s) => Program.Config.PnJson = s },
            { "wp|webport=", $"web server port for hosting OPC Publisher configuration file.\nDefault: {Program.Config.WebServerPort}", (uint i) => Program.Config.WebServerPort = i },
            { "cdn|certdnsnames=", "add additional DNS names or IP addresses to this application's certificate (comma separated values; no spaces allowed).\nDefault: DNS hostname", (string s) => DnsNames = ParseListOfStrings(s) },

            { "h|help", "show this message and exit", (string s) => Program.Config.ShowHelp = s != null },
        };

        // Add options from plugin nodes list.
        foreach (var plugin in Program.PluginNodes)
        {
            plugin.AddOptions(options);
        }

        return options;
    }

    /// <summary>
    /// Usage message.
    /// </summary>
    public static void PrintUsage(Mono.Options.OptionSet options)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine($"{Program.Config.ProgramName} v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}");
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
        options.WriteOptionDescriptions(stringWriter);

        Program.Logger.LogInformation(sb.ToString());
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
