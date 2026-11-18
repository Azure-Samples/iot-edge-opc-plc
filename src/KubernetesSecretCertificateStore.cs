namespace OpcPlc.Certs;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
    private const string PendingKeyEntryPrefix = "opcplc-pending-";
    private const string CrtExtension = ".crt";
    private const string DerExtension = ".der";
    private const string PemExtension = ".pem";
    private const string KeyExtension = ".key";
    private const string PfxExtension = ".pfx";
    private const string CrlExtension = ".crl";
    // PEMReader.ImportPublicKeysFromPEM examines at most this many certificate blocks.
    private const int SdkPemReaderBlockLimit = 99;
    private static readonly ConditionalWeakTable<IKubernetesSecretStoreClient,
        ConcurrentDictionary<(string Namespace, string Secret), SemaphoreSlim>> MutationGates = new();

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

    public async Task AddAsync(
        Certificate certificate,
        char[] password = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        await UpdateSecretDataAsync(secretData =>
        {
            secretData[GetCertificateEntryKey(certificate.Thumbprint)] = certificate.Export(X509ContentType.Cert);
            if (!_noPrivateKeys && certificate.HasPrivateKey)
            {
                secretData[GetPrivateKeyEntryKey(certificate.Thumbprint)] = certificate.Export(
                    X509ContentType.Pkcs12,
                    password is null ? string.Empty : new string(password));
            }
            return true;
        }, ct).ConfigureAwait(false);
    }

    public async Task AddRejectedAsync(CertificateCollection certificates, int maxCertificates, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(certificates);

        await UpdateSecretDataAsync(secretData =>
        {
            foreach (var certificate in certificates)
            {
                secretData[GetCertificateEntryKey(certificate.Thumbprint)] = certificate.Export(X509ContentType.Cert);
            }
            if (maxCertificates > 0)
            {
                var certificateKeys = secretData.Keys
                    .Where(IsCertificateEntryKey)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToList();
                int overflow = certificateKeys.Count - maxCertificates;
                foreach (var certificateKey in certificateKeys.Take(Math.Max(overflow, 0)))
                {
                    secretData.Remove(certificateKey);
                    secretData.Remove(GetPrivateKeyEntryKey(GetThumbprintFromEntryKey(certificateKey)));
                }
            }
            return true;
        }, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string thumbprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        return await UpdateSecretDataAsync(secretData => RemoveMatchingCertificateEntries(secretData, thumbprint), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes every copy of a certificate and the private keys proven to belong only to it. All entries are scanned
    /// before anything changes, so a removal that cannot be done safely leaves the Secret untouched. Like the SDK
    /// directory store, deletion is not reversible: a rollback re-adds the certificate as a canonical entry.
    /// </summary>
    private bool RemoveMatchingCertificateEntries(Dictionary<string, byte[]> secretData, string thumbprint)
    {
        var thumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { thumbprint };
        var scan = new DeletionScan();
        var removals = new List<(string EntryKey, byte[] Remaining)>();
        foreach (string entryKey in secretData.Keys.Where(IsCertificateEntryKey).ToList())
        {
            switch (RemoveCertificates(entryKey, secretData[entryKey], thumbprints, scan, out byte[] remaining))
            {
                case CertificateRemoval.Refused:
                    _logger.LogWarning("Not deleting certificate {Thumbprint} from Kubernetes secret {SecretName}: " +
                        "removing it from entry {EntryKey} would change how its remaining PEM content is read",
                        thumbprint, _secretName, entryKey);
                    return false;
                case CertificateRemoval.Removed:
                    removals.Add((entryKey, remaining));
                    break;
            }
        }

        foreach ((string entryKey, byte[] remaining) in removals)
        {
            if (remaining is null)
            {
                secretData.Remove(entryKey);
            }
            else
            {
                secretData[entryKey] = remaining;
            }
        }

        var modifiedBaseNames = removals.Select(removal => GetBaseName(removal.EntryKey))
            .ToHashSet(StringComparer.Ordinal);
        string canonicalKey = GetPrivateKeyEntryKey(thumbprint);
        bool removed = removals.Count > 0;
        foreach (string entryKey in secretData.Keys.Where(key => key == canonicalKey ||
            (IsCompanionKeyEntryKey(key) && modifiedBaseNames.Contains(GetBaseName(key)))).ToList())
        {
            byte[] keyData = secretData[entryKey];
            if (keyData.Length > 0 && CanDeletePrivateKey(entryKey, keyData, thumbprint, scan))
            {
                secretData.Remove(entryKey);
                removed = true;
            }
        }
        return removed;
    }
    /// <summary>
    /// Removes the certificates with the given thumbprints from one entry, keeping every other byte of PEM content.
    /// </summary>
    private static CertificateRemoval RemoveCertificates(string entryKey, byte[] data, IReadOnlySet<string> thumbprints,
        DeletionScan scan, out byte[] remaining)
    {
        remaining = data;
        string baseName = GetBaseName(entryKey);
        if (!IsPemCertificateEntryKey(entryKey))
        {
            if (Classify(data, thumbprints, baseName, scan, skipEmptyNames: false) != CertificateKind.Match)
            {
                return CertificateRemoval.NotFound;
            }
            remaining = null;
            return CertificateRemoval.Removed;
        }

        var blocks = FindPemCertificateBlocks(data);
        var kept = new List<byte[]>(blocks.Count);
        using var output = new MemoryStream(data.Length);
        int copied = 0;
        foreach (PemCertificateBlock block in blocks)
        {
            if (Classify(block.Der, thumbprints, baseName, scan, skipEmptyNames: true) == CertificateKind.Match)
            {
                output.Write(data, copied, block.Start - copied);
                copied = block.End;
            }
            else
            {
                kept.Add(block.Der);
            }
        }
        if (copied == 0)
        {
            return CertificateRemoval.NotFound;
        }
        output.Write(data, copied, data.Length - copied);
        byte[] result = output.ToArray();

        // Joining the text around a removed block, or bringing blocks beyond the reader's limit into view, must not
        // make a certificate readable that was not before; otherwise a certificate never enumerated becomes trusted.
        if (!FindPemCertificateBlocks(result).Select(block => block.Der)
            .ToHashSet(ByteArrayComparer.Instance).SetEquals(kept))
        {
            return CertificateRemoval.Refused;
        }
        remaining = string.IsNullOrWhiteSpace(Encoding.UTF8.GetString(result)) ? null : result;
        return CertificateRemoval.Removed;
    }

    private static CertificateKind Classify(byte[] der, IReadOnlySet<string> thumbprints, string baseName,
        DeletionScan scan, bool skipEmptyNames)
    {
        Certificate certificate;
        try
        {
            certificate = der is null ? null : new Certificate(der);
        }
        catch (CryptographicException)
        {
            certificate = null;
        }
        if (certificate is null)
        {
            scan?.UnreadableBaseNames.Add(baseName);
            return CertificateKind.Unreadable;
        }

        using (certificate)
        {
            // Mirrors the PEM reader, which never returns these certificates.
            if (skipEmptyNames && DistinguishedNameUtils.HasEmptyDistinguishedName(certificate))
            {
                return CertificateKind.Hidden;
            }
            string publicKey = Convert.ToHexString(certificate.PublicKey.ExportSubjectPublicKeyInfo());
            if (thumbprints.Contains(certificate.Thumbprint))
            {
                scan?.SetRemoved(certificate.RawData, publicKey);
                return CertificateKind.Match;
            }
            scan?.AddSurvivor(baseName, publicKey);
            return CertificateKind.Survivor;
        }
    }

    private bool CanDeletePrivateKey(string entryKey, byte[] data, string thumbprint, DeletionScan scan)
    {
        string baseName = GetBaseName(entryKey);
        try
        {
            if (entryKey.EndsWith(PfxExtension, StringComparison.OrdinalIgnoreCase))
            {
                // A PFX carries its own certificate and LoadPrivateKeyAsync returns that certificate, so a PFX whose
                // private keys all belong to the deleted certificate is removed even when a survivor shares its key.
                if (TryReadPfxIdentities(data, out List<string> identities))
                {
                    return identities.Count > 0 && identities.All(candidate =>
                        string.Equals(candidate, thumbprint, StringComparison.OrdinalIgnoreCase));
                }
                // AddAsync writes <THUMBPRINT>.pfx, so the name proves ownership when a password protects the content.
                if (string.Equals(entryKey, GetPrivateKeyEntryKey(thumbprint), StringComparison.Ordinal))
                {
                    return true;
                }
                _logger.LogWarning("Retaining private key entry {EntryKey} in Kubernetes secret {SecretName} " +
                    "because ownership could not be established", entryKey, _secretName);
                return false;
            }

            if (scan.UnreadableBaseNames.Contains(baseName))
            {
                _logger.LogWarning("Retaining private key entry {EntryKey} in Kubernetes secret {SecretName} " +
                    "because an unreadable certificate may still need it", entryKey, _secretName);
                return false;
            }
            // A PEM key pairs with whichever certificate its entry holds, so a survivor using it keeps it.
            if (scan.RemovedRawData is null || scan.HasSurvivor(baseName, scan.RemovedPublicKey))
            {
                return false;
            }
            string pem = Encoding.UTF8.GetString(data);
            if (!PemEncoding.TryFind(pem, out PemFields fields) ||
                !string.IsNullOrWhiteSpace(pem[..fields.Location.Start]) ||
                !string.IsNullOrWhiteSpace(pem[fields.Location.End..]))
            {
                _logger.LogWarning("Retaining private key entry {EntryKey} in Kubernetes secret {SecretName} " +
                    "because it contains unrecognized or additional content", entryKey, _secretName);
                return false;
            }
            using var withKey = X509Certificate2.CreateFromPem(
                PemEncoding.WriteString("CERTIFICATE", scan.RemovedRawData), pem);
            return withKey.HasPrivateKey;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            _logger.LogWarning(ex, "Retaining private key entry {EntryKey} in Kubernetes secret {SecretName} " +
                "because ownership could not be established", entryKey, _secretName);
            return false;
        }
    }

    /// <summary>
    /// The thumbprints of the certificates a PFX provides a private key for; public chain certificates do not count.
    /// A PFX without private keys is identified by all of its certificates.
    /// </summary>
    private static bool TryReadPfxIdentities(byte[] data, out List<string> thumbprints)
    {
        foreach (string password in new[] { null, string.Empty })
        {
            try
            {
                var certificates = X509CertificateLoader.LoadPkcs12Collection(data, password,
                    X509KeyStorageFlags.EphemeralKeySet);
                var withKeys = certificates.Where(certificate => certificate.HasPrivateKey).ToList();
                thumbprints = (withKeys.Count > 0 ? withKeys : certificates.ToList())
                    .Select(certificate => certificate.Thumbprint).ToList();
                foreach (var certificate in certificates)
                {
                    certificate.Dispose();
                }
                return true;
            }
            catch (CryptographicException)
            {
                // Password protected or malformed; ownership falls back to the entry name.
            }
        }
        thumbprints = null;
        return false;
    }

    public async Task<CertificateCollection> EnumerateAsync(CancellationToken ct = default)
    {
        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var certificates = new CertificateCollection();
        var thumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var certificateEntry in secretData.Where(pair => IsCertificateEntryKey(pair.Key)))
        {
            try
            {
                using var loadedCertificates = LoadCertificates(certificateEntry.Key, certificateEntry.Value);
                foreach (var certificate in loadedCertificates)
                {
                    // SDK TrustList reconciliation expects one certificate per thumbprint, not per Secret entry.
                    if (thumbprints.Add(certificate.Thumbprint))
                    {
                        certificates.Add(certificate);
                    }
                }
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

        await UpdateSecretDataAsync(secretData =>
        {
            secretData[GetCrlEntryKey(crl)] = crl.RawData;
            return true;
        }, ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteCRLAsync(X509CRL crl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(crl);

        return await UpdateSecretDataAsync(secretData => secretData.Remove(GetCrlEntryKey(crl)), ct)
            .ConfigureAwait(false);
    }

    internal async ValueTask<(Certificate Certificate, DateTimeOffset? CreatedAt)> LoadPendingKeyAsync(
        string scope, char[] password, CancellationToken ct)
    {
        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        string keyEntry = GetPendingKeyEntry(scope);
        if (!secretData.TryGetValue(keyEntry, out byte[] keyData))
        {
            return (null, null);
        }
        if (!secretData.TryGetValue(keyEntry + ".created", out byte[] createdAtData) ||
            !DateTimeOffset.TryParse(Encoding.UTF8.GetString(createdAtData), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTimeOffset createdAt))
        {
            throw new ServiceResultException(StatusCodes.BadInvalidState,
                "The pending signing-key creation time in Kubernetes Secret is invalid.");
        }
        Certificate certificate = new(keyData, password is null ? string.Empty : new string(password),
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        return (certificate, createdAt);
    }

    internal async ValueTask SavePendingKeyAsync(
        string scope, Certificate certificate, char[] password, DateTimeOffset createdAt, CancellationToken ct)
    {
        await UpdateSecretDataAsync(secretData =>
        {
            string keyEntry = GetPendingKeyEntry(scope);
            secretData[keyEntry] = certificate.Export(X509ContentType.Pkcs12,
                password is null ? string.Empty : new string(password));
            secretData[keyEntry + ".created"] =
                Encoding.UTF8.GetBytes(createdAt.ToString("O", CultureInfo.InvariantCulture));
            return true;
        }, ct).ConfigureAwait(false);
    }

    internal async ValueTask DeletePendingKeyAsync(string scope, CancellationToken ct)
    {
        await UpdateSecretDataAsync(secretData =>
        {
            string keyEntry = GetPendingKeyEntry(scope);
            bool removedKey = secretData.Remove(keyEntry);
            bool removedCreationTime = secretData.Remove(keyEntry + ".created");
            return removedKey || removedCreationTime;
        }, ct).ConfigureAwait(false);
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

    public async Task<X509CRLCollection> EnumerateCRLsAsync(Certificate issuer, bool validateUpdateTime = true, CancellationToken ct = default)
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

    public async Task<CertificateCollection> FindByThumbprintAsync(string thumbprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
        var certificates = new CertificateCollection();

        if (secretData.TryGetValue(GetCertificateEntryKey(thumbprint), out var certificateBytes))
        {
            using var certificate = new Certificate(certificateBytes);
            certificates.Add(certificate);
        }

        foreach (var certificateEntry in secretData.Where(pair => IsPemCertificateEntryKey(pair.Key)))
        {
            using var loadedCertificates = LoadCertificates(certificateEntry.Key, certificateEntry.Value);
            foreach (var certificate in loadedCertificates.Where(candidate => string.Equals(candidate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)))
            {
                certificates.Add(certificate);
            }
        }

        return certificates;
    }

    public async Task<StatusCode> IsRevokedAsync(Certificate issuer, Certificate certificate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentNullException.ThrowIfNull(certificate);

        var crls = await EnumerateCRLsAsync(issuer, validateUpdateTime: true, ct).ConfigureAwait(false);
        return crls.Any(crl => crl.IsRevoked(certificate))
            ? StatusCodes.BadCertificateRevoked
            : StatusCodes.Good;
    }

    public Task<Certificate> LoadPrivateKeyAsync(string thumbprint, string subjectName, string password, CancellationToken ct = default)
        => LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri: null, certificateType: default, password?.ToCharArray(), ct);

    public async Task<Certificate> LoadPrivateKeyAsync(string thumbprint, string subjectName, string applicationUri, NodeId certificateType, char[] password = null, CancellationToken ct = default)
    {
        if (_noPrivateKeys)
        {
            return null;
        }

        var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(thumbprint) && secretData.TryGetValue(GetPrivateKeyEntryKey(thumbprint), out var privateKeyBytes))
        {
            using var certificate = new Certificate(
                privateKeyBytes,
                password is null ? string.Empty : new string(password),
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            if (MatchCertificate(certificate, thumbprint, subjectName, applicationUri, certificateType))
            {
                return certificate.AddRef();
            }
        }

        foreach (var certificateEntry in secretData.Where(pair => IsCertificateEntryKey(pair.Key)))
        {
            try
            {
                using var loadedCertificates = LoadCertificates(certificateEntry.Key, certificateEntry.Value);
                foreach (var certificate in loadedCertificates)
                {
                    if (!MatchCertificate(certificate, thumbprint, subjectName, applicationUri, certificateType))
                    {
                        continue;
                    }

                    var certificateBaseName = GetBaseName(certificateEntry.Key);
                    if (secretData.TryGetValue(certificateBaseName + PfxExtension, out var pkcs12Bytes))
                    {
                        return new Certificate(
                            pkcs12Bytes,
                            password is null ? string.Empty : new string(password),
                            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                    }

                    if (secretData.TryGetValue(certificateBaseName + PemExtension, out var pemPrivateKeyBytes))
                    {
                        var certificateWithPrivateKey = CreateCertificateFromPem(certificateEntry.Value, pemPrivateKeyBytes);
                        return certificateWithPrivateKey;
                    }

                    if (secretData.TryGetValue(certificateBaseName + KeyExtension, out var keyPrivateKeyBytes))
                    {
                        var certificateWithPrivateKey = CreateCertificateFromPem(certificateEntry.Value, keyPrivateKeyBytes);
                        return certificateWithPrivateKey;
                    }
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

    private static bool IsCompanionKeyEntryKey(string key) =>
        key.EndsWith(PfxExtension, StringComparison.OrdinalIgnoreCase) ||
        key.EndsWith(PemExtension, StringComparison.OrdinalIgnoreCase) ||
        key.EndsWith(KeyExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsPrivateKeyEntryKey(string key) => key.EndsWith(PfxExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsCrlEntryKey(string key) => key.EndsWith(CrlExtension, StringComparison.OrdinalIgnoreCase);

    private async Task<Dictionary<string, byte[]>> LoadSecretDataAsync(CancellationToken ct)
    {
        EnsureOpen();
        try
        {
            var data = await _client.ReadAsync(_defaultNamespace, _secretName, ct).ConfigureAwait(false);
            return data.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
        }
        catch (k8s.Autorest.HttpOperationException exception)
            when (exception.Response?.StatusCode == HttpStatusCode.Forbidden)
        {
            throw CreateAccessDeniedException(exception, "read");
        }
    }

    private static string GetPendingKeyEntry(string scope) => PendingKeyEntryPrefix + scope;

    private async Task<bool> UpdateSecretDataAsync(
        Func<Dictionary<string, byte[]>, bool> update, CancellationToken ct)
    {
        EnsureOpen();
        // Stores opened for the same Secret share a client and must not overwrite each other's mutations.
        SemaphoreSlim gate = MutationGates.GetOrCreateValue(_client)
            .GetOrAdd((_defaultNamespace, _secretName), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
            bool changed = update(secretData);
            if (changed)
            {
                await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
            }
            return changed;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task SaveSecretDataAsync(Dictionary<string, byte[]> secretData, CancellationToken ct)
    {
        EnsureOpen();
        try
        {
            await _client.WriteAsync(_defaultNamespace, _secretName, secretData, ct).ConfigureAwait(false);
        }
        catch (k8s.Autorest.HttpOperationException exception)
            when (exception.Response?.StatusCode == HttpStatusCode.Forbidden)
        {
            throw CreateAccessDeniedException(exception, "write");
        }
    }

    private ServiceResultException CreateAccessDeniedException(Exception exception, string operation)
    {
        _logger.LogError(exception,
            "Kubernetes denied {Operation} access to certificate secret {Namespace}/{SecretName}; check service account RBAC",
            operation, _defaultNamespace, _secretName);
        return new ServiceResultException(StatusCodes.BadUserAccessDenied,
            $"Kubernetes denied {operation} access to certificate secret '{_secretName}' in namespace '{_defaultNamespace}'.",
            exception);
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

    private static CertificateCollection LoadCertificates(string entryKey, byte[] certificateBytes)
    {
        if (entryKey.EndsWith(DerExtension, StringComparison.OrdinalIgnoreCase))
        {
            using var certificate = new Certificate(certificateBytes);
            return new CertificateCollection { certificate };
        }

        if (entryKey.EndsWith(CrtExtension, StringComparison.OrdinalIgnoreCase))
        {
            return CertificateCollection.From(PEMReader.ImportPublicKeysFromPEM(certificateBytes));
        }

        throw new ArgumentException($"Unsupported certificate entry key '{entryKey}'.", nameof(entryKey));
    }

    /// <summary>
    /// Locates the certificate blocks <see cref="PEMReader.ImportPublicKeysFromPEM(ReadOnlySpan{byte})"/> examines,
    /// so deletion only edits what is enumerated: the END label is searched from the end of the previous block,
    /// scanning stops when it precedes the next BEGIN label or is missing, and at most 99 blocks are examined.
    /// </summary>
    private static List<PemCertificateBlock> FindPemCertificateBlocks(byte[] data)
    {
        ReadOnlySpan<byte> beginLabel = "-----BEGIN CERTIFICATE-----"u8;
        ReadOnlySpan<byte> endLabel = "-----END CERTIFICATE-----"u8;
        var blocks = new List<PemCertificateBlock>();
        int offset = 0;
        while (blocks.Count < SdkPemReaderBlockLimit)
        {
            ReadOnlySpan<byte> remaining = data.AsSpan(offset);
            int begin = remaining.IndexOf(beginLabel);
            if (begin < 0)
            {
                break;
            }
            int end = remaining.IndexOf(endLabel);
            int contentStart = begin + beginLabel.Length;
            if (end < 0 || end <= contentStart)
            {
                break;
            }
            string content = Encoding.UTF8.GetString(remaining[contentStart..end]);
            byte[] decoded = new byte[content.Length];
            byte[] der = Convert.TryFromBase64Chars(content, decoded, out int written) ? decoded[..written] : null;
            blocks.Add(new PemCertificateBlock(offset + begin, offset + end + endLabel.Length, der));
            offset += end + endLabel.Length;
        }
        return blocks;
    }

    private static Certificate CreateCertificateFromPem(byte[] certificateBytes, byte[] privateKeyBytes)
    {
        return Certificate.From(X509Certificate2.CreateFromPem(
            Encoding.UTF8.GetString(certificateBytes),
            Encoding.UTF8.GetString(privateKeyBytes)));
    }

    private static bool MatchCertificate(
        Certificate certificate, string thumbprint, string subjectName, string applicationUri, NodeId certificateType)
    {
        if (certificateType.IsNull || certificateType == ObjectTypeIds.RsaSha256ApplicationCertificateType || certificateType == ObjectTypeIds.RsaMinApplicationCertificateType || certificateType == ObjectTypeIds.ApplicationCertificateType)
        {
            if (!string.IsNullOrEmpty(thumbprint) && !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(subjectName) && !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) && (subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) || !X509Utils.ParseDistinguishedName(certificate.Subject).Any(subject => subject.Equals("CN=" + subjectName, StringComparison.Ordinal))))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(applicationUri) &&
                !X509Utils.CompareApplicationUriWithCertificate(certificate, applicationUri))
            {
                return false;
            }

            return X509Utils.GetRSAPublicKeySize(certificate) >= 0;
        }

        return false;
    }

    private enum CertificateRemoval
    {
        NotFound,
        Removed,
        Refused,
    }

    private enum CertificateKind
    {
        Match,
        Survivor,
        Unreadable,
        Hidden,
    }

    /// <summary>
    /// A certificate block located in PEM text; <see cref="Der"/> is null when its base64 is invalid.
    /// </summary>
    private readonly record struct PemCertificateBlock(int Start, int End, byte[] Der);

    /// <summary>
    /// What a deletion learned about the certificate entries while scanning them.
    /// </summary>
    private sealed class DeletionScan
    {
        private readonly Dictionary<string, HashSet<string>> _survivors = new(StringComparer.Ordinal);

        public HashSet<string> UnreadableBaseNames { get; } = new(StringComparer.Ordinal);

        public byte[] RemovedRawData { get; private set; }

        public string RemovedPublicKey { get; private set; }

        public void SetRemoved(byte[] rawData, string publicKey)
        {
            RemovedRawData ??= rawData;
            RemovedPublicKey ??= publicKey;
        }

        public void AddSurvivor(string baseName, string publicKey)
        {
            if (!_survivors.TryGetValue(baseName, out HashSet<string> publicKeys))
            {
                _survivors[baseName] = publicKeys = new HashSet<string>(StringComparer.Ordinal);
            }
            publicKeys.Add(publicKey);
        }

        public bool HasSurvivor(string baseName, string publicKey)
            => _survivors.TryGetValue(baseName, out HashSet<string> publicKeys) && publicKeys.Contains(publicKey);
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static ByteArrayComparer Instance { get; } = new();

        public bool Equals(byte[] x, byte[] y) => x is null ? y is null : y is not null && x.AsSpan().SequenceEqual(y);

        public int GetHashCode(byte[] obj) => obj?.Length ?? 0;
    }
}