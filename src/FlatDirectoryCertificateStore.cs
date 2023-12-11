namespace OpcPlc.Certs;

using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

/// <summary>
/// Flat directory certificate store that does not have internal
/// hierarchy with certs/crl/private subdirectories.
/// </summary>
public sealed class FlatDirectoryCertificateStore : ICertificateStore
{
    private const string CrtExtension = ".crt";
    private const string KeyExtension = ".key";

    private readonly DirectoryCertificateStore _innerStore;

    /// <summary>
    /// Identifier for flat directory certificate store.
    /// </summary>
    public const string StoreTypeName = "FlatDirectory";

    /// <summary>
    /// Initializes a new instance of the <see cref="FlatDirectoryCertificateStore"/> class.
    /// </summary>
    public FlatDirectoryCertificateStore()
    {
        _innerStore = new DirectoryCertificateStore(noSubDirs: true);
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
    public void Dispose()
    {
        _innerStore.Dispose();
    }

    /// <inheritdoc/>
    public void Open(string location, bool noPrivateKeys = true)
    {
        ArgumentNullException.ThrowIfNull(location);
        _innerStore.Open(location, noPrivateKeys);
    }

    /// <inheritdoc/>
    public void Close()
    {
        _innerStore.Close();
    }

    /// <inheritdoc/>
    public Task Add(X509Certificate2 certificate, string password = null)
    {
        return _innerStore.Add(certificate, password);
    }

    /// <inheritdoc/>
    public Task<bool> Delete(string thumbprint)
    {
        return _innerStore.Delete(thumbprint);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2Collection> Enumerate()
    {
        var certificatesCollection = await _innerStore.Enumerate().ConfigureAwait(false);
        if (!_innerStore.Directory.Exists)
        {
            return certificatesCollection;
        }

        foreach (FileInfo file in _innerStore.Directory.GetFiles('*' + CrtExtension))
        {
            try
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                certificates.ImportFromPemFile(file.FullName);
                certificatesCollection.AddRange(certificates);
                foreach (X509Certificate2 certificate in certificates)
                {
                    Utils.LogInfo("Enumerate certificates - certificate added {0}", certificate.Thumbprint);
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load certificate from file: {0}", file.FullName);
            }
        }

        return certificatesCollection;
    }

    /// <inheritdoc/>
    public Task AddCRL(X509CRL crl)
    {
        return _innerStore.AddCRL(crl);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteCRL(X509CRL crl)
    {
        return _innerStore.DeleteCRL(crl);
    }

    /// <inheritdoc/>
    public Task<X509CRLCollection> EnumerateCRLs()
    {
        return _innerStore.EnumerateCRLs();
    }

    /// <inheritdoc/>
    public Task<X509CRLCollection> EnumerateCRLs(X509Certificate2 issuer, bool validateUpdateTime = true)
    {
        return _innerStore.EnumerateCRLs(issuer, validateUpdateTime);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2Collection> FindByThumbprint(string thumbprint)
    {
        var certificatesCollection = await _innerStore.FindByThumbprint(thumbprint).ConfigureAwait(false);

        if (!_innerStore.Directory.Exists)
        {
            return certificatesCollection;
        }

        foreach (FileInfo file in _innerStore.Directory.GetFiles('*' + CrtExtension))
        {
            try
            {
                X509Certificate2Collection certificates = new X509Certificate2Collection();
                certificates.ImportFromPemFile(file.FullName);
                foreach (X509Certificate2 certificate in certificates)
                {
                    if (string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        Utils.LogInfo("Find by thumbprint: {0} - found", thumbprint);
                        certificatesCollection.Add(certificate);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load certificate from file: {0}", file.FullName);
            }
        }

        return certificatesCollection;
    }

    /// <inheritdoc/>
    public Task<StatusCode> IsRevoked(X509Certificate2 issuer, X509Certificate2 certificate)
    {
        return _innerStore.IsRevoked(issuer, certificate);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> LoadPrivateKey(string thumbprint, string subjectName, string password)
    {
        if (!_innerStore.Directory.Exists)
        {
            return await _innerStore.LoadPrivateKey(thumbprint, subjectName, password).ConfigureAwait(false);
        }

        foreach (FileInfo file in _innerStore.Directory.GetFiles('*' + CrtExtension))
        {
            try
            {
                FileInfo keyFile = new FileInfo(file.FullName.Replace(CrtExtension, KeyExtension, StringComparison.OrdinalIgnoreCase));
                if (keyFile.Exists)
                {
                    using X509Certificate2 certificate = new X509Certificate2(file.FullName);
                    if (!MatchCertificate(certificate, thumbprint, subjectName))
                    {
                        continue;
                    }

                    X509Certificate2 privateKeyCertificate = X509Certificate2.CreateFromPemFile(file.FullName, keyFile.FullName);

                    Utils.LogInfo("Loading private key succeeded for {0} - {1}", thumbprint, subjectName);
                    return privateKeyCertificate;
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Could not load private key for certificate file: {0}", file.FullName);
            }
        }

        return await _innerStore.LoadPrivateKey(thumbprint, subjectName, password).ConfigureAwait(false);
    }

    private bool MatchCertificate(X509Certificate2 certificate, string thumbprint, string subjectName)
    {
        if (!string.IsNullOrEmpty(thumbprint) &&
            !string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(subjectName) &&
            !X509Utils.CompareDistinguishedName(subjectName, certificate.Subject) &&
            (
                subjectName.Contains('=', StringComparison.OrdinalIgnoreCase) ||
                !X509Utils.ParseDistinguishedName(certificate.Subject).Any(s => s.Equals("CN=" + subjectName, StringComparison.Ordinal))))
        {
            return false;
        }

        // skip if not RSA certificate
        if (X509Utils.GetRSAPublicKeySize(certificate) < 0)
        {
            return false;
        }

        return true;
    }
}
