namespace OpcPlc.Configuration;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public partial class CertificateManagementService(
    OpcPlcConfiguration config,
    ILogger logger,
    ITelemetryContext telemetryContext,
    IEnumerable<ICertificateStoreProvider> certificateStoreProviders = null)
{
    private readonly OpcPlcConfiguration _config = config;
    private readonly ILogger _logger = logger;
    private readonly ITelemetryContext _telemetryContext = telemetryContext ?? throw new ArgumentNullException(nameof(telemetryContext));
    private readonly IEnumerable<ICertificateStoreProvider> _certificateStoreProviders = certificateStoreProviders;

    /// <summary>
    /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store.
    /// </summary>
    public Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
    {
        return new CertificateTrustListOperations(_config.OpcUa.ApplicationConfiguration.CertificateManager, _logger)
            .RemovePeerCertificatesAsync(thumbprintsToRemove);
    }

    /// <summary>
    /// Validate and add certificates to the trusted issuer or trusted peer store.
    /// </summary>
    public Task<bool> AddCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        return new CertificateTrustListOperations(_config.OpcUa.ApplicationConfiguration.CertificateManager, _logger)
            .AddAsync(certificateBase64Strings, certificateFileNames, TrustListIdentifier.Peers, issuerCertificate);
    }

    /// <summary>
    /// Validate and add certificates to the trusted user or user issuer store.
    /// </summary>
    public Task<bool> AddUserCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        return new CertificateTrustListOperations(_config.OpcUa.ApplicationConfiguration.CertificateManager, _logger)
            .AddAsync(certificateBase64Strings, certificateFileNames, TrustListIdentifier.Users, issuerCertificate);
    }

    /// <summary>
    /// Update the CRL in the corresponding store.
    /// </summary>
    public Task<bool> UpdateCrlAsync(string newCrlBase64String, string newCrlFileName)
    {
        ICertificateManager manager = _config.OpcUa.ApplicationConfiguration.CertificateManager;
        return new CertificateTrustListOperations(manager, _logger)
            .ReplaceCrlAsync(manager, newCrlBase64String, newCrlFileName);
    }

    /// <summary>
    /// Validate and update the application.
    /// </summary>
    public async Task<bool> UpdateApplicationCertificateAsync(
        string newCertificateBase64String,
        string newCertificateFileName,
        string certificatePassword,
        string privateKeyBase64String,
        string privateKeyFileName)
    {
        if (string.IsNullOrEmpty(newCertificateFileName) && string.IsNullOrEmpty(newCertificateBase64String))
        {
            LogNoNewCertificateData();
            return false;
        }

        byte[] privateKey = null;
        try
        {
            byte[] certificateData = string.IsNullOrEmpty(newCertificateFileName)
                ? Convert.FromBase64String(newCertificateBase64String)
                : await File.ReadAllBytesAsync(newCertificateFileName).ConfigureAwait(false);
            using var newCertificate = new Certificate(certificateData);
            if (!string.IsNullOrEmpty(privateKeyBase64String))
            {
                privateKey = Convert.FromBase64String(privateKeyBase64String);
            }
            if (!string.IsNullOrEmpty(privateKeyFileName))
            {
                if (privateKey is not null)
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
                privateKey = await File.ReadAllBytesAsync(privateKeyFileName).ConfigureAwait(false);
            }
            ICertificateManager manager = _config.OpcUa.ApplicationConfiguration.CertificateManager;
            using CertificateEntry current = manager.AcquireApplicationCertificateByType(
                ObjectTypeIds.RsaSha256ApplicationCertificateType);
            if (current is not null && !X509Utils.CompareDistinguishedName(
                current.Certificate.Subject, newCertificate.Subject))
            {
                LogSubjectNameMismatch(newCertificate.Subject, current.Certificate.Subject);
                return false;
            }

            if (!X509Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
            {
                await VerifyReplacementTrustAsync(newCertificate, manager).ConfigureAwait(false);
                using var chain = new CertificateCollection { newCertificate };
                var validation = await manager.ValidateAsync(chain, TrustListIdentifier.Peers,
                    new Opc.Ua.Security.Certificates.CertificateValidationOptions
                    {
                        AutoAcceptUntrustedCertificates = false,
                        AcceptError = (_, error) => error.StatusCode == StatusCodes.BadCertificateUntrusted
                    }, CancellationToken.None).ConfigureAwait(false);
                validation.ThrowIfInvalid();
            }

            using Certificate candidate = CombinePrivateKey(newCertificate, privateKey, certificatePassword, current);
            LogActivatingNewAppCert(candidate.Thumbprint);
            await ApplicationCertificateLifecycle.PersistAndActivateAsync(_config.OpcUa.ApplicationConfiguration,
                candidate, _certificateStoreProviders, _telemetryContext).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailedToActivateNewAppCert(exception);
            return false;
        }
        finally
        {
            if (privateKey is not null)
            {
                CryptographicOperations.ZeroMemory(privateKey);
            }
        }
    }

    private static async Task VerifyReplacementTrustAsync(Certificate certificate, ITrustListFileAccess manager)
    {
        using TrustListData trust = await manager.ReadTrustListAsync(TrustListIdentifier.Peers,
            TrustListMasks.TrustedCertificates | TrustListMasks.IssuerCertificates).ConfigureAwait(false);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        var owned = new List<X509Certificate2>();
        try
        {
            foreach (CertificateCollection certificates in
                new[] { trust.TrustedCertificates, trust.IssuerCertificates })
            {
                foreach (Certificate trusted in certificates)
                {
                    X509Certificate2 publicCertificate = X509CertificateLoader.LoadCertificate(trusted.RawData);
                    owned.Add(publicCertificate);
                    chain.ChainPolicy.CustomTrustStore.Add(publicCertificate);
                    chain.ChainPolicy.ExtraStore.Add(publicCertificate);
                }
            }
            using X509Certificate2 candidate = X509CertificateLoader.LoadCertificate(certificate.RawData);
            if (!chain.Build(candidate))
            {
                throw new InvalidOperationException(
                    "Application certificate does not chain to a configured trust anchor.");
            }
        }
        finally
        {
            foreach (X509Certificate2 item in owned)
            {
                item.Dispose();
            }
        }
    }

    private Certificate CombinePrivateKey(
        Certificate certificate, byte[] privateKey, string password, CertificateEntry current)
    {
        if (privateKey is not null)
        {
            try
            {
                using Certificate imported = X509Utils.CreateCertificateFromPKCS12(privateKey, password);
                return DefaultCertificateFactory.Instance.CreateWithPrivateKey(certificate, imported);
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException
                or NotSupportedException)
            {
                LogCertFileNotFormat(exception, "PFX");
            }
            try
            {
                return DefaultCertificateFactory.Instance.CreateWithPEMPrivateKey(certificate, privateKey, password);
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException
                or NotSupportedException)
            {
                LogCertFileNotFormat(exception, "PEM");
            }
        }

        if (current is not null && current.Certificate.HasPrivateKey)
        {
            return DefaultCertificateFactory.Instance.CreateWithPrivateKey(certificate, current.Certificate);
        }
        throw new InvalidOperationException("No matching application certificate private key was provided or loaded.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no thumbprint specified for certificates to remove. Please check your command line options.")]
    partial void LogNoThumbprintSpecified();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to remove certificate(s) from trusted peer and trusted issuer store")]
    partial void LogStartingRemoveCertificates();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove certificate with thumbprint '{Thumbprint}' from the {StoreName} store")]
    partial void LogFailedToRemoveCertificate(string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed certificate with thumbprint '{Thumbprint}' from the {StoreName} store")]
    partial void LogRemovedCertificate(string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while trying to remove certificate(s) from the {StoreName} store")]
    partial void LogErrorRemovingCertificates(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no certificate provided. Please check your command line options.")]
    partial void LogNoCertificateProvided();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to add certificate(s) to the {StoreType} store")]
    partial void LogStartingAddCertificates(string storeType);

    [LoggerMessage(Level = LogLevel.Error, Message = "The provided string '{PartialString}...' is not a valid base64 string")]
    partial void LogInvalidBase64String(string partialString);

    [LoggerMessage(Level = LogLevel.Error, Message = "The {CertType} certificate data is invalid. Please check your command line options")]
    partial void LogInvalidCertificateData(Exception exception, string certType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the {StoreName} store")]
    partial void LogCertificateAddedToStore(string subjectName, string thumbprint, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certificate '{SubjectName}' already exists in {StoreName} store")]
    partial void LogCertificateAlreadyExists(Exception exception, string subjectName, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while adding a certificate to the {StoreName} store")]
    partial void LogErrorAddingCertificate(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no CRL specified. Please check your command line options")]
    partial void LogNoCrlSpecified();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to update the current CRL")]
    partial void LogStartingUpdateCrl();

    [LoggerMessage(Level = LogLevel.Error, Message = "The new CRL data is invalid")]
    partial void LogInvalidCrlData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Remove the current CRL from the {StoreName} store")]
    partial void LogRemoveCrlFromStore(string storeName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove CRL issued by '{Issuer}' from the {StoreName} store")]
    partial void LogFailedToRemoveCrl(string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while removing the current CRL issued by '{Issuer}' from the {StoreName} store")]
    partial void LogErrorRemovingCrl(Exception exception, string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while removing the current CRL from the {StoreName} store")]
    partial void LogErrorRemovingCrlFromStore(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Information, Message = "The new CRL issued by '{Issuer}' was added to the {StoreName} store")]
    partial void LogCrlAddedToStore(string issuer, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while adding the new CRL to the {StoreName} store")]
    partial void LogErrorAddingCrlToStore(Exception exception, string storeName);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no new application certificate data provided. Please check your command line options.")]
    partial void LogNoNewCertificateData();

    [LoggerMessage(Level = LogLevel.Error, Message = "The new application certificate data is invalid")]
    partial void LogInvalidNewCertificateData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Start updating the current application certificate")]
    partial void LogStartUpdatingAppCert();

    [LoggerMessage(Level = LogLevel.Error, Message = "The private key data is invalid")]
    partial void LogInvalidPrivateKeyData(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The current application certificate has SubjectName '{CurrentSubjectName}' and thumbprint '{CurrentThumbprint}'")]
    partial void LogCurrentAppCertInfo(string currentSubjectName, string currentThumbprint);

    [LoggerMessage(Level = LogLevel.Information, Message = "There is no existing application certificate")]
    partial void LogNoExistingAppCert();

    [LoggerMessage(Level = LogLevel.Error, Message = "The SubjectName '{NewSubjectName}' of the new certificate doesn't match the current certificates SubjectName '{CurrentSubjectName}'")]
    partial void LogSubjectNameMismatch(string newSubjectName, string currentSubjectName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to verify integrity of the new certificate and the trusted issuer list")]
    partial void LogFailedToVerifyCertIntegrity(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The private key for the new certificate was passed in using {Format} format")]
    partial void LogPrivateKeyFormat(string format);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Certificate file is not {Format}")]
    partial void LogCertFileNotFormat(Exception exception, string format);

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format")]
    partial void LogNoExistingCertForPrivateKey();

    [LoggerMessage(Level = LogLevel.Error, Message = "The provided format of the private key is not supported (must be PEM or PFX) or the provided cert password is wrong")]
    partial void LogUnsupportedPrivateKeyFormat();

    [LoggerMessage(Level = LogLevel.Error, Message = "There is no application certificate we can update and for the new application certificate there was not usable private key (must be PEM or PFX format) provided or the provided cert password is wrong")]
    partial void LogNoAppCertAndNoUsablePrivateKey();

    [LoggerMessage(Level = LogLevel.Information, Message = "Remove the existing application certificate")]
    partial void LogRemoveExistingAppCert();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Removing the existing application certificate with thumbprint '{CurrentThumbprint}' failed")]
    partial void LogRemovingExistingCertFailed(string currentThumbprint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to remove the existing application certificate from the ApplicationCertificate store")]
    partial void LogFailedToRemoveExistingCert(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "The new application certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the application certificate store")]
    partial void LogNewAppCertAdded(string subjectName, string thumbprint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add the new application certificate to the application certificate store")]
    partial void LogFailedToAddNewAppCert(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Activating the new application certificate with thumbprint '{NewThumbprint}'")]
    partial void LogActivatingNewAppCert(string newThumbprint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to activate the new application certificate")]
    partial void LogFailedToActivateNewAppCert(Exception exception);
}
