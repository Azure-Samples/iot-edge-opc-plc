namespace OpcPlc.Configuration;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public partial class OpcUaAppConfigFactory(
    OpcPlcConfiguration config,
    ILogger logger,
    ILoggerFactory loggerFactory,
    ITelemetryContext telemetryContext,
    IKubernetesSecretStoreClientFactory kubernetesSecretStoreClientFactory = null)
{
    private readonly OpcPlcConfiguration _config = config;
    private readonly IKubernetesSecretStoreClientFactory _kubernetesSecretStoreClientFactory = kubernetesSecretStoreClientFactory ?? new KubernetesSecretStoreClientFactory(config.OpcUa.OpcKubernetesKubeConfigFilePath, loggerFactory.CreateLogger<KubernetesSecretStoreClient>());
    private readonly ILogger _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ITelemetryContext _telemetryContext = telemetryContext ?? throw new ArgumentNullException(nameof(telemetryContext));
    private readonly CertificateManagementService _certificateManagementService = new(config, logger, telemetryContext);

    /// OpcApplicationConfiguration

    /// <summary>
    /// Configures all OPC stack settings.
    /// </summary>
    public async Task<ApplicationConfiguration> ConfigureAsync()
    {
        // instead of using a configuration XML file, configure everything programmatically
        var application = new ApplicationInstance(_telemetryContext) {
            ApplicationName = _config.ProgramName, // Name in the certificate, e.g. OpcPlc.
            ApplicationType = ApplicationType.Server,
        };

        var transportQuotas = new TransportQuotas {
            MaxStringLength = _config.OpcUa.OpcMaxStringLength,
            MaxMessageSize = 4 * 1024 * 1024, // 4 MB.
            MaxByteStringLength = 4 * 1024 * 1024, // 4 MB.
            ChannelLifetime = 60_000, // 60 s.
        };

        var operationLimits = new OperationLimits() {
            MaxMonitoredItemsPerCall = 2500,
            MaxNodesPerBrowse = 2500,
            MaxNodesPerHistoryReadData = 1000,
            MaxNodesPerHistoryReadEvents = 1000,
            MaxNodesPerHistoryUpdateData = 1000,
            MaxNodesPerHistoryUpdateEvents = 1000,
            MaxNodesPerMethodCall = 1000,
            MaxNodesPerNodeManagement = 1000,
            MaxNodesPerRead = 2500,
            MaxNodesPerWrite = 1000,
            MaxNodesPerRegisterNodes = 1000,
            MaxNodesPerTranslateBrowsePathsToNodeIds = 1000,
        };

        var alternateBaseAddresses = from dnsName in _config.OpcUa.DnsNames
                                     select $"opc.tcp://{dnsName}:{_config.OpcUa.ServerPort}{_config.OpcUa.ServerPath}";

        // When no DNS names are configured, use the hostname as alternative name.
        if (!alternateBaseAddresses.Any())
        {
            try
            {
                alternateBaseAddresses = alternateBaseAddresses.Append($"opc.tcp://{Utils.GetHostName().ToLowerInvariant()}:{_config.OpcUa.ServerPort}{_config.OpcUa.ServerPath}");
            }
            catch (Exception ex)
            {
                LogCouldNotGetHostname(ex);
            }
        }

        LogAlternateBaseAddresses(alternateBaseAddresses);

        string applicationUri = $"urn:{_config.ProgramName}:{_config.OpcUa.HostnameLabel}{(string.IsNullOrEmpty(_config.OpcUa.ServerPath)
             ? string.Empty
             : (_config.OpcUa.ServerPath.StartsWith('/') ? string.Empty : ":"))}{_config.OpcUa.ServerPath.Replace('/', ':')}";

        // configure OPC UA server
        var serverBuilder = application.Build(applicationUri, _config.OpcUa.ProductUri)
                .SetTransportQuotas(transportQuotas)
                .AsServer(baseAddresses: [
                    $"opc.tcp://{_config.OpcUa.Hostname}:{_config.OpcUa.ServerPort}{_config.OpcUa.ServerPath}",
                ],
                alternateBaseAddresses.ToArray())
                .AddSignAndEncryptPolicies()
                .AddSignPolicies();

        // use backdoor to access app config used by builder
        _config.OpcUa.ApplicationConfiguration = application.ApplicationConfiguration;

        if (_config.OpcUa.EnableUnsecureTransport)
        {
            serverBuilder.AddUnsecurePolicyNone();
        }

        ConfigureUserTokenPolicies(serverBuilder);

        // Support larger number of nodes.
        var securityBuilder = serverBuilder
            .SetMaxMessageQueueSize(_config.OpcUa.MaxMessageQueueSize)
            .SetMaxNotificationQueueSize(_config.OpcUa.MaxNotificationQueueSize)
            .SetMaxNotificationsPerPublish(_config.OpcUa.MaxNotificationsPerPublish)
            .SetMaxPublishRequestCount(_config.OpcUa.MaxPublishRequestPerSession)
            .SetMaxRequestThreadCount(_config.OpcUa.MaxRequestThreadCount)
            // LDS registration interval.
            .SetMaxRegistrationInterval(_config.OpcUa.LdsRegistrationInterval)
            // Enable auditing events and diagnostics.
            .SetDiagnosticsEnabled(true)
            .SetAuditingEnabled(false)
            // Set the server capabilities.
            .SetMaxSessionCount(_config.OpcUa.MaxSessionCount)
            .SetMaxSessionTimeout(_config.OpcUa.MaxSessionTimeout)
            .SetMaxSubscriptionCount(_config.OpcUa.MaxSubscriptionCount)
            .SetMaxQueuedRequestCount(_config.OpcUa.MaxQueuedRequestCount)
            .SetOperationLimits(operationLimits)
            // Ignore max channel count.
            // TODO: Remove this when the OPC UA stack supports more than 100 channels.
            .SetMaxChannelCount(0);

        // Security configuration.
        _config.OpcUa.ApplicationConfiguration = await InitApplicationSecurityAsync(securityBuilder).ConfigureAwait(false);

        foreach (var policy in _config.OpcUa.ApplicationConfiguration.ServerConfiguration.SecurityPolicies)
        {
            LogAddedSecurityPolicy(policy.SecurityPolicyUri, policy.SecurityMode);

            if (policy.SecurityMode == MessageSecurityMode.None)
            {
                LogSecurityPolicyNoneRisk();
            }
        }

        LogLdsRegistrationInterval(_config.OpcUa.LdsRegistrationInterval);

        // Determine if custom certificate was provided via command line
        bool customCertificateProvided = !string.IsNullOrEmpty(_config.OpcUa.NewCertificateBase64String) ||
                                         !string.IsNullOrEmpty(_config.OpcUa.NewCertificateFileName);

        // log certificate status - refetch after InitApplicationSecurityAsync
        var certificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
        if (certificate == null)
        {
            LogNoExistingCertificateFound(CertificateFactory.DefaultLifeTime, CertificateFactory.DefaultKeySize, CertificateFactory.DefaultHashSize);
        }
        else
        {
            LogCertificateFound(certificate.Thumbprint);
        }

        // Check the certificate, create new self-signed certificate if necessary (but not if custom cert was provided)
        bool isCertValid;
        if (customCertificateProvided)
        {
            // Custom certificate was provided, just validate it without creating a new one
            isCertValid = certificate != null;
            if (!isCertValid)
            {
                throw new Exception("Custom application certificate was provided but could not be loaded.");
            }
            LogUsingCustomCertificate();
        }
        else
        {
            // No custom certificate, let the system create a self-signed one if needed
            isCertValid = await application.CheckApplicationInstanceCertificatesAsync(silent: true, lifeTimeInMonths: CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
            if (!isCertValid)
            {
                throw new Exception("Application certificate invalid.");
            }

            if (certificate == null)
            {
                certificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
                LogCertificateCreated(certificate.Thumbprint);
            }
        }

        LogApplicationCertificateInfo(
            _config.OpcUa.ApplicationConfiguration.ApplicationUri,
            _config.OpcUa.ApplicationConfiguration.ApplicationName,
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate.Subject);

        // show CreateSigningRequest data
        if (_config.OpcUa.ShowCreateSigningRequestInfo)
        {
            await ShowCreateSigningRequestInformationAsync(_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate).ConfigureAwait(false);
        }

        // show certificate store information
        await ShowCertificateStoreInformationAsync().ConfigureAwait(false);

        LogApplicationConfigured(
            _config.OpcUa.ApplicationConfiguration.ServerConfiguration.MaxSessionCount,
            _config.OpcUa.ApplicationConfiguration.ServerConfiguration.MaxSubscriptionCount);

        return _config.OpcUa.ApplicationConfiguration;
    }

    private void ConfigureUserTokenPolicies(IApplicationConfigurationBuilderServerSelected serverBuilder)
    {
        if (!_config.DisableAnonymousAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.Anonymous));
        }

        if (!_config.DisableUsernamePasswordAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.UserName));
        }

        if (!_config.DisableCertAuth)
        {
            serverBuilder.AddUserTokenPolicy(new UserTokenPolicy(UserTokenType.Certificate));
        }
    }

    /// <summary>
    /// Configures OPC stack security.
    /// </summary>
    public async Task<ApplicationConfiguration> InitApplicationSecurityAsync(IApplicationConfigurationBuilderServerOptions securityBuilder)
    {
        RegisterCustomCertificateStoreType();

        // Update/install the custom application certificate first if provided, before setting up stores
        if (!string.IsNullOrEmpty(_config.OpcUa.NewCertificateBase64String) || !string.IsNullOrEmpty(_config.OpcUa.NewCertificateFileName))
        {
            LogCustomCertificateProvided();
        }

        var applicationCerts = new CertificateIdentifierCollection
        {
            new CertificateIdentifier
            {
                StoreType = _config.OpcUa.OpcOwnCertStoreType,
                SubjectName = _config.ProgramName,
                CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType,
            }
        };
        var options = securityBuilder.AddSecurityConfiguration(applicationCerts, _config.OpcUa.OpcOwnPKIRootDefault, rejectedRoot: null)
            .SetAutoAcceptUntrustedCertificates(_config.OpcUa.AutoAcceptCerts)
            .SetRejectUnknownRevocationStatus(!_config.OpcUa.DontRejectUnknownRevocationStatus)
            .SetRejectSHA1SignedCertificates(false)
            .SetMinimumCertificateKeySize(1024)
            .SetAddAppCertToTrustedStore(_config.OpcUa.TrustMyself);

        var securityConfiguration = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration;

        if (UsesCustomCertificateStoreType(_config.OpcUa.OpcOwnCertStoreType))
        {
            ConfigureCustomCertificateStores(securityConfiguration);
        }
        else
        {
            securityConfiguration.ApplicationCertificate.StoreType = _config.OpcUa.OpcOwnCertStoreType;
            securityConfiguration.ApplicationCertificate.StorePath = _config.OpcUa.OpcOwnCertStorePath;

            // configure trusted issuer certificates store
            securityConfiguration.TrustedIssuerCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.TrustedIssuerCertificates.StorePath = _config.OpcUa.OpcIssuerCertStorePath;

            // configure trusted peer certificates store
            securityConfiguration.TrustedPeerCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.TrustedPeerCertificates.StorePath = _config.OpcUa.OpcTrustedCertStorePath;

            // configure trusted user certificates store
            securityConfiguration.TrustedUserCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.TrustedUserCertificates.StorePath = _config.OpcUa.OpcTrustedUserCertStorePath;

            // configure user issuer certificates store
            securityConfiguration.UserIssuerCertificates.StoreType = CertificateStoreType.Directory;
            securityConfiguration.UserIssuerCertificates.StorePath = _config.OpcUa.OpcUserIssuerCertStorePath;

            // configure rejected certificates store
            securityConfiguration.RejectedCertificateStore.StoreType = CertificateStoreType.Directory;
            securityConfiguration.RejectedCertificateStore.StorePath = _config.OpcUa.OpcRejectedCertStorePath;

        }

        // update application certificate if requested BEFORE creating configuration
        // This ensures the custom certificate is available when the security configuration is created
        if ((!string.IsNullOrEmpty(_config.OpcUa.NewCertificateBase64String) || !string.IsNullOrEmpty(_config.OpcUa.NewCertificateFileName)))
        {
            // Temporarily create the configuration so we can install the custom certificate
            _config.OpcUa.ApplicationConfiguration = await options.CreateAsync().ConfigureAwait(false);

            if (!await _certificateManagementService.UpdateApplicationCertificateAsync(_config.OpcUa.NewCertificateBase64String, _config.OpcUa.NewCertificateFileName, _config.OpcUa.CertificatePassword, _config.OpcUa.PrivateKeyBase64String, _config.OpcUa.PrivateKeyFileName).ConfigureAwait(false))
            {
                throw new Exception("Update/Setting of the application certificate failed.");
            }

            LogCustomCertificateInstalled();
        }
        else
        {
            _config.OpcUa.ApplicationConfiguration = await options.CreateAsync().ConfigureAwait(false);
        }

        LogStoreTypeInfo("Application Certificate", securityConfiguration.ApplicationCertificate.StoreType);
        LogStorePathInfo("Application Certificate", securityConfiguration.ApplicationCertificate.StorePath);

        LogSha1RejectionStatus(securityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled");

        LogMinCertKeySize(securityConfiguration.MinimumCertificateKeySize);

        LogStoreTypeInfo("Trusted Issuer", securityConfiguration.TrustedIssuerCertificates.StoreType);
        LogStorePathInfo("Trusted Issuer Certificate", securityConfiguration.TrustedIssuerCertificates.StorePath);

        LogStoreTypeInfo("Trusted Peer Certificate", securityConfiguration.TrustedPeerCertificates.StoreType);
        LogStorePathInfo("Trusted Peer Certificate", securityConfiguration.TrustedPeerCertificates.StorePath);

        LogStoreTypeInfo("Trusted User Certificate", securityConfiguration.TrustedUserCertificates.StoreType);
        LogStorePathInfo("Trusted User Certificate", securityConfiguration.TrustedUserCertificates.StorePath);

        LogStoreTypeInfo("User Issuer Certificate", securityConfiguration.UserIssuerCertificates.StoreType);
        LogStorePathInfo("User Issuer Certificate", securityConfiguration.UserIssuerCertificates.StorePath);

        LogStoreTypeInfo("Rejected certificate", securityConfiguration.RejectedCertificateStore.StoreType);
        LogStorePathInfo("Rejected Certificate", securityConfiguration.RejectedCertificateStore.StorePath);

        // handle cert validation
        if (_config.OpcUa.AutoAcceptCerts)
        {
            LogAutoAcceptWarning();
        }

        _config.OpcUa.ApplicationConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;

        // remove issuer and trusted certificates with the given thumbprints
        if (_config.OpcUa.ThumbprintsToRemove?.Count > 0 &&
            !await _certificateManagementService.RemoveCertificatesAsync(_config.OpcUa.ThumbprintsToRemove).ConfigureAwait(false))
        {
            throw new Exception("Removing certificates failed.");
        }

        // add trusted issuer certificates
        if ((_config.OpcUa.IssuerCertificateBase64Strings?.Count > 0 || _config.OpcUa.IssuerCertificateFileNames?.Count > 0) &&
            !await _certificateManagementService.AddCertificatesAsync(_config.OpcUa.IssuerCertificateBase64Strings, _config.OpcUa.IssuerCertificateFileNames, true).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted issuer certificate(s) failed.");
        }

        // add trusted peer certificates
        if ((_config.OpcUa.TrustedCertificateBase64Strings?.Count > 0 || _config.OpcUa.TrustedCertificateFileNames?.Count > 0) &&
            !await _certificateManagementService.AddCertificatesAsync(_config.OpcUa.TrustedCertificateBase64Strings, _config.OpcUa.TrustedCertificateFileNames, false).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted peer certificate(s) failed.");
        }

        // add user issuer certificates (for user certificate chain validation)
        if ((_config.OpcUa.UserIssuerCertificateBase64Strings?.Count > 0 || _config.OpcUa.UserIssuerCertificateFileNames?.Count > 0) &&
            !await _certificateManagementService.AddUserCertificatesAsync(_config.OpcUa.UserIssuerCertificateBase64Strings, _config.OpcUa.UserIssuerCertificateFileNames, issuerCertificate: true).ConfigureAwait(false))
        {
            throw new Exception("Adding user issuer certificate(s) failed.");
        }

        // add trusted user certificates (user identity certificates)
        if ((_config.OpcUa.TrustedUserCertificateBase64Strings?.Count > 0 || _config.OpcUa.TrustedUserCertificateFileNames?.Count > 0) &&
            !await _certificateManagementService.AddUserCertificatesAsync(_config.OpcUa.TrustedUserCertificateBase64Strings, _config.OpcUa.TrustedUserCertificateFileNames, issuerCertificate: false).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted user certificate(s) failed.");
        }

        // update CRL if requested
        if ((!string.IsNullOrEmpty(_config.OpcUa.CrlBase64String) || !string.IsNullOrEmpty(_config.OpcUa.CrlFileName)) &&
            !await _certificateManagementService.UpdateCrlAsync(_config.OpcUa.CrlBase64String, _config.OpcUa.CrlFileName).ConfigureAwait(false))
        {
            throw new Exception("CRL update failed.");
        }

        return _config.OpcUa.ApplicationConfiguration;
    }

    private void ConfigureCustomCertificateStores(SecurityConfiguration securityConfiguration)
    {
        var storeType = _config.OpcUa.OpcOwnCertStoreType;
        var storePathPrefix = GetCustomStorePathPrefix(storeType);

        securityConfiguration.ApplicationCertificate.StoreType = storeType;
        securityConfiguration.ApplicationCertificate.StorePath = storePathPrefix + _config.OpcUa.OpcOwnCertStorePath;

        securityConfiguration.TrustedIssuerCertificates.StoreType = storeType;
        securityConfiguration.TrustedIssuerCertificates.StorePath = storePathPrefix + _config.OpcUa.OpcIssuerCertStorePath;

        securityConfiguration.TrustedPeerCertificates.StoreType = storeType;
        securityConfiguration.TrustedPeerCertificates.StorePath = storePathPrefix + _config.OpcUa.OpcTrustedCertStorePath;

        securityConfiguration.TrustedUserCertificates.StoreType = storeType;
        securityConfiguration.TrustedUserCertificates.StorePath = storePathPrefix + _config.OpcUa.OpcTrustedUserCertStorePath;

        securityConfiguration.UserIssuerCertificates.StoreType = storeType;
        securityConfiguration.UserIssuerCertificates.StorePath = storePathPrefix + _config.OpcUa.OpcUserIssuerCertStorePath;

        securityConfiguration.RejectedCertificateStore.StoreType = storeType;
        securityConfiguration.RejectedCertificateStore.StorePath = storePathPrefix + _config.OpcUa.OpcRejectedCertStorePath;
    }

    private static string GetCustomStorePathPrefix(string storeType) => storeType switch
    {
        FlatDirectoryCertificateStore.StoreTypeName => FlatDirectoryCertificateStore.StoreTypePrefix,
        KubernetesSecretCertificateStore.StoreTypeName => KubernetesSecretCertificateStore.StoreTypePrefix,
        _ => throw new ArgumentOutOfRangeException(nameof(storeType), $"Unsupported custom certificate store type '{storeType}'."),
    };

    private void RegisterCustomCertificateStoreType()
    {
        if (!UsesCustomCertificateStoreType(_config.OpcUa.OpcOwnCertStoreType))
        {
            return;
        }

        var certStoreTypeName = CertificateStoreType.GetCertificateStoreTypeByName(_config.OpcUa.OpcOwnCertStoreType);
        if (certStoreTypeName is not null)
        {
            return;
        }

        switch (_config.OpcUa.OpcOwnCertStoreType)
        {
            case FlatDirectoryCertificateStore.StoreTypeName:
                CertificateStoreType.RegisterCertificateStoreType(
                    FlatDirectoryCertificateStore.StoreTypeName,
                    new FlatDirectoryCertificateStoreType(_loggerFactory));
                break;
            case KubernetesSecretCertificateStore.StoreTypeName:
                CertificateStoreType.RegisterCertificateStoreType(
                    KubernetesSecretCertificateStore.StoreTypeName,
                    new KubernetesSecretCertificateStoreType(_loggerFactory, _kubernetesSecretStoreClientFactory, _config.OpcUa.OpcKubernetesSecretNamespace));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_config.OpcUa.OpcOwnCertStoreType), $"Unsupported custom certificate store type '{_config.OpcUa.OpcOwnCertStoreType}'.");
        }
    }

    private static bool UsesCustomCertificateStoreType(string storeType)
    {
        return storeType == FlatDirectoryCertificateStore.StoreTypeName ||
            storeType == KubernetesSecretCertificateStore.StoreTypeName;
    }

    /// <summary>
    /// Show information needed for the Create Signing Request process.
    /// </summary>
    public async Task ShowCreateSigningRequestInformationAsync(X509Certificate2 certificate)
    {
        try
        {
            // we need a certificate with a private key
            if (!certificate.HasPrivateKey)
            {
                // fetch the certificate with the private key
                try
                {
                    certificate = await LoadCertificatePrivateKeyAsync(
                        _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate,
                        null,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    LogErrorLoadingPrivateKey(e);
                    return;
                }
            }

            byte[] certificateSigningRequest = null;
            try
            {
                certificateSigningRequest = CertificateFactory.CreateSigningRequest(certificate);
            }
            catch (Exception e)
            {
                LogErrorCreatingSigningRequest(e);
                return;
            }

            LogCsrHeader();
            LogCsrApplicationUri(_config.OpcUa.ApplicationConfiguration.ApplicationUri);
            LogCsrApplicationName(_config.OpcUa.ApplicationConfiguration.ApplicationName);
            LogCsrApplicationType(_config.OpcUa.ApplicationConfiguration.ApplicationType);
            LogCsrProductUri(_config.OpcUa.ApplicationConfiguration.ProductUri);

            if (_config.OpcUa.ApplicationConfiguration.ApplicationType != ApplicationType.Client)
            {
                int serverNum = 0;
                foreach (var endpoint in _config.OpcUa.ApplicationConfiguration.ServerConfiguration.BaseAddresses)
                {
                    LogDiscoveryUrl(serverNum++, endpoint);
                }

                foreach (var endpoint in _config.OpcUa.ApplicationConfiguration.ServerConfiguration.AlternateBaseAddresses)
                {
                    LogDiscoveryUrl(serverNum++, endpoint);
                }

                string[] serverCapabilities = _config.OpcUa.ApplicationConfiguration.ServerConfiguration.ServerCapabilities.ToArray();
                LogServerCapabilities(string.Join(", ", serverCapabilities));
            }

            LogCsrBase64Header();
            LogCsrBase64Content(Convert.ToBase64String(certificateSigningRequest));
            LogCsrFooter();

            try
            {
                await File.WriteAllBytesAsync($"{_config.OpcUa.ApplicationConfiguration.ApplicationName}.csr", certificateSigningRequest).ConfigureAwait(false);
                LogBinaryCsrWritten($"{_config.OpcUa.ApplicationConfiguration.ApplicationName}.csr");
            }
            catch (Exception e)
            {
                LogErrorWritingCsrFile(e);
            }
        }
        catch (Exception e)
        {
            LogErrorInCsrCreation(e);
        }
    }

    /// <summary>
    /// Show all certificates in the certificate stores.
    /// </summary>
    public async Task ShowCertificateStoreInformationAsync()
    {
        // show application certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("Application", certs.Count);

            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "application");
        }

        // show trusted issuer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("Trusted issuer", certs.Count);
            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLsAsync(CancellationToken.None).ConfigureAwait(false);
                int crlNum = 1;
                LogStoreHasCrls("Trusted issuer", crls.Count);

                foreach (var crl in crls)
                {
                    LogCrlDetails($"{crlNum++:D2}", crl.Issuer, crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "trusted issuer");
        }

        // show trusted peer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("Trusted peer", certs.Count);

            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLsAsync(CancellationToken.None).ConfigureAwait(false);
                int crlNum = 1;
                LogStoreHasCrls("Trusted peer", crls.Count);

                foreach (var crl in crls)
                {
                    LogCrlDetails($"{crlNum++:D2}", crl.Issuer, crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "trusted peer");
        }

        // show trusted user certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedUserCertificates.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("Trusted user", certs.Count);

            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLsAsync(CancellationToken.None).ConfigureAwait(false);
                int crlNum = 1;
                LogStoreHasCrls("Trusted user", crls.Count);

                foreach (var crl in crls)
                {
                    LogCrlDetails($"{crlNum++:D2}", crl.Issuer, crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "trusted user");
        }

        // show user issuer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.UserIssuerCertificates.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("User issuer", certs.Count);

            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLsAsync(CancellationToken.None).ConfigureAwait(false);
                int crlNum = 1;
                LogStoreHasCrls("User issuer", crls.Count);

                foreach (var crl in crls)
                {
                    LogCrlDetails($"{crlNum++:D2}", crl.Issuer, crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "user issuer");
        }

        // show rejected peer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.OpenStore(_telemetryContext);
            var certs = await certStore.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            int certNum = 1;
            LogStoreContainsCerts("Rejected certificate", certs.Count);

            foreach (var cert in certs)
            {
                LogCertificateDetails($"{certNum++:D2}", cert.Subject, cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            LogErrorReadingStore(e, "rejected certificate");
        }
    }

    /// <summary>
    /// Event handler to validate certificates.
    /// </summary>
    private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
    {
        if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
        {
            e.Accept = _config.OpcUa.AutoAcceptCerts;
            if (_config.OpcUa.AutoAcceptCerts)
            {
                LogTrustingCertificate(e.Certificate.Subject);
            }
            else
            {
                LogRejectingCertificate(
                    e.Certificate.Subject,
                    $"{_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}{Path.DirectorySeparatorChar}certs",
                    $"{_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}{Path.DirectorySeparatorChar}certs");
            }
        }
    }

    private async Task<X509Certificate2> LoadCertificatePrivateKeyAsync(CertificateIdentifier certificateIdentifier, string password, CancellationToken ct)
    {
        if (certificateIdentifier is null)
        {
            return null;
        }

        using ICertificateStore store = certificateIdentifier.OpenStore(_telemetryContext);
        return await store.LoadPrivateKeyAsync(
            certificateIdentifier.Thumbprint,
            certificateIdentifier.SubjectName,
            null,
            certificateIdentifier.CertificateType,
            password?.ToCharArray(),
            ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not get hostname.")]
    partial void LogCouldNotGetHostname(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Alternate base addresses (for server binding and certificate DNSNames and IPAddresses extensions): {AlternateBaseAddresses}")]
    partial void LogAlternateBaseAddresses(IEnumerable<string> alternateBaseAddresses);

    [LoggerMessage(Level = LogLevel.Information, Message = "Added security policy {SecurityPolicyUri} with mode {SecurityMode}")]
    partial void LogAddedSecurityPolicy(string securityPolicyUri, MessageSecurityMode securityMode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Security policy None is a security risk and needs to be disabled for production use")]
    partial void LogSecurityPolicyNoneRisk();

    [LoggerMessage(Level = LogLevel.Information, Message = "LDS(-ME) registration interval set to {LdsRegistrationInterval} ms (0 means no registration)")]
    partial void LogLdsRegistrationInterval(int ldsRegistrationInterval);

    [LoggerMessage(Level = LogLevel.Information, Message = "No existing application certificate found. Creating a self-signed application certificate valid since yesterday for {DefaultLifeTime} months, with a {DefaultKeySize} bit key and {DefaultHashSize} bit hash")]
    partial void LogNoExistingCertificateFound(ushort defaultLifeTime, ushort defaultKeySize, ushort defaultHashSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "Application certificate with thumbprint {Thumbprint} found in the application certificate store")]
    partial void LogCertificateFound(string thumbprint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Using custom application certificate, skipping automatic certificate creation")]
    partial void LogUsingCustomCertificate();

    [LoggerMessage(Level = LogLevel.Information, Message = "Application certificate with thumbprint {Thumbprint} created")]
    partial void LogCertificateCreated(string thumbprint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Application certificate is for ApplicationUri {ApplicationUri}, ApplicationName {ApplicationName} and Subject is {Subject}")]
    partial void LogApplicationCertificateInfo(string applicationUri, string applicationName, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Application configured with MaxSessionCount {MaxSessionCount} and MaxSubscriptionCount {MaxSubscriptionCount}")]
    partial void LogApplicationConfigured(int maxSessionCount, int maxSubscriptionCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Custom application certificate provided via command line, installing before security configuration")]
    partial void LogCustomCertificateProvided();

    [LoggerMessage(Level = LogLevel.Information, Message = "Custom application certificate installed successfully")]
    partial void LogCustomCertificateInstalled();

    [LoggerMessage(Level = LogLevel.Information, Message = "{StoreName} store type is: {StoreType}")]
    partial void LogStoreTypeInfo(string storeName, string storeType);

    [LoggerMessage(Level = LogLevel.Information, Message = "{StoreName} store path is: {StorePath}")]
    partial void LogStorePathInfo(string storeName, string storePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejection of SHA1 signed certificates is {Status}")]
    partial void LogSha1RejectionStatus(string status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Minimum certificate key size set to {MinimumCertificateKeySize}")]
    partial void LogMinCertKeySize(ushort minimumCertificateKeySize);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Automatically accepting all client certificates, this is a security risk!")]
    partial void LogAutoAcceptWarning();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Trusting certificate {CertificateSubject} because of corresponding command line option")]
    partial void LogTrustingCertificate(string certificateSubject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Rejecting OPC application with certificate {CertificateSubject}. If you want to trust this certificate, please copy it from the directory {RejectedCertificateStore} to {TrustedPeerCertificates}")]
    partial void LogRejectingCertificate(string certificateSubject, string rejectedCertificateStore, string trustedPeerCertificates);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while loading private key")]
    partial void LogErrorLoadingPrivateKey(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while creating signing request")]
    partial void LogErrorCreatingSigningRequest(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "----------------------- CreateSigningRequest information ------------------")]
    partial void LogCsrHeader();

    [LoggerMessage(Level = LogLevel.Information, Message = "ApplicationUri: {ApplicationUri}")]
    partial void LogCsrApplicationUri(string applicationUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "ApplicationName: {ApplicationName}")]
    partial void LogCsrApplicationName(string applicationName);

    [LoggerMessage(Level = LogLevel.Information, Message = "ApplicationType: {ApplicationType}")]
    partial void LogCsrApplicationType(ApplicationType applicationType);

    [LoggerMessage(Level = LogLevel.Information, Message = "ProductUri: {ProductUri}")]
    partial void LogCsrProductUri(string productUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "DiscoveryUrl[{ServerNumber}]: {Endpoint}")]
    partial void LogDiscoveryUrl(int serverNumber, string endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "ServerCapabilities: {ServerCapabilities}")]
    partial void LogServerCapabilities(string serverCapabilities);

    [LoggerMessage(Level = LogLevel.Information, Message = "CSR (base64 encoded):")]
    partial void LogCsrBase64Header();

    [LoggerMessage(Level = LogLevel.Information, Message = "{CertificateSigningRequestBase64}")]
    partial void LogCsrBase64Content(string certificateSigningRequestBase64);

    [LoggerMessage(Level = LogLevel.Information, Message = "---------------------------------------------------------------------------")]
    partial void LogCsrFooter();

    [LoggerMessage(Level = LogLevel.Information, Message = "Binary CSR written to '{CsrFileName}'")]
    partial void LogBinaryCsrWritten(string csrFileName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while writing .csr file")]
    partial void LogErrorWritingCsrFile(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in CSR creation")]
    partial void LogErrorInCsrCreation(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "{StoreName} store contains {Count} certs")]
    partial void LogStoreContainsCerts(string storeName, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Index}: Subject {Subject} (thumbprint: {Thumbprint})")]
    partial void LogCertificateDetails(string index, string subject, string thumbprint);

    [LoggerMessage(Level = LogLevel.Information, Message = "{StoreName} store has {Count} CRLs")]
    partial void LogStoreHasCrls(string storeName, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Index}: Issuer {Issuer}, Next update time {NextUpdate}")]
    partial void LogCrlDetails(string index, string issuer, DateTime nextUpdate);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while trying to read information from {StoreName} store")]
    partial void LogErrorReadingStore(Exception exception, string storeName);
}
