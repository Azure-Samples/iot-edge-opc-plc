namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Configuration;
using OpcPlc.Certs;

public static class PlcSecurityConfiguration
{
    public static ArrayOf<CertificateIdentifier> CreateApplicationCertificates(OpcPlcConfiguration config)
    {
        return
        [
            new CertificateIdentifier
            {
                StoreType = config.OpcUa.OpcOwnCertStoreType,
                StorePath = GetStorePathPrefix(config.OpcUa.OpcOwnCertStoreType) + config.OpcUa.OpcOwnCertStorePath,
                SubjectName = "CN=" + config.ProgramName,
                CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
            }
        ];
    }

    public static IApplicationConfigurationBuilderSecurityOptions Configure(
        IApplicationConfigurationBuilderSecurityOptions builder, OpcPlcConfiguration config)
    {
        return builder.SetApplicationCertificates(CreateApplicationCertificates(config))
            .SetAutoAcceptUntrustedCertificates(config.OpcUa.AutoAcceptCerts)
            .SetRejectUnknownRevocationStatus(!config.OpcUa.DontRejectUnknownRevocationStatus)
            .SetRejectSHA1SignedCertificates(config.OpcUa.RejectSHA1SignedCertificates)
            .SetMinimumCertificateKeySize(config.OpcUa.MinimumCertificateKeySize)
            .SetAddAppCertToTrustedStore(config.OpcUa.TrustMyself);
    }

    public static void ConfigureStores(SecurityConfiguration security, OpcPlcConfiguration config)
    {
        string prefix = GetStorePathPrefix(config.OpcUa.OpcOwnCertStoreType);
        string trustStoreType = prefix.Length == 0 ? CertificateStoreType.Directory : config.OpcUa.OpcOwnCertStoreType;
        security.ApplicationCertificates = CreateApplicationCertificates(config);
        SetStore(security.TrustedIssuerCertificates, trustStoreType, prefix + config.OpcUa.OpcIssuerCertStorePath);
        SetStore(security.TrustedPeerCertificates, trustStoreType, prefix + config.OpcUa.OpcTrustedCertStorePath);
        SetStore(security.TrustedUserCertificates, trustStoreType, prefix + config.OpcUa.OpcTrustedUserCertStorePath);
        SetStore(security.UserIssuerCertificates, trustStoreType, prefix + config.OpcUa.OpcUserIssuerCertStorePath);
        SetStore(security.RejectedCertificateStore, trustStoreType, prefix + config.OpcUa.OpcRejectedCertStorePath);
    }

    private static void SetStore(CertificateStoreIdentifier store, string storeType, string storePath)
    {
        store.StoreType = storeType;
        store.StorePath = storePath;
    }

    private static string GetStorePathPrefix(string storeType) => storeType switch
    {
        FlatDirectoryCertificateStore.StoreTypeName => FlatDirectoryCertificateStore.StoreTypePrefix,
        KubernetesSecretCertificateStore.StoreTypeName => KubernetesSecretCertificateStore.StoreTypePrefix,
        _ => string.Empty
    };
}