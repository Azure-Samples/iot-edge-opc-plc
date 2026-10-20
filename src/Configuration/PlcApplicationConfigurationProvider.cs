// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class PlcApplicationConfigurationProvider : IOpcUaApplicationConfigurationProvider
{
    private readonly OpcUaAppConfigFactory _factory;
    private readonly bool _deferCertificateCompletion;
    private readonly object _lock = new();
    private readonly CancellationTokenSource _configurationCancellation = new();
    private Task<ApplicationConfiguration> _creation;
    private Task _completion;
    private Task _disposal;

    public PlcApplicationConfigurationProvider(OpcUaAppConfigFactory factory)
        : this(factory, deferCertificateCompletion: false)
    {
    }

    internal PlcApplicationConfigurationProvider(OpcUaAppConfigFactory factory, bool deferCertificateCompletion)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _deferCertificateCompletion = deferCertificateCompletion;
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
            _creation ??= _factory.ConfigureAsync(
                Application, _configurationCancellation.Token, _deferCertificateCompletion);
            return _creation.WaitAsync(ct);
        }
    }

    internal Task CompleteCertificateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposal is not null, this);
            if (!_deferCertificateCompletion || _creation is null)
            {
                throw new InvalidOperationException("Hosted configuration must be requested before completion.");
            }
            return _completion ??= CompleteCoreAsync(cancellationToken);
        }
    }

    private async Task CompleteCoreAsync(CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _configurationCancellation.Token);
        await _creation.WaitAsync(cancellation.Token).ConfigureAwait(false);
        await _factory.CompleteCertificateConfigurationAsync(
            Application, certificateAlreadyChecked: true, cancellation.Token).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            _disposal ??= DisposeCoreAsync(_creation, _completion);
            return new ValueTask(_disposal);
        }
    }

    private async Task DisposeCoreAsync(Task creation, Task completion)
    {
        try
        {
            await _configurationCancellation.CancelAsync().ConfigureAwait(false);
            if (creation is not null)
            {
                await Task.WhenAny(creation).ConfigureAwait(false);
            }
            if (completion is not null)
            {
                await Task.WhenAny(completion).ConfigureAwait(false);
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
