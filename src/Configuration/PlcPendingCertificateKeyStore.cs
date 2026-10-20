namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Keeps regenerated signing keys separate from active certificates in the configured store.
/// </summary>
public sealed class PlcPendingCertificateKeyStore(
    ICertificateStoreResolver resolver,
    TimeProvider timeProvider = null)
    : IPeekablePendingCertificateKeyStore, IDisposable
{
    private static readonly TimeSpan PendingKeyLifetime = TimeSpan.FromHours(24);
    private readonly ICertificateStoreResolver _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<(string StoreType, string Path, string Scope),
        (string Thumbprint, DateTimeOffset CreatedAt)> _claimedKeys = [];

    public ValueTask<bool> SaveAsync(PendingCertificateKeyContext context, Certificate certificateWithPrivateKey,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, async (store, identifier, ct) =>
        {
            ValidatePrivateKey(certificateWithPrivateKey);
            (Certificate existing, DateTimeOffset? createdAt) = await
                ReadPendingKeyAsync(store, identifier, context, ct).ConfigureAwait(false);
            using Certificate existingLifetime = existing;
            await SavePendingKeyAsync(store, identifier, context, certificateWithPrivateKey,
                existing?.Thumbprint == certificateWithPrivateKey.Thumbprint
                    ? createdAt.Value : _timeProvider.GetUtcNow(), ct).ConfigureAwait(false);
            return true;
        }, cancellationToken);

    public ValueTask<Certificate> TryTakeAsync(PendingCertificateKeyContext context,
        CancellationToken cancellationToken = default) =>
        ReadAsync(context, null, consume: true, cancellationToken);

    public ValueTask<Certificate> TryTakeMatchingAsync(PendingCertificateKeyContext context, Certificate certificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return ReadAsync(context, certificate, consume: true, cancellationToken);
    }

    public ValueTask<Certificate> TryPeekMatchingAsync(PendingCertificateKeyContext context, Certificate certificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return ReadAsync(context, certificate, consume: false, cancellationToken);
    }

    public ValueTask<bool> TryRestoreAsync(PendingCertificateKeyContext context, Certificate certificateWithPrivateKey,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, async (store, identifier, ct) =>
        {
            ValidatePrivateKey(certificateWithPrivateKey);
            (Certificate pending, _) = await ReadPendingKeyAsync(store, identifier, context, ct)
                .ConfigureAwait(false);
            using Certificate pendingLifetime = pending;
            if (pending is not null)
            {
                return false;
            }
            if (!_claimedKeys.TryGetValue(GetClaimScope(context), out var claimed) ||
                claimed.Thumbprint != certificateWithPrivateKey.Thumbprint ||
                IsExpired(claimed.CreatedAt))
            {
                return false;
            }
            await SavePendingKeyAsync(store, identifier, context, certificateWithPrivateKey,
                claimed.CreatedAt, ct).ConfigureAwait(false);
            _claimedKeys.Remove(GetClaimScope(context));
            return true;
        }, cancellationToken);

    public async ValueTask RemoveAsync(PendingCertificateKeyContext context,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(context, async (store, identifier, ct) =>
        {
            await RemovePendingKeyAsync(store, identifier, context, ct).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _gate.Dispose();

    private ValueTask<Certificate> ReadAsync(PendingCertificateKeyContext context, Certificate matchingCertificate,
        bool consume, CancellationToken cancellationToken) =>
        ExecuteAsync(context, async (store, identifier, ct) =>
        {
            (Certificate key, DateTimeOffset? createdAt) = await
                ReadPendingKeyAsync(store, identifier, context, ct).ConfigureAwait(false);
            try
            {
                if (key is null)
                {
                    return null;
                }
                if (matchingCertificate is not null && !X509Utils.VerifyKeyPair(matchingCertificate, key))
                {
                    return null;
                }
                if (consume)
                {
                    await RemovePendingKeyAsync(store, identifier, context, ct).ConfigureAwait(false);
                    // Rollback must preserve the original expiry, not start a new lifetime.
                    _claimedKeys[GetClaimScope(context)] = (key.Thumbprint, createdAt.Value);
                }
                Certificate result = key;
                key = null;
                return result;
            }
            finally
            {
                key?.Dispose();
            }
        }, cancellationToken);

    private async ValueTask RemovePendingKeyAsync(ICertificateStore store, CertificateIdentifier identifier,
        PendingCertificateKeyContext context, CancellationToken ct)
    {
        if (store is KubernetesSecretCertificateStore kubernetesStore)
        {
            await kubernetesStore.DeletePendingKeyAsync(GetScope(context), ct).ConfigureAwait(false);
            return;
        }
        using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
        foreach (Certificate entry in entries)
        {
            await DeleteAsync(store, entry.Thumbprint, ct).ConfigureAwait(false);
        }
        await DeleteCreatedAtAsync(identifier).ConfigureAwait(false);
    }

    private bool IsExpired(DateTimeOffset createdAt) =>
        _timeProvider.GetUtcNow() - createdAt >= PendingKeyLifetime;

    private static (string StoreType, string Path, string Scope) GetClaimScope(
        PendingCertificateKeyContext context) =>
        (context.BaseStore.StoreType, context.BaseStore.StorePath, GetScope(context));

    private static async ValueTask SaveCoreAsync(ICertificateStore store, CertificateIdentifier identifier,
        PendingCertificateKeyContext context, Certificate certificate, CancellationToken ct)
    {
        ValidatePrivateKey(certificate);
        char[] password = context.PasswordProvider?.GetPassword(identifier);
        try
        {
            using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
            if (!entries.Any(entry => entry.Thumbprint == certificate.Thumbprint))
            {
                await store.AddAsync(certificate, password, ct).ConfigureAwait(false);
            }
            using Certificate persisted = await store.LoadPrivateKeyAsync(certificate.Thumbprint,
                certificate.Subject, null, NodeId.Null, password, ct).ConfigureAwait(false);
            if (persisted is null || !persisted.HasPrivateKey || !X509Utils.VerifyKeyPair(certificate, persisted))
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                    "The pending signing key was not persisted.");
            }
            foreach (Certificate entry in entries)
            {
                if (entry.Thumbprint != certificate.Thumbprint)
                {
                    await DeleteAsync(store, entry.Thumbprint, ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (password is not null)
            {
                Array.Clear(password);
            }
        }
    }

    private async ValueTask<(Certificate Certificate, DateTimeOffset? CreatedAt)> ReadPendingKeyAsync(
        ICertificateStore store, CertificateIdentifier identifier, PendingCertificateKeyContext context,
        CancellationToken ct)
    {
        char[] password = context.PasswordProvider?.GetPassword(identifier);
        Certificate certificate = null;
        try
        {
            DateTimeOffset? createdAt;
            if (store is KubernetesSecretCertificateStore kubernetesStore)
            {
                (certificate, createdAt) = await kubernetesStore
                    .LoadPendingKeyAsync(GetScope(context), password, ct).ConfigureAwait(false);
            }
            else
            {
                using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
                if (entries.Count == 0)
                {
                    await DeleteCreatedAtAsync(identifier).ConfigureAwait(false);
                    return (null, null);
                }
                if (entries.Count != 1)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState,
                        "The pending signing-key scope contains multiple certificates.");
                }
                createdAt = await GetCreatedAtAsync(identifier, ct).ConfigureAwait(false);
                if (createdAt is null)
                {
                    // Older persisted keys have no age metadata; begin their lifetime on first access.
                    createdAt = _timeProvider.GetUtcNow();
                    await SaveCreatedAtAsync(identifier, createdAt.Value, ct).ConfigureAwait(false);
                }
                certificate = await store.LoadPrivateKeyAsync(entries[0].Thumbprint, entries[0].Subject, null,
                    NodeId.Null, password, ct).ConfigureAwait(false);
                if (certificate is null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                        "The persisted pending signing key could not be loaded.");
                }
            }
            if (certificate is null)
            {
                return (null, null);
            }
            if (!certificate.HasPrivateKey)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                    "The persisted pending signing key could not be loaded.");
            }
            if (IsExpired(createdAt.Value))
            {
                certificate.Dispose();
                certificate = null;
                await RemovePendingKeyAsync(store, identifier, context, ct).ConfigureAwait(false);
                return (null, null);
            }
            Certificate result = certificate;
            certificate = null;
            return (result, createdAt);
        }
        finally
        {
            certificate?.Dispose();
            if (password is not null)
            {
                Array.Clear(password);
            }
        }
    }

    private async ValueTask SavePendingKeyAsync(
        ICertificateStore store, CertificateIdentifier identifier, PendingCertificateKeyContext context,
        Certificate certificate, DateTimeOffset createdAt, CancellationToken ct)
    {
        ValidatePrivateKey(certificate);
        if (store is KubernetesSecretCertificateStore kubernetesStore)
        {
            char[] password = context.PasswordProvider?.GetPassword(identifier);
            try
            {
                await kubernetesStore.SavePendingKeyAsync(GetScope(context), certificate, password,
                    createdAt, ct).ConfigureAwait(false);
            }
            finally
            {
                if (password is not null)
                {
                    Array.Clear(password);
                }
            }
            return;
        }
        await SaveCoreAsync(store, identifier, context, certificate, ct).ConfigureAwait(false);
        await SaveCreatedAtAsync(identifier, createdAt, ct).ConfigureAwait(false);
    }

    private static async ValueTask<DateTimeOffset?> GetCreatedAtAsync(
        CertificateIdentifier identifier, CancellationToken ct)
    {
        string path = GetFileSystemPath(identifier.StorePath) + ".created";
        if (!File.Exists(path))
        {
            return null;
        }
        string value = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
            out DateTimeOffset createdAt))
        {
            throw new ServiceResultException(StatusCodes.BadInvalidState,
                "The pending signing-key creation time is invalid.");
        }
        return createdAt;
    }

    private static async ValueTask SaveCreatedAtAsync(
        CertificateIdentifier identifier, DateTimeOffset createdAt, CancellationToken ct)
    {
        await File.WriteAllTextAsync(GetFileSystemPath(identifier.StorePath) + ".created",
            createdAt.ToString("O", CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
    }

    private static ValueTask DeleteCreatedAtAsync(CertificateIdentifier identifier)
    {
        string path = GetFileSystemPath(identifier.StorePath) + ".created";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return ValueTask.CompletedTask;
    }

    private static void ValidatePrivateKey(Certificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (!certificate.HasPrivateKey)
        {
            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                "A pending signing certificate must have a private key.");
        }
    }

    private static string GetScope(PendingCertificateKeyContext context)
    {
        string scope = Utils.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            context.CertificateGroupId + "|" + context.CertificateTypeId)))[..16];
        return context.BaseStore.StoreType == CertificateStoreType.Directory ? scope : scope.ToLowerInvariant();
    }

    private static string GetFileSystemPath(string storePath)
    {
        string path = storePath.StartsWith(FlatDirectoryCertificateStore.StoreTypeName + ":", StringComparison.Ordinal)
            ? storePath[(FlatDirectoryCertificateStore.StoreTypeName.Length + 1)..]
            : storePath;
        return Utils.ReplaceSpecialFolderNames(path);
    }

    private static async ValueTask DeleteAsync(ICertificateStore store, string thumbprint, CancellationToken ct)
    {
        if (!await store.DeleteAsync(thumbprint, ct).ConfigureAwait(false))
        {
            throw new ServiceResultException(StatusCodes.BadInvalidState, "The pending signing key was not removed.");
        }
    }

    private async ValueTask<T> ExecuteAsync<T>(PendingCertificateKeyContext context,
        Func<ICertificateStore, CertificateIdentifier, CancellationToken, ValueTask<T>> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        string scope = GetScope(context);
        string path = context.BaseStore.StoreType switch
        {
            CertificateStoreType.Directory => Path.Combine(context.BaseStore.StorePath, "pending", scope),
            FlatDirectoryCertificateStore.StoreTypeName => context.BaseStore.StorePath + ".pending-" + scope,
            KubernetesSecretCertificateStore.StoreTypeName => context.BaseStore.StorePath,
            _ => throw new ServiceResultException(StatusCodes.BadNotSupported,
                "This pending-key store requires a Directory, FlatDirectory or KubernetesSecret application store.")
        };
        var identifier = new CertificateIdentifier
        {
            StoreType = context.BaseStore.StoreType,
            StorePath = path,
            CertificateType = context.CertificateTypeId
        };
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using ICertificateStore store = _resolver.OpenCertificateStore(path, identifier.StoreType,
                noPrivateKeys: false);
            return await operation(store, identifier, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

}
