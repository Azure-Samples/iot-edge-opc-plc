namespace OpcPlc.Certs;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Kubernetes Secret backed certificate store.
/// </summary>
public sealed class KubernetesSecretCertificateStore : ICertificateStore
{
    private const string CrtExtension = ".crt";
    private const string DerExtension = ".der";
    private const string PemExtension = ".pem";
    private const string KeyExtension = ".key";
    private const string PfxExtension = ".pfx";
    private const string CrlExtension = ".crl";

    private readonly IKubernetesSecretStoreClient _client;
    private readonly string _defaultNamespace;
    private readonly ILogger _logger;

    private bool _noPrivateKeys;
    private string _secretName;
    private string _storePath;

    /// <summary>
    /// Identifier for Kubernetes secret certificate store.
    /// </summary>
    public const string StoreTypeName = "KubernetesSecret";

    /// <summary>
    /// Prefix for Kubernetes secret certificate store.
    /// </summary>
    public const string StoreTypePrefix = $"{StoreTypeName}:";

    public KubernetesSecretCertificateStore(ILogger logger, IKubernetesSecretStoreClient client, string defaultNamespace)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _defaultNamespace = KubernetesSecretStorePath.ResolveNamespace(defaultNamespace);
    }

    /// <inheritdoc/>
    public string StoreType => StoreTypeName;

    /// <inheritdoc/>
    public string StorePath => _storePath;

    /// <inheritdoc/>
    public bool SupportsLoadPrivateKey => true;

    /// <inheritdoc/>
    public bool SupportsCRLs => true;

    /// <inheritdoc/>
    public bool NoPrivateKeys => _noPrivateKeys;

    public void Dispose()
    {
    }

    public void Open(string location, bool noPrivateKeys = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(location);
        if (!location.StartsWith(StoreTypePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected argument {nameof(location)} starting with {StoreTypePrefix}",
                nameof(location));
        }

        _storePath = location;
        _secretName = KubernetesSecretStorePath.NormalizeSecretName(location.Substring(StoreTypePrefix.Length));
        _noPrivateKeys = noPrivateKeys;
    }

    public void Close()
    {
        _storePath = null;
        _secretName = null;
        _noPrivateKeys = true;
    }

    public async Task AddAsync(X509Certificate2 certificate, char[] password = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        secretData[GetCertificateEntryKey(certificate.Thumbprint)] = certificate.Export(X509ContentType.Cert);

        if (!_noPrivateKeys && certificate.HasPrivateKey)
        {
            secretData[GetPrivateKeyEntryKey(certificate.Thumbprint)] = certificate.Export(
                X509ContentType.Pkcs12,
                password is null ? string.Empty : new string(password));
        }

        await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
    }

    public async Task AddRejectedAsync(X509Certificate2Collection certificates, int maxCertificates, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(certificates);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);

        foreach (var certificate in certificates.Cast<X509Certificate2>())
        {
            secretData[GetCertificateEntryKey(certificate.Thumbprint)] = certificate.Export(X509ContentType.Cert);
        }

        if (maxCertificates > 0)
        {
            var certificateKeys = secretData.Keys
                .Where(IsCertificateEntryKey)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            var overflow = certificateKeys.Count - maxCertificates;
            foreach (var certificateKey in certificateKeys.Take(Math.Max(overflow, 0)))
            {
                secretData.Remove(certificateKey);
                secretData.Remove(GetPrivateKeyEntryKey(GetThumbprintFromEntryKey(certificateKey)));
            }
        }

        await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string thumbprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var removedCertificate = secretData.Remove(GetCertificateEntryKey(thumbprint));
        var removedPrivateKey = secretData.Remove(GetPrivateKeyEntryKey(thumbprint));

        if (removedCertificate || removedPrivateKey)
        {
            await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
        }

        return removedCertificate || removedPrivateKey;
    }

    public async Task<X509Certificate2Collection> EnumerateAsync(CancellationToken ct = default)
    {
        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var certificates = new X509Certificate2Collection();

        foreach (var certificateEntry in secretData.Where(pair => IsCertificateEntryKey(pair.Key)))
        {
            try
            {
                certificates.AddRange(LoadCertificates(certificateEntry.Key, certificateEntry.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load certificate entry {EntryKey} from Kubernetes secret {SecretName}", certificateEntry.Key, _secretName);
            }
        }

        return certificates;
    }

    public async Task AddCRLAsync(X509CRL crl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(crl);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        secretData[GetCrlEntryKey(crl)] = crl.RawData;
        await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteCRLAsync(X509CRL crl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(crl);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var removed = secretData.Remove(GetCrlEntryKey(crl));
        if (removed)
        {
            await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
        }

        return removed;
    }

    public async Task<X509CRLCollection> EnumerateCRLsAsync(CancellationToken ct = default)
    {
        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var crls = new X509CRLCollection();

        foreach (var crlBytes in secretData.Where(pair => IsCrlEntryKey(pair.Key)).Select(pair => pair.Value))
        {
            try
            {
                crls.Add(new X509CRL(crlBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load CRL from Kubernetes secret {SecretName}", _secretName);
            }
        }

        return crls;
    }

    public async Task<X509CRLCollection> EnumerateCRLsAsync(X509Certificate2 issuer, bool validateUpdateTime = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issuer);

        var crls = await EnumerateCRLsAsync(ct).ConfigureAwait(false);
        var filtered = new X509CRLCollection();

        foreach (var crl in crls.Where(crl => X509Utils.CompareDistinguishedName(crl.Issuer, issuer.Subject)))
        {
            if (!validateUpdateTime || crl.NextUpdate >= DateTime.UtcNow)
            {
                filtered.Add(crl);
            }
        }

        return filtered;
    }

    public async Task<X509Certificate2Collection> FindByThumbprintAsync(string thumbprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var certificates = new X509Certificate2Collection();

        if (secretData.TryGetValue(GetCertificateEntryKey(thumbprint), out var certificateBytes))
        {
            certificates.Add(X509CertificateLoader.LoadCertificate(certificateBytes));
        }

        foreach (var certificateEntry in secretData.Where(pair => IsPemCertificateEntryKey(pair.Key)))
        {
            foreach (var certificate in LoadCertificates(certificateEntry.Key, certificateEntry.Value).Where(certificate => string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)))
            {
                certificates.Add(certificate);
            }
        }

        return certificates;
    }

    public async Task<StatusCode> IsRevokedAsync(X509Certificate2 issuer, X509Certificate2 certificate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentNullException.ThrowIfNull(certificate);

        var crls = await EnumerateCRLsAsync(issuer, validateUpdateTime: true, ct).ConfigureAwait(false);
        return crls.Any(crl => crl.IsRevoked(certificate))
            ? StatusCodes.BadCertificateRevoked
            : StatusCodes.Good;
    }

    public Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName, string password, CancellationToken ct = default)
        => LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri: null, certificateType: null, password?.ToCharArray(), ct);

    public async Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName, string applicationUri, NodeId certificateType, char[] password = null, CancellationToken ct = default)
    {
        if (_noPrivateKeys)
        {
            return null;
        }

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(thumbprint) && secretData.TryGetValue(GetPrivateKeyEntryKey(thumbprint), out var privateKeyBytes))
        {
            return X509CertificateLoader.LoadPkcs12(
                privateKeyBytes,
                password is null ? string.Empty : new string(password),
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        }

        foreach (var certificateEntry in secretData.Where(pair => IsCertificateEntryKey(pair.Key)))
        {
            try
            {
                foreach (var certificate in LoadCertificates(certificateEntry.Key, certificateEntry.Value))
                {
                    if (!MatchCertificate(certificate, thumbprint, subjectName, certificateType))
                    {
                        certificate.Dispose();
                        continue;
                    }

                    var certificateBaseName = GetBaseName(certificateEntry.Key);
                    if (secretData.TryGetValue(certificateBaseName + PfxExtension, out var pkcs12Bytes))
                    {
                        certificate.Dispose();
                        return X509CertificateLoader.LoadPkcs12(
                            pkcs12Bytes,
                            password is null ? string.Empty : new string(password),
                            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                    }

                    if (secretData.TryGetValue(certificateBaseName + PemExtension, out var pemPrivateKeyBytes))
                    {
                        var certificateWithPrivateKey = CreateCertificateFromPem(certificateEntry.Value, pemPrivateKeyBytes);
                        certificate.Dispose();
                        return certificateWithPrivateKey;
                    }

                    if (secretData.TryGetValue(certificateBaseName + KeyExtension, out var keyPrivateKeyBytes))
                    {
                        var certificateWithPrivateKey = CreateCertificateFromPem(certificateEntry.Value, keyPrivateKeyBytes);
                        certificate.Dispose();
                        return certificateWithPrivateKey;
                    }

                    certificate.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load private key material for certificate entry {EntryKey} from Kubernetes secret {SecretName}", certificateEntry.Key, _secretName);
            }
        }

        return null;
    }

    private static string GetCertificateEntryKey(string thumbprint) => $"{thumbprint.ToUpperInvariant()}{DerExtension}";

    private static string GetPrivateKeyEntryKey(string thumbprint) => $"{thumbprint.ToUpperInvariant()}{PfxExtension}";

    private static string GetCrlEntryKey(X509CRL crl) => $"{Convert.ToHexString(SHA256.HashData(crl.RawData)).ToUpperInvariant()}{CrlExtension}";

    private static string GetThumbprintFromEntryKey(string entryKey)
    {
        if (entryKey.EndsWith(DerExtension, StringComparison.OrdinalIgnoreCase))
        {
            return entryKey[..^DerExtension.Length];
        }

        if (entryKey.EndsWith(CrtExtension, StringComparison.OrdinalIgnoreCase))
        {
            return entryKey[..^CrtExtension.Length];
        }

        if (entryKey.EndsWith(PfxExtension, StringComparison.OrdinalIgnoreCase))
        {
            return entryKey[..^PfxExtension.Length];
        }

        throw new ArgumentException($"Unsupported certificate entry key '{entryKey}'.", nameof(entryKey));
    }

    private static bool IsCertificateEntryKey(string key) =>
        key.EndsWith(DerExtension, StringComparison.OrdinalIgnoreCase) ||
        key.EndsWith(CrtExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsPemCertificateEntryKey(string key) => key.EndsWith(CrtExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsPrivateKeyEntryKey(string key) => key.EndsWith(PfxExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsCrlEntryKey(string key) => key.EndsWith(CrlExtension, StringComparison.OrdinalIgnoreCase);

    private async Task<Dictionary<string, byte[]>> LoadSecretDataAsync(CancellationToken ct)
    {
        EnsureOpen();
        var data = await _client.ReadAsync(_defaultNamespace, _secretName, ct).ConfigureAwait(false);
        return data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private async Task SaveSecretDataAsync(Dictionary<string, byte[]> secretData, CancellationToken ct)
    {
        EnsureOpen();
        await _client.WriteAsync(_defaultNamespace, _secretName, secretData, ct).ConfigureAwait(false);
    }

    private void EnsureOpen()
    {
        if (string.IsNullOrWhiteSpace(_secretName))
        {
            throw new InvalidOperationException("The Kubernetes secret certificate store must be opened before use.");
        }
    }

    private static string GetBaseName(string entryKey)
    {
        var extension = System.IO.Path.GetExtension(entryKey);
        return string.IsNullOrWhiteSpace(extension)
            ? entryKey
            : entryKey[..^extension.Length];
    }

    private static X509Certificate2Collection LoadCertificates(string entryKey, byte[] certificateBytes)
    {
        if (entryKey.EndsWith(DerExtension, StringComparison.OrdinalIgnoreCase))
        {
            return [X509CertificateLoader.LoadCertificate(certificateBytes)];
        }

        if (entryKey.EndsWith(CrtExtension, StringComparison.OrdinalIgnoreCase))
        {
            var certificates = new X509Certificate2Collection();
            certificates.ImportFromPem(Encoding.UTF8.GetString(certificateBytes));
            return certificates;
        }

        throw new ArgumentException($"Unsupported certificate entry key '{entryKey}'.", nameof(entryKey));
    }

    private static X509Certificate2 CreateCertificateFromPem(byte[] certificateBytes, byte[] privateKeyBytes)
    {
        return X509Certificate2.CreateFromPem(
            Encoding.UTF8.GetString(certificateBytes),
            Encoding.UTF8.GetString(privateKeyBytes));
    }

    private static bool MatchCertificate(X509Certificate2 certificate, string thumbprint, string subjectName, NodeId certificateType)
    {
        if (certificateType == null || certificateType == ObjectTypeIds.RsaSha256ApplicationCertificateType || certificateType == ObjectTypeIds.RsaMinApplicationCertificateType || certificateType == ObjectTypeIds.ApplicationCertificateType)
        {
            if (!string.IsNullOrEmpty(thumbprint) && !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(subjectName) && !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) && (subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) || !X509Utils.ParseDistinguishedName(certificate.Subject).Any(subject => subject.Equals("CN=" + subjectName, StringComparison.Ordinal))))
            {
                return false;
            }

            return X509Utils.GetRSAPublicKeySize(certificate) >= 0;
        }

        return false;
    }
}
