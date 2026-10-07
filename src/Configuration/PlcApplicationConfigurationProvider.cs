namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class PlcApplicationConfigurationProvider : IOpcUaApplicationConfigurationProvider
{
    private readonly OpcUaAppConfigFactory _factory;
    private readonly object _lock = new();
    private readonly CancellationTokenSource _configurationCancellation = new();
    private Task<ApplicationConfiguration> _creation;
    private Task _disposal;

    public PlcApplicationConfigurationProvider(OpcUaAppConfigFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Application = factory.CreateApplication();
    }

    public IApplicationInstance Application { get; }

    public ApplicationConfiguration Configuration => Application.ApplicationConfiguration;

    public Task<ApplicationConfiguration> GetAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposal is not null, this);
            _creation ??= _factory.ConfigureAsync(Application, _configurationCancellation.Token);
            return _creation.WaitAsync(ct);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            _disposal ??= DisposeCoreAsync(_creation);
            return new ValueTask(_disposal);
        }
    }

    private async Task DisposeCoreAsync(Task creation)
    {
        try
        {
            await _configurationCancellation.CancelAsync().ConfigureAwait(false);
            if (creation is not null)
            {
                await Task.WhenAny(creation).ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                await Application.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _configurationCancellation.Dispose();
            }
        }
    }
}
