namespace OpcPlc.Certs;

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public interface IKubernetesSecretStoreClient
{
    Task<IReadOnlyDictionary<string, byte[]>> ReadAsync(string namespaceName, string secretName, CancellationToken ct = default);

    Task WriteAsync(string namespaceName, string secretName, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct = default);
}

public interface IKubernetesSecretStoreClientFactory
{
    IKubernetesSecretStoreClient Create();
}

internal static partial class KubernetesSecretStorePath
{
    private const string DefaultNamespace = "default";
    private const string ServiceAccountNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";

    public static string NormalizeSecretName(string rawStorePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawStorePath);

        var normalized = InvalidSecretNameChars().Replace(rawStorePath.ToLowerInvariant(), "-")
            .Replace("--", "-", StringComparison.Ordinal);

        normalized = normalized.Replace('\\', '-').Replace('/', '-').Trim('-', '.');

        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("The configured Kubernetes secret store path does not produce a valid secret name.", nameof(rawStorePath));
        }

        if (!char.IsLetterOrDigit(normalized[0]) || !char.IsLetterOrDigit(normalized[^1]))
        {
            throw new ArgumentException($"The configured Kubernetes secret store path '{rawStorePath}' does not map to a valid Kubernetes secret name.", nameof(rawStorePath));
        }

        return normalized;
    }

    public static string ResolveNamespace(string configuredNamespace)
    {
        if (!string.IsNullOrWhiteSpace(configuredNamespace))
        {
            return configuredNamespace;
        }

        var environmentNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE");
        if (!string.IsNullOrWhiteSpace(environmentNamespace))
        {
            return environmentNamespace;
        }

        if (File.Exists(ServiceAccountNamespaceFile))
        {
            var serviceAccountNamespace = File.ReadAllText(ServiceAccountNamespaceFile).Trim();
            if (!string.IsNullOrWhiteSpace(serviceAccountNamespace))
            {
                return serviceAccountNamespace;
            }
        }

        return DefaultNamespace;
    }

    [GeneratedRegex("[^a-z0-9.-]+")]
    private static partial Regex InvalidSecretNameChars();
}

public sealed class KubernetesSecretStoreClientFactory(string kubeConfigFilePath, ILogger logger) : IKubernetesSecretStoreClientFactory
{
    private readonly Lazy<IKubernetesSecretStoreClient> _client = new(
        () => new KubernetesSecretStoreClient(kubeConfigFilePath, logger),
        isThreadSafe: true);

    public IKubernetesSecretStoreClient Create() => _client.Value;
}

public sealed class KubernetesSecretStoreClient : IKubernetesSecretStoreClient, IDisposable
{
    private const string ManagedByLabelName = "app.kubernetes.io/managed-by";
    private const string ManagedByLabelValue = "opcplc";

    private readonly Kubernetes _client;
    private readonly ILogger _logger;

    public KubernetesSecretStoreClient(string kubeConfigFilePath, ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var kubernetesClientConfiguration = string.IsNullOrWhiteSpace(kubeConfigFilePath)
            ? KubernetesClientConfiguration.BuildDefaultConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigFilePath);

        _client = new Kubernetes(kubernetesClientConfiguration);
    }

    public async Task<IReadOnlyDictionary<string, byte[]>> ReadAsync(string namespaceName, string secretName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var secret = await FindSecretAsync(namespaceName, secretName, ct).ConfigureAwait(false);
        if (secret?.Data is null || secret.Data.Count == 0)
        {
            return new Dictionary<string, byte[]>(StringComparer.Ordinal);
        }

        return secret.Data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    public async Task WriteAsync(string namespaceName, string secretName, IReadOnlyDictionary<string, byte[]> data, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        ArgumentNullException.ThrowIfNull(data);

        var secret = await FindSecretAsync(namespaceName, secretName, ct).ConfigureAwait(false);
        var secretData = data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);

        if (secret is null)
        {
            _logger.LogInformation("Creating Kubernetes secret-backed certificate store {Namespace}/{SecretName}", namespaceName, secretName);
            var newSecret = new V1Secret
            {
                Metadata = new V1ObjectMeta
                {
                    Name = secretName,
                    NamespaceProperty = namespaceName,
                    Labels = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [ManagedByLabelName] = ManagedByLabelValue,
                    },
                },
                Type = "Opaque",
                Data = secretData,
            };

            await _client.CoreV1.CreateNamespacedSecretAsync(newSecret, namespaceName, cancellationToken: ct).ConfigureAwait(false);
            return;
        }

        secret.Data = secretData;
        await _client.CoreV1.ReplaceNamespacedSecretAsync(secret, secretName, namespaceName, cancellationToken: ct).ConfigureAwait(false);
    }

    public void Dispose() => _client.Dispose();

    private async Task<V1Secret> FindSecretAsync(string namespaceName, string secretName, CancellationToken ct)
    {
        var secretList = await _client.CoreV1.ListNamespacedSecretAsync(
            namespaceName,
            fieldSelector: $"metadata.name={secretName}",
            cancellationToken: ct).ConfigureAwait(false);

        return secretList.Items.FirstOrDefault();
    }
}
