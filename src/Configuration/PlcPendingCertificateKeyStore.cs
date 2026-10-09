namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Security.Certificates;
using Opc.Ua.Server;
using OpcPlc.Certs;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Keeps regenerated signing keys separate from active certificates in the configured custom store.
/// </summary>
public sealed class PlcPendingCertificateKeyStore(ICertificateStoreResolver resolver)
    : IPeekablePendingCertificateKeyStore, IDisposable
{
    private readonly ICertificateStoreResolver _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ValueTask<bool> SaveAsync(PendingCertificateKeyContext context, Certificate certificateWithPrivateKey,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(context, async (store, identifier, ct) =>
        {
            await SaveCoreAsync(store, identifier, context, certificateWithPrivateKey, ct).ConfigureAwait(false);
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
            using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
            if (entries.Count != 0)
            {
                return false;
            }
            await SaveCoreAsync(store, identifier, context, certificateWithPrivateKey, ct).ConfigureAwait(false);
            return true;
        }, cancellationToken);

    public async ValueTask RemoveAsync(PendingCertificateKeyContext context,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(context, async (store, _, ct) =>
        {
            using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
            foreach (Certificate entry in entries)
            {
                await DeleteAsync(store, entry.Thumbprint, ct).ConfigureAwait(false);
            }
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _gate.Dispose();

    private ValueTask<Certificate> ReadAsync(PendingCertificateKeyContext context, Certificate matchingCertificate,
        bool consume, CancellationToken cancellationToken) =>
        ExecuteAsync(context, async (store, identifier, ct) =>
        {
            using CertificateCollection entries = await store.EnumerateAsync(ct).ConfigureAwait(false);
            if (entries.Count == 0)
            {
                return null;
            }
            if (entries.Count != 1)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState,
                    "The pending signing-key scope contains multiple certificates.");
            }

            char[] password = context.PasswordProvider?.GetPassword(identifier);
            Certificate key = null;
            try
            {
                key = await store.LoadPrivateKeyAsync(entries[0].Thumbprint, entries[0].Subject, null,
                    NodeId.Null, password, ct).ConfigureAwait(false);
                if (key is null || !key.HasPrivateKey)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                        "The persisted pending signing key could not be loaded.");
                }
                if (matchingCertificate is not null && !X509Utils.VerifyKeyPair(matchingCertificate, key))
                {
                    return null;
                }
                if (consume)
                {
                    await DeleteAsync(store, key.Thumbprint, ct).ConfigureAwait(false);
                }
                Certificate result = key;
                key = null;
                return result;
            }
            finally
            {
                key?.Dispose();
                if (password is not null)
                {
                    Array.Clear(password);
                }
            }
        }, cancellationToken);

    private static async ValueTask SaveCoreAsync(ICertificateStore store, CertificateIdentifier identifier,
        PendingCertificateKeyContext context, Certificate certificate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (!certificate.HasPrivateKey)
        {
            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed,
                "A pending signing certificate must have a private key.");
        }
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
        string scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            context.CertificateGroupId + "|" + context.CertificateTypeId)))[..16].ToLowerInvariant();
        string path = context.BaseStore.StoreType switch
        {
            FlatDirectoryCertificateStore.StoreTypeName => context.BaseStore.StorePath + ".pending-" + scope,
            KubernetesSecretCertificateStore.StoreTypeName => GetSecretPath(context.BaseStore.StorePath, scope),
            _ => throw new ServiceResultException(StatusCodes.BadNotSupported,
                "This pending-key store requires a FlatDirectory or KubernetesSecret application store.")
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

    private static string GetSecretPath(string basePath, string scope)
    {
        string name = KubernetesSecretStorePath.NormalizeSecretName(
            basePath[KubernetesSecretCertificateStore.StoreTypePrefix.Length..]);
        // Hash the full name as well, so truncating a long Kubernetes name cannot merge scopes.
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16].ToLowerInvariant();
        return KubernetesSecretCertificateStore.StoreTypePrefix +
            name[..Math.Min(name.Length, 200)] + "-pending-" + hash + "-" + scope;
    }
}
