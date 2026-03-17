namespace OpcPlc.Certs;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using System;

/// <summary>
/// Defines type for <see cref="KubernetesSecretCertificateStore"/>.
/// </summary>
public sealed class KubernetesSecretCertificateStoreType : ICertificateStoreType
{
    private readonly IKubernetesSecretStoreClientFactory _kubernetesSecretStoreClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _secretNamespace;

    public KubernetesSecretCertificateStoreType(ILoggerFactory loggerFactory, IKubernetesSecretStoreClientFactory kubernetesSecretStoreClientFactory, string secretNamespace)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _kubernetesSecretStoreClientFactory = kubernetesSecretStoreClientFactory ?? throw new ArgumentNullException(nameof(kubernetesSecretStoreClientFactory));
        _secretNamespace = secretNamespace;
    }

    /// <inheritdoc/>
    public ICertificateStore CreateStore(ITelemetryContext telemetry)
    {
        ILogger logger = _loggerFactory.CreateLogger<KubernetesSecretCertificateStore>();
        return new KubernetesSecretCertificateStore(logger, _kubernetesSecretStoreClientFactory.Create(), _secretNamespace);
    }

    /// <inheritdoc/>
    public bool SupportsStorePath(string storePath)
    {
        return !string.IsNullOrEmpty(storePath) && storePath.StartsWith(KubernetesSecretCertificateStore.StoreTypePrefix, StringComparison.Ordinal);
    }
}
