namespace OpcPlc.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpcPlc;
using OpcPlc.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

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

    private readonly OpcPlcServer _opcPlcServer;

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
    private readonly ConcurrentBag<(OpcPlc.ITimer timer, ElapsedEventHandler handler)> _timers
        = new();

    /// <summary>
    /// Registry of mocked fast timers.
    /// </summary>
    private readonly ConcurrentBag<(OpcPlc.ITimer timer, FastTimerElapsedEventHandler handler)> _fastTimers
        = new();

    private Task _serverTask;

    private readonly CancellationTokenSource _serverCancellationTokenSource = new();

    private ApplicationConfiguration _config;

    private ConfiguredEndpoint _serverEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlcSimulatorFixture"/> class.
    /// </summary>
    /// <param name="args">Command-line arguments to be passed to the simulator.</param>
    public PlcSimulatorFixture(string[] args)
    {
        _args = args ?? Array.Empty<string>();
        _opcPlcServer = new OpcPlcServer();
    }

    /// <summary>
    /// Configure and run the simulator in a background thread, run once for the entire assembly.
    /// The simulator is instrumented with mock time services.
    /// </summary>
    public async Task StartAsync()
    {
        Reset();

        _opcPlcServer.LoggerFactory = LoggingProvider.CreateDefaultLoggerFactory(LogLevel.Information);
        _opcPlcServer.Logger = new TestLogger<PlcSimulatorFixture>(TestContext.Progress, new SyslogFormatter(new SyslogFormatterOptions()));

        _log = TestContext.Progress;

        var mock = new Mock<TimeService>();
        mock.Setup(f => f.NewTimer(It.IsAny<ElapsedEventHandler>(), It.IsAny<uint>()))
            .Returns((ElapsedEventHandler handler, uint intervalInMilliseconds) => {
                var mockTimer = new Mock<OpcPlc.ITimer>();
                mockTimer.SetupAllProperties();
                var timer = mockTimer.Object;
                timer.Interval = intervalInMilliseconds;
                timer.AutoReset = true;
                timer.Enabled = true;
                _timers.Add((timer, handler));
                return timer;
            });

        mock.Setup(f => f.NewFastTimer(It.IsAny<FastTimerElapsedEventHandler>(), It.IsAny<uint>()))
            .Returns((FastTimerElapsedEventHandler handler, uint intervalInMilliseconds) => {
                var mockTimer = new Mock<OpcPlc.ITimer>();
                mockTimer.SetupAllProperties();
                var timer = mockTimer.Object;
                timer.Interval = intervalInMilliseconds;
                timer.AutoReset = true;
                timer.Enabled = true;
                _fastTimers.Add((timer, handler));
                return timer;
            });

        _opcPlcServer.TimeService = mock.Object;

        mock.Setup(f => f.Now())
            .Returns(() => _now);

        mock.Setup(f => f.UtcNow())
            .Returns(() => _now);

        // The simulator program command line.
        // Passed args override the following defaults.
        _serverTask = Task.Run(async () => await _opcPlcServer.StartAsync(
            _args.Concat(
                new[]
                {
                    "--autoaccept",
                    $"--portnum={Port}",
                    "--fn=25",
                    "--fr=1",
                    "--ft=uint",
                }).ToArray(),
            _serverCancellationTokenSource.Token)
            .ConfigureAwait(false));

        string endpointUrl = await WaitForServerUpAsync().ConfigureAwait(false);
        await _log.WriteAsync($"Found server at: {endpointUrl}").ConfigureAwait(false);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On Mac platforms (in particular in Azure DevOps builds),
            // the URL containing the machine hostname is sometimes not accessible.
            // Use the Loopback IP address as a workaround.
            // In contrast, on Windows Azure DevOps builds, this results in issues.
            endpointUrl = $"opc.tcp://{IPAddress.Loopback}:{Port}";
            await _log.WriteAsync($"Connecting to server URL: {endpointUrl}").ConfigureAwait(false);
        }

        _config = await GetConfigurationAsync().ConfigureAwait(false);
        _serverEndpoint = await GetServerEndpointAsync(endpointUrl).ConfigureAwait(false);
    }

    public Task StopAsync()
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
    public async Task<Session> CreateSessionAsync(string sessionName)
    {
        await _log.WriteLineAsync("Create a session with OPC UA server ...").ConfigureAwait(false);
        var userIdentity = new UserIdentity(new AnonymousIdentityToken());

        // When unit test certificate expires,
        // remove the pki folder from \tests\bin\<CONFIG>\<ARCH>
        return await Session.Create(
            _config,
            reverseConnectManager: null,
            _serverEndpoint,
            updateBeforeConnect: false,
            checkDomain: false,
            sessionName,
            sessionTimeout: 60000,
            userIdentity,
            preferredLocales: null).ConfigureAwait(false);
    }

    /// <summary>
    /// Cause a subset of the mocked timers to fire a number of times,
    /// and the current mocked time to advance accordingly.
    /// </summary>
    /// <param name="periodInMilliseconds">Defines the timers to fire: only timers with this interval are fired.</param>
    /// <param name="numberOfTimes">Number of times the timer should be fired.</param>
    public void FireTimersWithPeriod(uint periodInMilliseconds, int numberOfTimes)
    {
        var matchedHandlers = GetTimerHandlersForPeriod(periodInMilliseconds);
        matchedHandlers.Should().NotBeEmpty("expected Timer(s) to be setup with interval {0} ms", periodInMilliseconds);

        for (var i = 0; i < numberOfTimes; i++)
        {
            _now += TimeSpan.FromMilliseconds(periodInMilliseconds);
            foreach (var handler in matchedHandlers)
            {
                handler();
            }
        }
    }

    public List<Action> GetTimerHandlersForPeriod(uint periodInMilliseconds)
    {
        var matchedTimers = _timers.Where(t
                => t.timer.Enabled && CloseTo(t.timer.Interval, periodInMilliseconds))
            .Select(t => (Action)(() => t.handler(null, null)))
            .ToList();

        var matchedFastTimers = _fastTimers.Where(t
                => t.timer.Enabled && CloseTo(t.timer.Interval, periodInMilliseconds))
            .Select(t => (Action)(() => t.handler(null, null)))
            .ToList();

        var matchedHandlers = matchedTimers.Union(matchedFastTimers).ToList();
        return matchedHandlers;
    }

    private void Reset()
    {
        _opcPlcServer.Ready = false;

        if (_opcPlcServer.PlcSimulationInstance is not null)
        {
            _opcPlcServer.PlcSimulationInstance.AddAlarmSimulation = false;
            _opcPlcServer.PlcSimulationInstance.DeterministicAlarmSimulationFile = null;
        }
    }

    private static bool CloseTo(double a, double b) => Math.Abs(a - b) <= Math.Abs(a * .00001);

    private async Task<ApplicationConfiguration> GetConfigurationAsync()
    {
        await _log.WriteLineAsync("Create Application Configuration").ConfigureAwait(false);

        var application = new ApplicationInstance {
            ApplicationName = nameof(PlcSimulatorFixture),
            ApplicationType = ApplicationType.Client,
            ConfigSectionName = nameof(PlcSimulatorFixture) // Defines name of *.Config.xml file read
        };

        // load the application configuration.
        var config = await application.LoadApplicationConfiguration(silent: false).ConfigureAwait(false);

        // check the application certificate.
        bool haveAppCertificate = await application.CheckApplicationInstanceCertificates(silent: false).ConfigureAwait(false);
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
    /// <exception cref="Exception"></exception>
    private async Task<ConfiguredEndpoint> GetServerEndpointAsync(string endpointUrl)
    {
        var sw = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                var endpoint = CoreClientUtils.SelectEndpoint(_config, endpointUrl, useSecurity: false, discoverTimeout: 15000);

                var endpointConfiguration = EndpointConfiguration.Create(_config);
                return new ConfiguredEndpoint(collection: null, endpoint, endpointConfiguration);
            }
            catch (ServiceResultException) when (sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                await _log.WriteLineAsync("Retrying to access endpoint...").ConfigureAwait(false);
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }

    private async Task<string> WaitForServerUpAsync()
    {
        while (true)
        {
            if (_serverTask.IsFaulted)
            {
                throw _serverTask.Exception!;
            }

            if (_serverTask.IsCompleted)
            {
                throw new Exception("The OPC PLC server failed to start.");
            }

            if (!_opcPlcServer.Ready)
            {
                await _log.WriteLineAsync("Waiting for server to start ...").ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                continue;
            }

            return _opcPlcServer.PlcServer.GetEndpoints()[0].EndpointUrl;
        }
    }
}
