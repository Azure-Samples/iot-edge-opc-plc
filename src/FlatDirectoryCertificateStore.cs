namespace OpcPlc.Certs;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Flat directory certificate store that does not have internal
/// hierarchy with certs/crl/private subdirectories.
/// </summary>
public sealed partial class FlatDirectoryCertificateStore : ICertificateStore
{
    private const string CrtExtension = ".crt";
    private const string KeyExtension = ".key";

    private readonly DirectoryCertificateStore _innerStore;
    private readonly ILogger _logger;
    private readonly Func<string, CancellationToken, Task<byte[]>> _readFileAsync;

    /// <summary>
    /// Identifier for flat directory certificate store.
    /// </summary>
    public const string StoreTypeName = "FlatDirectory";

    /// <summary>
    /// Prefix for flat directory certificate store.
    /// </summary>
    public const string StoreTypePrefix = $"{StoreTypeName}:";

    /// <summary>
    /// Initializes a new instance of the <see cref="FlatDirectoryCertificateStore"/> class.
    /// </summary>
    public FlatDirectoryCertificateStore(ILogger logger, ITelemetryContext telemetry)
        : this(logger, telemetry, File.ReadAllBytesAsync)
    {
    }

    internal FlatDirectoryCertificateStore(
        ILogger logger, ITelemetryContext telemetry, Func<string, CancellationToken, Task<byte[]>> readFileAsync)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _innerStore = new DirectoryCertificateStore(noSubDirs: true, telemetry);
        _readFileAsync = readFileAsync ?? throw new ArgumentNullException(nameof(readFileAsync));
    }

    /// <inheritdoc/>
    public string StoreType => StoreTypeName;

    /// <inheritdoc/>
    public string StorePath => _innerStore.StorePath;

    /// <inheritdoc/>
    public bool SupportsLoadPrivateKey => _innerStore.SupportsLoadPrivateKey;

    /// <inheritdoc/>
    public bool SupportsCRLs => _innerStore.SupportsCRLs;

    /// <inheritdoc/>
    public bool NoPrivateKeys => _innerStore.NoPrivateKeys;

    public void Dispose() => _innerStore.Dispose();

    public void Open(string location, bool noPrivateKeys = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(location);
        if (!location.StartsWith(StoreTypePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected argument {nameof(location)} starting with {StoreTypePrefix}",
                nameof(location));
        }
        _innerStore.Open(location.Substring(StoreTypePrefix.Length), noPrivateKeys);
    }

    public void Close() => _innerStore.Close();

    public Task AddAsync(Certificate certificate, char[] password = null, CancellationToken ct = default) => _innerStore.AddAsync(certificate, password, ct);

    public Task AddRejectedAsync(CertificateCollection certificates, int maxCertificates, CancellationToken ct = default) => _innerStore.AddRejectedAsync(certificates, maxCertificates, ct);

    public Task<bool> DeleteAsync(string thumbprint, CancellationToken ct = default) => _innerStore.DeleteAsync(thumbprint, ct);

    public async Task<CertificateCollection> EnumerateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var certificatesCollection = await _innerStore.EnumerateAsync(ct).ConfigureAwait(false);
        return await LoadPemCertificatesAsync(certificatesCollection, null, false, ct).ConfigureAwait(false);
    }

    public Task AddCRLAsync(X509CRL crl, CancellationToken ct = default) => _innerStore.AddCRLAsync(crl, ct);

    public Task<bool> DeleteCRLAsync(X509CRL crl, CancellationToken ct = default) => _innerStore.DeleteCRLAsync(crl, ct);

    public Task<X509CRLCollection> EnumerateCRLsAsync(CancellationToken ct = default) => _innerStore.EnumerateCRLsAsync(ct);

    public Task<X509CRLCollection> EnumerateCRLsAsync(Certificate issuer, bool validateUpdateTime = true, CancellationToken ct = default) => _innerStore.EnumerateCRLsAsync(issuer, validateUpdateTime, ct);

    public async Task<CertificateCollection> FindByThumbprintAsync(string thumbprint, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var certificatesCollection = await _innerStore.FindByThumbprintAsync(thumbprint, ct).ConfigureAwait(false);
        return await LoadPemCertificatesAsync(certificatesCollection, thumbprint, true, ct).ConfigureAwait(false);
    }

    private async Task<CertificateCollection> LoadPemCertificatesAsync(
        CertificateCollection certificatesCollection, string thumbprint, bool filterByThumbprint, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            if (!_innerStore.Directory.Exists)
            {
                return certificatesCollection;
            }

            foreach (var filePath in _innerStore.Directory.GetFiles('*' + CrtExtension).Select(f => f.FullName))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var certificates = CertificateCollection.From(
                        PEMReader.ImportPublicKeysFromPEM(await _readFileAsync(filePath, ct).ConfigureAwait(false)));
                    ct.ThrowIfCancellationRequested();
                    foreach (var certificate in certificates)
                    {
                        if (!filterByThumbprint ||
                            string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            certificatesCollection.Add(certificate);
                        }
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LogCouldNotLoadCertificate(e, filePath);
                }
            }
            ct.ThrowIfCancellationRequested();
            return certificatesCollection;
        }
        catch
        {
            certificatesCollection.Dispose();
            throw;
        }
    }

    public Task<StatusCode> IsRevokedAsync(Certificate issuer, Certificate certificate, CancellationToken ct = default) => _innerStore.IsRevokedAsync(issuer, certificate, ct);

    public Task<Certificate> LoadPrivateKeyAsync(string thumbprint, string subjectName, string password, CancellationToken ct = default)
        => LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri: null, certificateType: default, password?.ToCharArray(), ct);

    public async Task<Certificate> LoadPrivateKeyAsync(string thumbprint, string subjectName, string applicationUri, NodeId certificateType, char[] password = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (NoPrivateKeys)
        {
            return null;
        }

        if (!_innerStore.Directory.Exists)
        {
            return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri, certificateType, password, ct).ConfigureAwait(false);
        }

        foreach (var filePath in _innerStore.Directory.GetFiles('*' + CrtExtension).Select(f => f.FullName))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var keyFilePath = filePath.Replace(CrtExtension, KeyExtension, StringComparison.OrdinalIgnoreCase);
                if (!File.Exists(keyFilePath)) continue;
                using var certificate = new Certificate(filePath);
                if (!MatchCertificate(certificate, thumbprint, subjectName, applicationUri, certificateType)) continue;
                return Certificate.From(X509Certificate2.CreateFromPemFile(filePath, keyFilePath));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogCouldNotLoadPrivateKey(e, filePath);
            }
        }

        return await _innerStore.LoadPrivateKeyAsync(thumbprint, subjectName, applicationUri, certificateType, password, ct).ConfigureAwait(false);
    }

    private static bool MatchCertificate(
        Certificate certificate, string thumbprint, string subjectName, string applicationUri, NodeId certificateType)
    {
        if (certificateType.IsNull || certificateType == ObjectTypeIds.RsaSha256ApplicationCertificateType || certificateType == ObjectTypeIds.RsaMinApplicationCertificateType || certificateType == ObjectTypeIds.ApplicationCertificateType)
        {
            if (!string.IsNullOrEmpty(thumbprint) && !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(subjectName) && !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) && (subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) || !X509Utils.ParseDistinguishedName(certificate.Subject).Any(s => s.Equals("CN=" + subjectName, StringComparison.Ordinal)))) return false;
            if (!string.IsNullOrEmpty(applicationUri) &&
                !X509Utils.CompareApplicationUriWithCertificate(certificate, applicationUri)) return false;
            return X509Utils.GetRSAPublicKeySize(certificate) >= 0;
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not load certificate from file {FilePath}")]
    partial void LogCouldNotLoadCertificate(Exception exception, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not load private key for certificate file {FilePath}")]
    partial void LogCouldNotLoadPrivateKey(Exception exception, string filePath);
}
