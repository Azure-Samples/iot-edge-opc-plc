namespace OpcPlc.Configuration;

using Opc.Ua;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Class for OPC Application configuration. Here the security relevant configuration.
/// </summary>
public partial class OpcApplicationConfiguration
{
    public OpcApplicationConfiguration()
    {
        OpcOwnCertStorePath = OpcOwnCertDirectoryStorePathDefault;
        OpcTrustedCertStorePath = OpcTrustedCertDirectoryStorePathDefault;
        OpcRejectedCertStorePath = OpcRejectedCertDirectoryStorePathDefault;
        OpcIssuerCertStorePath = OpcIssuerCertDirectoryStorePathDefault;
        OpcTrustedUserCertStorePath = OpcTrustedUserCertDirectoryStorePathDefault;
        OpcUserIssuerCertStorePath = OpcUserIssuerCertDirectoryStorePathDefault;
    }

    /// <summary>
    /// Add own certificate to trusted peer store.
    /// </summary>
    public bool TrustMyself { get; set; }

    /// <summary>
    /// Certificate store configuration for own, trusted peer, issuer and rejected stores.
    /// </summary>
    public string OpcOwnPKIRootDefault { get; } = "pki";
    public string OpcOwnCertStoreType { get; set; } = CertificateStoreType.Directory;
    public string OpcOwnCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "own");
    public string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
    public string OpcOwnCertStorePath { get; set; }
    public string OpcTrustedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "trusted");
    public string OpcTrustedCertStorePath { get; set; }

    public string OpcRejectedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "rejected");
    public string OpcRejectedCertStorePath { get; set; }

    public string OpcIssuerCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "issuer");
    public string OpcIssuerCertStorePath { get; set; }

    public string OpcTrustedUserCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "trustedUser");
    public string OpcTrustedUserCertStorePath { get; set; }

    public string OpcUserIssuerCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "issuerUser");
    public string OpcUserIssuerCertStorePath { get; set; }

    /// <summary>
    /// Accept certs of the clients automatically.
    /// </summary>
    public bool AutoAcceptCerts { get; set; }

    /// <summary>
    /// Don't reject chain validation with CA certs with unknown revocation status,
    /// e.g. when the CRL is not available or the OCSP provider is offline.
    /// The default value is <see langword="false"/>, so rejection is enabled.
    /// </summary>
    public bool DontRejectUnknownRevocationStatus { get; set; }

    /// <summary>
    /// Show CSR information during startup.
    /// </summary>
    public bool ShowCreateSigningRequestInfo { get; set; }

    /// <summary>
    /// Update application certificate.
    /// </summary>
    public string NewCertificateBase64String { get; set; }
    public string NewCertificateFileName { get; set; }
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// If there is no application cert installed we need to install the private key as well.
    /// </summary>
    public string PrivateKeyBase64String { get; set; }
    public string PrivateKeyFileName { get; set; }

    /// <summary>
    /// Issuer certificates to add.
    /// </summary>
    public List<string> IssuerCertificateBase64Strings { get; set; }
    public List<string> IssuerCertificateFileNames { get; set; }

    /// <summary>
    /// Trusted certificates to add.
    /// </summary>
    public List<string> TrustedCertificateBase64Strings { get; set; }
    public List<string> TrustedCertificateFileNames { get; set; }

    /// <summary>
    /// Trusted user certificates to add for user authentication.
    /// </summary>
    public List<string> TrustedUserCertificateBase64Strings { get; set; }
    public List<string> TrustedUserCertificateFileNames { get; set; }

    /// <summary>
    /// User issuer certificates to add for user authentication.
    /// </summary>
    public List<string> UserIssuerCertificateBase64Strings { get; set; }
    public List<string> UserIssuerCertificateFileNames { get; set; }

    /// <summary>
    /// CRL to update/install.
    /// </summary>
    public string CrlFileName { get; set; }
    public string CrlBase64String { get; set; }

    /// <summary>
    /// Thumbprint of certificates to delete.
    /// </summary>
    public List<string> ThumbprintsToRemove { get; set; }

    /// <summary>
    /// Additional certificate DNS names.
    /// </summary>
    public List<string> DnsNames { get; set; } = new();
}
