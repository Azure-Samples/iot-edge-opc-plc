namespace OpcPlc.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Moq;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Configuration;
    using OpcPlc;
    using Serilog;

    /// <summary>
    /// A test fixture that starts a static singleton instance of the OPC PLC simulator.
    /// </summary>
    public class PlcSimulatorFixture
    {
        /// <summary>
        /// Port on which to run the simulator. Using a non-standard port so that
        /// developers can simultaneously run the simulator process.
        /// </summary>
        private const int Port = 50001;

        private readonly string[] _args;

        /// <summary>
        /// The writer in which output is immediately displayed in the NUnit console.
        /// </summary>
        private TextWriter _log;

        /// <summary>
        /// The mocked current time.
        /// </summary>
        private DateTime _now = DateTime.UtcNow;

        /// <summary>
        /// Registry of mocked timers.
        /// </summary>
        private readonly ConcurrentBag<(ITimer timer, ElapsedEventHandler handler)> _timers
            = new ConcurrentBag<(ITimer, ElapsedEventHandler)>();

        /// <summary>
        /// Registry of mocked fast timers.
        /// </summary>
        private readonly ConcurrentBag<(ITimer timer, FastTimerElapsedEventHandler handler)> _fastTimers
            = new ConcurrentBag<(ITimer, FastTimerElapsedEventHandler)>();

        private Task _serverTask;

        private readonly CancellationTokenSource _serverCancellationTokenSource = new CancellationTokenSource();

        private ApplicationConfiguration _config;

        private ConfiguredEndpoint _serverEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcSimulatorFixture"/> class.
        /// </summary>
        /// <param name="args">Command-line arguments to be passed to the simulator.</param>
        public PlcSimulatorFixture(string[] args)
        {
            _args = args ?? Array.Empty<string>();
        }

        /// <summary>
        /// Configure and run the simulator in a background thread, run once for the entire assembly.
        /// The simulator is instrumented with mock time services.
        /// </summary>
        public async Task Start()
        {
            Program.Ready = false;
            Program.Logger = new LoggerConfiguration()
                .WriteTo.NUnitOutput()
                .CreateLogger();
            _log = TestContext.Progress;

            var mock = new Mock<TimeService>();
            mock.Setup(f => f.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()))
                .Returns((ElapsedEventHandler handler, uint intervalInMilliseconds) =>
                {
                    var mockTimer = new Mock<ITimer>();
                    mockTimer.SetupAllProperties();
                    var timer = mockTimer.Object;
                    timer.Interval = intervalInMilliseconds;
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    _timers.Add((timer, handler));
                    return timer;
                });
            mock.Setup(f => f.NewFastTimer(It.IsAny<FastTimerElapsedEventHandler>(), It.IsAny<uint>()))
                .Returns((FastTimerElapsedEventHandler handler, uint intervalInMilliseconds) =>
                {
                    var mockTimer = new Mock<ITimer>();
                    mockTimer.SetupAllProperties();
                    var timer = mockTimer.Object;
                    timer.Interval = intervalInMilliseconds;
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    _fastTimers.Add((timer, handler));
                    return timer;
                });
            Program.TimeService = mock.Object;

            mock.Setup(f => f.Now())
                .Returns(() => _now);

            mock.Setup(f => f.UtcNow())
                .Returns(() => _now);

            // The simulator program command line.            
            _serverTask = Task.Run(() => Program.MainAsync(
                    _args.Concat(
                            new[]
                            {
                                "--autoaccept",
                                $"--portnum={Port}",
                            })
                        .ToArray(),
                    _serverCancellationTokenSource.Token)
                .GetAwaiter().GetResult());

            var endpointUrl = WaitForServerUp();
            await _log.WriteAsync($"Found server at: {endpointUrl}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // On Mac platforms (in particular in Azure DevOps builds),
                // the URL containing the machine hostname is sometimes not accessible.
                // Use the Loopback IP address as a workaround.
                // In contrast, on Windows Azure DevOps builds, this results in issues.
                endpointUrl = $"opc.tcp://{IPAddress.Loopback}:{Port}";
                await _log.WriteAsync($"Connecting to server URL: {endpointUrl}");
            }

            _config = await GetConfigurationAsync();
            _serverEndpoint = GetServerEndpoint(endpointUrl);
        }

        public Task Stop()
        {
            // shutdown simulator
            _serverCancellationTokenSource.Cancel();
            return _serverTask;
        }

        /// <summary>
        /// Create a OPC-UA Session.
        /// </summary>
        /// <param name="sessionName">The name to assign to the session.</param>
        /// <returns>The created session.</returns>
        public Task<Session> CreateSessionAsync(string sessionName)
        {
            _log.WriteLine("Create a session with OPC UA server.");
            var userIdentity = new UserIdentity(new AnonymousIdentityToken());
            return Session.Create(_config, _serverEndpoint, false, sessionName, 60000, userIdentity, null);
        }

        /// <summary>
        /// Cause a subset of the mocked timers to fire a number of times,
        /// and the current mocked time to advance accordingly.
        /// </summary>
        /// <param name="periodInMilliseconds">Defines the timers to fire: only timers with this interval are fired.</param>
        /// <param name="numberOfTimes">Number of times the timer should be fired.</param>
        public void FireTimersWithPeriod(uint periodInMilliseconds, int numberOfTimes)
        {
            var matchedHandlers = _timers.Where(t
                    => t.timer.Enabled
                       && CloseTo(t.timer.Interval, periodInMilliseconds))
                .Select(t => t.handler)
                .ToList();
            for (var i = 0; i < numberOfTimes; i++)
            {
                _now += TimeSpan.FromMilliseconds(periodInMilliseconds);
                foreach (var handler in matchedHandlers)
                {
                    handler(null, null);
                }
            }

            var matchedFastHandlers = _fastTimers.Where(t
                    => t.timer.Enabled
                       && CloseTo(t.timer.Interval, periodInMilliseconds))
                .Select(t => t.handler)
                .ToList();
            for (var i = 0; i < numberOfTimes; i++)
            {
                _now += TimeSpan.FromMilliseconds(periodInMilliseconds);
                foreach (var handler in matchedFastHandlers)
                {
                    handler(null, null);
                }
            }
        }

        private static bool CloseTo(double a, double b) => Math.Abs(a - b) <= Math.Abs(a * .00001);

        private async Task<ApplicationConfiguration> GetConfigurationAsync()
        {
            await _log.WriteLineAsync("Create an Application Configuration.");

            var application = new ApplicationInstance
            {
                ApplicationName = nameof(PlcSimulatorFixture),
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = nameof(PlcSimulatorFixture) // Defines name of *.Config.xml file read
            };

            // load the application configuration.
            var config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // check the application certificate.
            var haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0).ConfigureAwait(false);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            config.ApplicationUri = X509Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);

            // Auto-accept server certificate
            config.CertificateValidator.CertificateValidation += CertificateValidator_AutoAccept;

            return config;
        }

        private static void CertificateValidator_AutoAccept(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = true;
            }
        }

        /// <summary>
        /// Get the configuration information for a given endpoint URL.
        /// In some environments, this can fail for a while when a simulator has been recreated after
        /// a simulator has been shut down, for unclear reasons.
        /// Therefore, the method retries for up to 10 seconds in case of failure.
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private ConfiguredEndpoint GetServerEndpoint(string endpointUrl)
        {
            var sw = Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, false, 15000);
                    var endpointConfiguration = EndpointConfiguration.Create(_config);
                    return new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
                }
                catch (ServiceResultException) when (sw.Elapsed < TimeSpan.FromSeconds(10))
                {
                    _log.Write("Retrying to access endpoint...");
                    Thread.Sleep(100);
                }
            }
        }

        private string WaitForServerUp()
        {
            while (true)
            {
                if (_serverTask.IsFaulted)
                {
                    throw _serverTask.Exception!;
                }

                if (_serverTask.IsCompleted)
                {
                    throw new Exception("Server failed to start");
                }

                if (!Program.Ready)
                {
                    _log.WriteLine("Waiting for server to start...");
                    Thread.Sleep(1000);
                    continue;
                }

                return Program.PlcServer.GetEndpoints().First().EndpointUrl;
            }
        }
    }
}