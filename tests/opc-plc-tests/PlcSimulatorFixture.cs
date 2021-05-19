namespace OpcPlc.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
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
    [SetUpFixture]
    public class PlcSimulatorFixture
    {
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

        private Task _serverTask;

        private ApplicationConfiguration _config;

        private ConfiguredEndpoint _serverEndpoint;

        // The global singleton fixture instance.
        public static PlcSimulatorFixture Instance { get; private set; }

        /// <summary>
        /// Configure and run the simulator in a background thread, run once for the entire assembly.
        /// The simulator is instrumented with mock time services.
        /// </summary>
        [OneTimeSetUp]
        public async Task RunBeforeAnyTests()
        {
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
            PlcSimulation.TimeService = mock.Object;

            mock.Setup(f => f.Now())
                .Returns(() => _now);

            mock.Setup(f => f.UtcNow())
                .Returns(() => _now);

            // The simulator program command line.
            // Currently, we do not support multiple instances of PLC server in test framework hence we are limited to use mutually exclusive cmd line parameters
            // e.g. we are using --str=true which we use for slow nodes random values test, fast nodes are using sequential value increment.
            _serverTask = Task.Run(() => Program.MainAsync(new[] { "--autoaccept", "--simpleevents", "--alm", "--ref", "--str=true", "--sr=2" }).GetAwaiter().GetResult());
            
            var endpointUrl = WaitForServerUp();
            await _log.WriteAsync($"Found server at {endpointUrl}");
            _config = await GetConfigurationAsync();
            var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, false, 15000);
            _serverEndpoint = GetServerEndpoint(endpoint, _config);
            Instance = this;
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            // TODO shutdown simulator
            _serverTask = null;
            _config = null;
            _serverEndpoint = null;
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
        }

        private static bool CloseTo(double a, double b) => Math.Abs(a - b) <= Math.Abs(a * .00001);

        private async Task<ApplicationConfiguration> GetConfigurationAsync()
        {
            await _log.WriteLineAsync("Create an Application Configuration.");

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = nameof(PlcSimulatorFixture),
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = nameof(PlcSimulatorFixture) // Defines name of *.Config.xml file read
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0).ConfigureAwait(false);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            // Note for future OpcUa update: Utils is renamed X509Utils in later versions
            config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);

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

        private ConfiguredEndpoint GetServerEndpoint(EndpointDescription endpoint, ApplicationConfiguration config)
        {
            var endpointConfiguration = EndpointConfiguration.Create(config);
            return new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
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