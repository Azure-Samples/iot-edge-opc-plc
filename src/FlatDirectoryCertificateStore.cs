namespace OpcPlc.Certs;

using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public sealed class FlatDirectoryCertificateStore : ICertificateStore
{
    private const string CrtExtension = ".crt";
    private const string KeyExtension = ".key";
    private readonly DirectoryCertificateStore _innerStore;
    public const string StoreTypeName = "FlatDirectory";
    public const string StoreTypePrefix = $"{StoreTypeName}:";

    public FlatDirectoryCertificateStore() => _innerStore = new DirectoryCertificateStore(noSubDirs: true);

    public string StoreType => StoreTypeName;
    public string StorePath => _innerStore.StorePath;
    public bool SupportsLoadPrivateKey => _innerStore.SupportsLoadPrivateKey;
    public bool SupportsCRLs => _innerStore.SupportsCRLs;
    public bool NoPrivateKeys => _innerStore.NoPrivateKeys;

    public void Dispose() => _innerStore.Dispose();

    public void Open(string location, bool noPrivateKeys = true)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(location);
        if (!location.StartsWith(StoreTypePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Expected argument {nameof(location)} starting with {StoreTypePrefix}", nameof(location));
        }
        _innerStore.Open(location.Substring(StoreTypePrefix.Length), noPrivateKeys);
    }

    public void Close() => _innerStore.Close();

    // Legacy convenience methods delegate to async versions.
    public Task Add(X509Certificate2 certificate, string password = null) => AddAsync(certificate, password, CancellationToken.None);
    public Task AddRejected(X509Certificate2Collection certificates, int maxCertificates) => AddRejectedAsync(certificates, maxCertificates, CancellationToken.None);
    public Task<bool> Delete(string thumbprint) => DeleteAsync(thumbprint, CancellationToken.None);
    public Task<X509Certificate2Collection> Enumerate() => EnumerateAsync(CancellationToken.None);
    public Task AddCRL(X509CRL crl) => AddCRLAsync(crl, CancellationToken.None);
    public Task<bool> DeleteCRL(X509CRL crl) => DeleteCRLAsync(crl, CancellationToken.None);
    public Task<X509CRLCollection> EnumerateCRLs() => EnumerateCRLsAsync(CancellationToken.None);
    public Task<X509CRLCollection> EnumerateCRLs(X509Certificate2 issuer, bool validateUpdateTime = true) => EnumerateCRLsAsync(issuer, validateUpdateTime, CancellationToken.None);
    public Task<X509Certificate2Collection> FindByThumbprint(string thumbprint) => FindByThumbprintAsync(thumbprint, CancellationToken.None);
    public Task<StatusCode> IsRevoked(X509Certificate2 issuer, X509Certificate2 certificate) => IsRevokedAsync(issuer, certificate, CancellationToken.None);
    public Task<X509Certificate2> LoadPrivateKey(string thumbprint, string subjectName, string password) => LoadPrivateKeyAsync(thumbprint, subjectName, null, null, password, CancellationToken.None);

    // Async interface members (add default parameter values to match interface definition).
    public Task AddAsync(X509Certificate2 certificate, string password = null, CancellationToken ct = default) => _innerStore.AddAsync(certificate, password, ct);
    public Task AddRejectedAsync(X509Certificate2Collection certificates, int maxCertificates, CancellationToken ct = default) => _innerStore.AddRejectedAsync(certificates, maxCertificates, ct);
    public Task<bool> DeleteAsync(string thumbprint, CancellationToken ct = default) => _innerStore.DeleteAsync(thumbprint, ct);
    public async Task<X509Certificate2Collection> EnumerateAsync(CancellationToken ct = default)
    {
        var certificatesCollection = await _innerStore.EnumerateAsync(ct).ConfigureAwait(false);
        if (ct.IsCancellationRequested || !_innerStore.Directory.Exists) return certificatesCollection;
        foreach (var filePath in _innerStore.Directory.GetFiles('*' + CrtExtension).Select(f => f.FullName))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var certificates = new X509Certificate2Collection();
                certificates.ImportFromPemFile(filePath);
                certificatesCollection.AddRange(certificates);
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load certificate from file: {fileName}", filePath);
            }
        }
        return certificatesCollection;
    }
    public Task AddCRLAsync(X509CRL crl, CancellationToken ct = default) => _innerStore.AddCRLAsync(crl, ct);
    public Task<bool> DeleteCRLAsync(X509CRL crl, CancellationToken ct = default) => _innerStore.DeleteCRLAsync(crl, ct);
    public Task<X509CRLCollection> EnumerateCRLsAsync(CancellationToken ct = default) => _innerStore.EnumerateCRLsAsync(ct);
    public Task<X509CRLCollection> EnumerateCRLsAsync(X509Certificate2 issuer, bool validateUpdateTime = true, CancellationToken ct = default) => _innerStore.EnumerateCRLsAsync(issuer, validateUpdateTime, ct);
    public async Task<X509Certificate2Collection> FindByThumbprintAsync(string thumbprint, CancellationToken ct = default)
    {
        var certificatesCollection = await _innerStore.FindByThumbprintAsync(thumbprint, ct).ConfigureAwait(false);
        if (ct.IsCancellationRequested || !_innerStore.Directory.Exists) return certificatesCollection;
        foreach (var filePath in _innerStore.Directory.GetFiles('*' + CrtExtension).Select(f => f.FullName))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var certificates = new X509Certificate2Collection();
                certificates.ImportFromPemFile(filePath);
                foreach (var certificate in certificates.Cast<X509Certificate2>().Where(c => string.Equals(c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)))
                {
                    certificatesCollection.Add(certificate);
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load certificate from file: {fileName}", filePath);
            }
        }
        return certificatesCollection;
    }
    public Task<StatusCode> IsRevokedAsync(X509Certificate2 issuer, X509Certificate2 certificate, CancellationToken ct = default) => _innerStore.IsRevokedAsync(issuer, certificate, ct);
    public Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName, string password) => LoadPrivateKeyAsync(thumbprint, subjectName, null, null, password, CancellationToken.None);
    public async Task<X509Certificate2> LoadPrivateKeyAsync(string thumbprint, string subjectName, string applicationUri, NodeId certificateType, string password, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested) return null;
        if (!_innerStore.Directory.Exists)
        {
            return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri, certificateType, password, ct).ConfigureAwait(false);
        }
        foreach (var filePath in _innerStore.Directory.GetFiles('*' + CrtExtension).Select(f => f.FullName))
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var keyFilePath = filePath.Replace(CrtExtension, KeyExtension, StringComparison.OrdinalIgnoreCase);
                if (!File.Exists(keyFilePath)) continue;
                using var certificate = X509CertificateLoader.LoadCertificateFromFile(filePath);
                if (!MatchCertificate(certificate, thumbprint, subjectName, certificateType)) continue;
                return X509Certificate2.CreateFromPemFile(filePath, keyFilePath);
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load private key for certificate file: {fileName}", filePath);
            }
        }
        return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri, certificateType, password, ct).ConfigureAwait(false);
    }

    private static bool MatchCertificate(X509Certificate2 certificate, string thumbprint, string subjectName, NodeId certificateType)
    {
        if (certificateType == null || certificateType == ObjectTypeIds.RsaSha256ApplicationCertificateType || certificateType == ObjectTypeIds.RsaMinApplicationCertificateType || certificateType == ObjectTypeIds.ApplicationCertificateType)
        {
            if (!string.IsNullOrEmpty(thumbprint) && !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(subjectName) && !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) && (subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) || !X509Utils.ParseDistinguishedName(certificate.Subject).Any(s => s.Equals("CN=" + subjectName, StringComparison.Ordinal)))) return false;
            return X509Utils.GetRSAPublicKeySize(certificate) >= 0;
        }
        return false;
    }
}
