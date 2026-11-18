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
    private static readonly TimeSpan DeletionJournalLifetime = TimeSpan.FromMinutes(5);
    private static readonly ConditionalWeakTable<IKubernetesSecretStoreClient,
        ConcurrentDictionary<(string Namespace, string Secret), SemaphoreSlim>> MutationGates = new();
    private static readonly ConditionalWeakTable<IKubernetesSecretStoreClient,
        ConcurrentDictionary<(string Namespace, string Secret), DeletionJournal>> DeletionJournals = new();

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

    /// <summary>
    /// Clock for expiring deletion journal records; replaceable in tests.
    /// </summary>
    internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;

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

        var committed = new List<Action>();
        await UpdateSecretDataAsync(secretData =>
        {
            (bool restoredCertificate, bool restoredKey) =
                RestoreDeletedEntries(secretData, certificate.Thumbprint, committed);
            if (!restoredCertificate)
            {
                secretData[GetCertificateEntryKey(certificate.Thumbprint)] = certificate.Export(X509ContentType.Cert);
            }
            if (!_noPrivateKeys && certificate.HasPrivateKey && !restoredKey)
            {
                secretData[GetPrivateKeyEntryKey(certificate.Thumbprint)] = certificate.Export(
                    X509ContentType.Pkcs12,
                    password is null ? string.Empty : new string(password));
            }
            return true;
        }, committed, ct).ConfigureAwait(false);
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

        var committed = new List<Action>();
        return await UpdateSecretDataAsync(
            secretData => RemoveMatchingCertificateEntries(secretData, thumbprint, committed), committed, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes every copy of a certificate and the private keys proven to belong only to it. All entries are scanned
    /// before anything changes, so a removal that cannot be done safely leaves the Secret untouched.
    /// </summary>
    private bool RemoveMatchingCertificateEntries(
        Dictionary<string, byte[]> secretData, string thumbprint, List<Action> committed)
    {
        DeletionJournal journal = GetDeletionJournal();
        DateTimeOffset now = TimeProvider.GetUtcNow();
        journal.Prune(now);
        DateTimeOffset expiresAt = now + DeletionJournalLifetime;
        var thumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { thumbprint };
        var scan = new DeletionScan();
        var removals = new List<(string EntryKey, byte[] Original, byte[] Remaining)>();
        foreach (string entryKey in secretData.Keys.Where(IsCertificateEntryKey).ToList())
        {
            byte[] original = secretData[entryKey];
            switch (RemoveCertificates(entryKey, original, thumbprints, scan, out byte[] remaining))
            {
                case CertificateRemoval.Refused:
                    _logger.LogWarning("Not deleting certificate {Thumbprint} from Kubernetes secret {SecretName}: " +
                        "removing it from entry {EntryKey} would change how its remaining PEM content is read",
                        thumbprint, _secretName, entryKey);
                    return false;
                case CertificateRemoval.Removed:
                    removals.Add((entryKey, original, remaining));
                    break;
            }
        }

        foreach ((string entryKey, byte[] original, byte[] remaining) in removals)
        {
            if (remaining is null)
            {
                secretData.Remove(entryKey);
            }
            else
            {
                secretData[entryKey] = remaining;
            }
            DeletedCertificateEntry record = journal.GetCertificateEntry(entryKey);
            bool extend = record is not null && record.Matches(entryKey, original);
            committed.Add(() =>
            {
                if (extend)
                {
                    journal.Extend(record, thumbprint, expiresAt);
                }
                else
                {
                    journal.SetCertificateEntry(entryKey, new DeletedCertificateEntry(original, thumbprint, expiresAt));
                }
            });
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
                committed.Add(() => journal.SetKeyEntry(entryKey, new DeletedKeyEntry(thumbprint, keyData, expiresAt)));
                removed = true;
            }
        }
        return removed;
    }

    /// <summary>
    /// Restores the Secret entries a recent deletion removed for this certificate, as long as nothing changed them
    /// since. The SDK rolls failed configuration updates back by re-adding the deleted certificates, so this keeps a
    /// failed update from leaving seeded entries rewritten or their private keys deleted.
    /// </summary>
    private (bool RestoredCertificate, bool RestoredKey) RestoreDeletedEntries(
        Dictionary<string, byte[]> secretData, string thumbprint, List<Action> committed)
    {
        DeletionJournal journal = GetDeletionJournal();
        journal.Prune(TimeProvider.GetUtcNow());
        var certificateEntries = journal.GetCertificateEntriesRemoving(thumbprint);
        var keyEntries = journal.GetKeyEntriesOwnedBy(thumbprint);
        if (certificateEntries.Count == 0 && keyEntries.Count == 0)
        {
            return (false, false);
        }

        var restored = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach ((string entryKey, DeletedCertificateEntry record) in certificateEntries)
        {
            byte[] current = secretData.GetValueOrDefault(entryKey);
            if (!record.Matches(entryKey, current) || !record.TryRestore(entryKey, thumbprint, out byte[] content))
            {
                _logger.LogWarning("Not restoring deleted certificate {Thumbprint} into entry {EntryKey} of " +
                    "Kubernetes secret {SecretName} because the entry changed since the deletion",
                    thumbprint, entryKey, _secretName);
                journal.Forget(thumbprint);
                return (false, false);
            }
            restored[entryKey] = content;
        }

        bool restoredKey = false;
        foreach ((string entryKey, DeletedKeyEntry record) in keyEntries)
        {
            if (secretData.TryGetValue(entryKey, out byte[] current) && current.Length > 0)
            {
                continue;
            }
            restored[entryKey] = record.Data.ToArray();
            restoredKey = true;
        }

        foreach ((string entryKey, byte[] content) in restored)
        {
            secretData[entryKey] = content;
        }
        committed.Add(() =>
        {
            foreach ((string entryKey, DeletedCertificateEntry record) in certificateEntries)
            {
                journal.MarkRestored(entryKey, record, thumbprint);
            }
            foreach ((string entryKey, _) in keyEntries)
            {
                journal.RemoveKeyEntry(entryKey);
            }
        });
        return (certificateEntries.Count > 0, restoredKey);
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

        // Joining the text around a removed block must not form, merge or split other blocks; otherwise a
        // certificate the reader never saw could become trusted.
        if (!FindPemCertificateBlocks(result).Select(block => block.Der)
            .SequenceEqual(kept, ByteArrayComparer.Instance))
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
                // A PFX carries its own certificate and LoadPrivateKeyAsync returns that certificate, so a PFX that
                // holds the deleted certificate is removed even when a surviving certificate shares its key.
                if (TryReadPfxThumbprints(data, out List<string> pfxThumbprints))
                {
                    return pfxThumbprints.Count > 0 && pfxThumbprints.All(candidate =>
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

    private static bool TryReadPfxThumbprints(byte[] data, out List<string> thumbprints)
    {
        foreach (string password in new[] { null, string.Empty })
        {
            try
            {
                var certificates = X509CertificateLoader.LoadPkcs12Collection(data, password,
                    X509KeyStorageFlags.EphemeralKeySet);
                thumbprints = certificates.Select(certificate => certificate.Thumbprint).ToList();
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

    private Task<bool> UpdateSecretDataAsync(Func<Dictionary<string, byte[]>, bool> update, CancellationToken ct)
        => UpdateSecretDataAsync(update, committed: null, ct);

    /// <summary>
    /// Applies an update under the Secret's mutation gate and then runs <paramref name="committed"/>, still under the
    /// gate, only if the updated data was written.
    /// </summary>
    private async Task<bool> UpdateSecretDataAsync(
        Func<Dictionary<string, byte[]>, bool> update, List<Action> committed, CancellationToken ct)
    {
        EnsureOpen();
        // Stores opened for the same Secret share a client and must not overwrite each other's mutations.
        SemaphoreSlim gate = GetMutationGate();
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var secretData = await LoadSecretDataAsync(ct).ConfigureAwait(false);
            bool changed = update(secretData);
            if (changed)
            {
                await SaveSecretDataAsync(secretData, ct).ConfigureAwait(false);
                committed?.ForEach(action => action());
            }
            return changed;
        }
        finally
        {
            gate.Release();
        }
    }

    private SemaphoreSlim GetMutationGate()
        => MutationGates.GetOrCreateValue(_client)
            .GetOrAdd((_defaultNamespace, _secretName), _ => new SemaphoreSlim(1, 1));

    private DeletionJournal GetDeletionJournal()
        => DeletionJournals.GetOrCreateValue(_client)
            .GetOrAdd((_defaultNamespace, _secretName), _ => new DeletionJournal(GetMutationGate(), TimeProvider));

    /// <summary>
    /// The number of deleted entries currently kept for rollback; for tests.
    /// </summary>
    internal int DeletionJournalCount => GetDeletionJournal().Count;

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
            return LoadPemCertificates(certificateBytes);
        }

        throw new ArgumentException($"Unsupported certificate entry key '{entryKey}'.", nameof(entryKey));
    }

    /// <summary>
    /// Reads certificates like <see cref="PEMReader.ImportPublicKeysFromPEM(ReadOnlySpan{byte})"/>, including
    /// skipping blocks with invalid base64 or an empty name and rejecting the entry on an unreadable certificate,
    /// but without its 99-block limit. Deletion uses the same block scan, so it removes exactly what is enumerated.
    /// </summary>
    private static CertificateCollection LoadPemCertificates(byte[] data)
    {
        var certificates = new X509Certificate2Collection();
        try
        {
            foreach (PemCertificateBlock block in FindPemCertificateBlocks(data))
            {
                if (block.Der is null)
                {
                    continue;
                }
                X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(block.Der);
                if (DistinguishedNameUtils.HasEmptyDistinguishedName(certificate))
                {
                    certificate.Dispose();
                    continue;
                }
                certificates.Add(certificate);
            }
        }
        catch
        {
            foreach (X509Certificate2 certificate in certificates)
            {
                certificate.Dispose();
            }
            throw;
        }
        return CertificateCollection.From(certificates);
    }

    /// <summary>
    /// Locates certificate blocks the way the SDK PEM reader does: the END label is searched from the end of the
    /// previous block, and scanning stops when it precedes the next BEGIN label or is missing.
    /// </summary>
    private static List<PemCertificateBlock> FindPemCertificateBlocks(byte[] data)
    {
        ReadOnlySpan<byte> beginLabel = "-----BEGIN CERTIFICATE-----"u8;
        ReadOnlySpan<byte> endLabel = "-----END CERTIFICATE-----"u8;
        var blocks = new List<PemCertificateBlock>();
        int offset = 0;
        while (true)
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

    /// <summary>
    /// Entries removed by recent deletions from one Secret, kept so that a rollback can restore them. Records may hold
    /// private keys, so they are cleared once restored, replaced or expired, and a timer enforces
    /// <see cref="DeletionJournalLifetime"/> even when the Secret is not changed again. Records are only read and
    /// cleared under the Secret's mutation gate.
    /// </summary>
    private sealed class DeletionJournal(SemaphoreSlim gate, TimeProvider timeProvider)
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<string, DeletedCertificateEntry> _certificates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DeletedKeyEntry> _keys = new(StringComparer.Ordinal);
        private ITimer _sweep;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _certificates.Count + _keys.Count;
                }
            }
        }

        public void Prune(DateTimeOffset now)
        {
            lock (_lock)
            {
                foreach ((string entryKey, DeletedCertificateEntry entry) in _certificates
                    .Where(pair => pair.Value.ExpiresAt <= now).ToList())
                {
                    _certificates.Remove(entryKey);
                    entry.Clear();
                }
                foreach ((string entryKey, DeletedKeyEntry entry) in _keys
                    .Where(pair => pair.Value.ExpiresAt <= now).ToList())
                {
                    _keys.Remove(entryKey);
                    entry.Clear();
                }
            }
        }

        public DeletedCertificateEntry GetCertificateEntry(string entryKey)
        {
            lock (_lock)
            {
                return _certificates.GetValueOrDefault(entryKey);
            }
        }

        public void SetCertificateEntry(string entryKey, DeletedCertificateEntry entry)
        {
            lock (_lock)
            {
                if (_certificates.Remove(entryKey, out DeletedCertificateEntry replaced) && replaced != entry)
                {
                    replaced.Clear();
                }
                _certificates[entryKey] = entry;
                ScheduleSweepLocked(entry.ExpiresAt);
            }
        }

        public void Extend(DeletedCertificateEntry entry, string thumbprint, DateTimeOffset expiresAt)
        {
            lock (_lock)
            {
                entry.Removed.Add(thumbprint);
                entry.ExpiresAt = expiresAt;
                ScheduleSweepLocked(expiresAt);
            }
        }

        public void MarkRestored(string entryKey, DeletedCertificateEntry entry, string thumbprint)
        {
            lock (_lock)
            {
                entry.Removed.Remove(thumbprint);
                if (entry.Removed.Count == 0 && _certificates.GetValueOrDefault(entryKey) == entry)
                {
                    _certificates.Remove(entryKey);
                    entry.Clear();
                }
            }
        }

        public void SetKeyEntry(string entryKey, DeletedKeyEntry entry)
        {
            lock (_lock)
            {
                if (_keys.Remove(entryKey, out DeletedKeyEntry replaced) && replaced != entry)
                {
                    replaced.Clear();
                }
                _keys[entryKey] = entry;
                ScheduleSweepLocked(entry.ExpiresAt);
            }
        }

        public void RemoveKeyEntry(string entryKey)
        {
            lock (_lock)
            {
                if (_keys.Remove(entryKey, out DeletedKeyEntry entry))
                {
                    entry.Clear();
                }
            }
        }

        public List<(string EntryKey, DeletedCertificateEntry Entry)> GetCertificateEntriesRemoving(string thumbprint)
        {
            lock (_lock)
            {
                return _certificates.Where(pair => pair.Value.Removed.Contains(thumbprint))
                    .Select(pair => (pair.Key, pair.Value)).ToList();
            }
        }

        public List<(string EntryKey, DeletedKeyEntry Entry)> GetKeyEntriesOwnedBy(string thumbprint)
        {
            lock (_lock)
            {
                return _keys.Where(pair => string.Equals(pair.Value.Thumbprint, thumbprint,
                    StringComparison.OrdinalIgnoreCase)).Select(pair => (pair.Key, pair.Value)).ToList();
            }
        }

        public void Forget(string thumbprint)
        {
            lock (_lock)
            {
                foreach ((string entryKey, DeletedCertificateEntry entry) in _certificates
                    .Where(pair => pair.Value.Removed.Contains(thumbprint)).ToList())
                {
                    _certificates.Remove(entryKey);
                    entry.Clear();
                }
                foreach ((string entryKey, DeletedKeyEntry entry) in _keys.Where(pair => string.Equals(
                    pair.Value.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    _keys.Remove(entryKey);
                    entry.Clear();
                }
            }
        }

        private void ScheduleSweepLocked(DateTimeOffset dueAt)
        {
            // Later records expire after earlier ones, and a running sweep reschedules itself for what remains.
            if (_sweep is not null)
            {
                return;
            }
            TimeSpan delay = dueAt - timeProvider.GetUtcNow();
            _sweep = timeProvider.CreateTimer(_ => _ = SweepAsync(), null,
                delay > TimeSpan.Zero ? delay : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        private async Task SweepAsync()
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (_lock)
                {
                    _sweep?.Dispose();
                    _sweep = null;
                }
                Prune(timeProvider.GetUtcNow());
                lock (_lock)
                {
                    DateTimeOffset? next = _certificates.Values.Select(entry => (DateTimeOffset?)entry.ExpiresAt)
                        .Concat(_keys.Values.Select(entry => (DateTimeOffset?)entry.ExpiresAt)).Min();
                    if (next is not null)
                    {
                        ScheduleSweepLocked(next.Value);
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }
    }

    /// <summary>
    /// A certificate entry's content before the deletions of the certificates in <see cref="Removed"/>.
    /// </summary>
    private sealed class DeletedCertificateEntry(byte[] original, string thumbprint, DateTimeOffset expiresAt)
    {
        public byte[] Original { get; } = original;

        public HashSet<string> Removed { get; } = new(StringComparer.OrdinalIgnoreCase) { thumbprint };

        public DateTimeOffset ExpiresAt { get; set; } = expiresAt;

        /// <summary>
        /// Whether the entry holds exactly the original content without the removed certificates; an absent entry
        /// or the empty placeholder kept for TLS Secrets stands for content that was removed entirely.
        /// </summary>
        public bool Matches(string entryKey, byte[] current)
            => RemoveCertificates(entryKey, Original, Removed, scan: null, out byte[] expected)
                    == CertificateRemoval.Removed
                && (expected ?? []).AsSpan().SequenceEqual(current ?? []);

        /// <summary>
        /// A copy of the original content without the other removed certificates, so only the re-added one returns.
        /// </summary>
        public bool TryRestore(string entryKey, string thumbprint, out byte[] content)
        {
            var others = new HashSet<string>(Removed, StringComparer.OrdinalIgnoreCase);
            others.Remove(thumbprint);
            if (others.Count == 0)
            {
                content = Original.ToArray();
                return true;
            }
            return RemoveCertificates(entryKey, Original, others, scan: null, out content) == CertificateRemoval.Removed
                && content is not null;
        }

        // PEM entries can hold inline private keys.
        public void Clear() => CryptographicOperations.ZeroMemory(Original);
    }

    private sealed record DeletedKeyEntry(string Thumbprint, byte[] Data, DateTimeOffset ExpiresAt)
    {
        public void Clear() => CryptographicOperations.ZeroMemory(Data);
    }
    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static ByteArrayComparer Instance { get; } = new();

        public bool Equals(byte[] x, byte[] y) => x is null ? y is null : y is not null && x.AsSpan().SequenceEqual(y);

        public int GetHashCode(byte[] obj) => obj?.Length ?? 0;
    }
}