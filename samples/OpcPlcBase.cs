namespace UnitTests;

using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpcPlc;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for tests that use the OPC PLC server NuGet.
/// </summary>
[NonParallelizable]
public class OpcPlcBase
{
    private readonly string[] _args;
    private readonly int _port;
    private readonly string? _endpointUriOverride;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ITelemetryContext _telemetry = DefaultTelemetry.Create(_ => { });
    private readonly string _pkiRoot = Path.Combine(Path.GetTempPath(), "opcplc-sample-" + Guid.NewGuid().ToString("N"));
    private OpcPlcServer? _opcPlcServer;
    private ApplicationInstance? _clientApplication;
    private Task? _serverTask;
    private string? _originalDirectory;
    private bool _stopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcPlcBase"/> class.
    /// Set the <paramref name="endpointUriOverride"/> to override spawning a server and use an existing one instead.
    /// </summary>
    public OpcPlcBase(string[] args, int port = 51234, string? endpointUriOverride = null)
    {
        _args = args;
        _port = port;
        _endpointUriOverride = endpointUriOverride;
    }

    /// <summary>
    /// Gets the OPC PLC server endpoint URL.
    /// </summary>
    public string OpcPlcEndpointUrl { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _originalDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = AppContext.BaseDirectory;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            if (!string.IsNullOrEmpty(_endpointUriOverride))
            {
                OpcPlcEndpointUrl = _endpointUriOverride;
            }
            else
            {
                int port = _port == 0 ? GetFreePort() : _port;
                _opcPlcServer = new OpcPlcServer();
                string[] arguments = [
                    .. _args, "--autoaccept", $"--portnum={port}",
                    $"--ap={Path.Combine(_pkiRoot, "server", "own")}",
                    $"--tp={Path.Combine(_pkiRoot, "server", "trusted")}",
                    $"--ip={Path.Combine(_pkiRoot, "server", "issuer")}",
                    $"--rp={Path.Combine(_pkiRoot, "server", "rejected")}",
                    $"--tup={Path.Combine(_pkiRoot, "server", "trusted-user")}",
                    $"--uip={Path.Combine(_pkiRoot, "server", "issuer-user")}"];
                _serverTask = Task.Run(() => _opcPlcServer.StartAsync(arguments, _shutdown.Token));
                while (!_opcPlcServer.Ready)
                {
                    if (_serverTask.IsCompleted)
                    {
                        await _serverTask.ConfigureAwait(false);
                        throw new InvalidOperationException("The packaged PLC exited before startup completed.");
                    }
                    await Task.Delay(100, deadline.Token).ConfigureAwait(false);
                }
                OpcPlcEndpointUrl = _opcPlcServer.PlcServer.GetEndpoints()[0].EndpointUrl
                    ?? throw new InvalidOperationException("The packaged PLC did not advertise an endpoint URL.");
            }

            string clientRoot = Path.Combine(_pkiRoot, "client");
            _clientApplication = new ApplicationInstance(_telemetry)
            {
                ApplicationName = "OpcPlcSampleClient",
                ApplicationType = ApplicationType.Client
            };
            var configuration = await _clientApplication
                .Build($"urn:{Utils.GetHostName()}:OpcPlcSampleClient", "urn:OpcPlc:Sample")
                .AsClient()
                .AddSecurityConfiguration([new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(clientRoot, "own"),
                    SubjectName = "CN=OpcPlcSampleClient",
                    CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                }], clientRoot)
                .CreateAsync().ConfigureAwait(false);
            bool valid = await _clientApplication.CheckApplicationInstanceCertificatesAsync(
                silent: true, ct: deadline.Token).ConfigureAwait(false);
            if (!valid)
            {
                throw new InvalidOperationException("The sample client certificate is invalid.");
            }
            configuration.CertificateManager.AcceptError =
                (_, error) => error.StatusCode == StatusCodes.BadCertificateUntrusted;
        }
        catch
        {
            await StopAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected async Task<ManagedSession> GetConnectedClientAsync(CancellationToken cancellationToken)
    {
        var configuration = _clientApplication?.ApplicationConfiguration
            ?? throw new InvalidOperationException("The sample fixture has not started.");
        var endpoint = await CoreClientUtils.SelectEndpointAsync(configuration, OpcPlcEndpointUrl,
            useSecurity: true, discoverTimeout: 15000, telemetry: _telemetry, cancellationToken).ConfigureAwait(false);
        if (endpoint is null)
        {
            throw new InvalidOperationException("The packaged PLC did not advertise a secure endpoint.");
        }
        return await ManagedSession.CreateAsync(configuration,
            new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(configuration)),
            new DefaultSessionFactory(_telemetry), telemetry: _telemetry, checkDomain: true,
            ct: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restarts the OPC PLC server with the same configuration.
    /// </summary>
    public Task RestartOpcPlcServerAsync()
    {
        if (_opcPlcServer is null)
        {
            throw new InvalidOperationException("Cannot restart OPC PLC server when the endpoint URL is overridden.");
        }

        return _opcPlcServer.RestartAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (_stopped)
        {
            return;
        }
        _stopped = true;
        try
        {
            if (_clientApplication is not null)
            {
                await _clientApplication.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                await _shutdown.CancelAsync().ConfigureAwait(false);
                if (_serverTask is not null)
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                }
            }
            finally
            {
                _shutdown.Dispose();
                if (_originalDirectory is not null)
                {
                    Environment.CurrentDirectory = _originalDirectory;
                }
                if (Directory.Exists(_pkiRoot))
                {
                    Directory.Delete(_pkiRoot, recursive: true);
                }
            }
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
