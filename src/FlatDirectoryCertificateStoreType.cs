namespace OpcPlc.Certs;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

/// <summary>
/// Defines type for <see cref="FlatDirectoryCertificateStore"/>.
/// </summary>
public sealed class FlatDirectoryCertificateStoreType : ICertificateStoreType
{
    private readonly ILoggerFactory _loggerFactory;

    public FlatDirectoryCertificateStoreType(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc/>
    public ICertificateStore CreateStore(ITelemetryContext telemetry)
    {
        ILogger logger = _loggerFactory.CreateLogger<FlatDirectoryCertificateStore>();
        return new FlatDirectoryCertificateStore(logger, telemetry);
    }

    /// <inheritdoc/>
    public bool SupportsStorePath(string storePath)
    {
        return !string.IsNullOrEmpty(storePath) && storePath.StartsWith(FlatDirectoryCertificateStore.StoreTypePrefix);
    }
}
