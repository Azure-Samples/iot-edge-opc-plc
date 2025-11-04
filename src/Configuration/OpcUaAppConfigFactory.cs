namespace OpcPlc.Configuration;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

public class OpcUaAppConfigFactory(OpcPlcConfiguration config, ILogger logger, ILoggerFactory loggerFactory)
{
    private readonly OpcPlcConfiguration _config = config;
    private readonly ILogger _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    /// OpcApplicationConfiguration

    /// <summary>
    /// Configures all OPC stack settings.
    /// </summary>
    public async Task<ApplicationConfiguration> ConfigureAsync()
    {
        // instead of using a configuration XML file, configure everything programmatically
        var application = new ApplicationInstance {
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
                _logger.LogWarning(ex, "Could not get hostname.");
            }
        }

        _logger.LogInformation("Alternate base addresses (for server binding and certificate DNSNames and IPAddresses extensions): {AlternateBaseAddresses}", alternateBaseAddresses);

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
            _logger.LogInformation("Added security policy {SecurityPolicyUri} with mode {SecurityMode}",
                policy.SecurityPolicyUri,
                policy.SecurityMode);

            if (policy.SecurityMode == MessageSecurityMode.None)
            {
                _logger.LogWarning("Security policy {None} is a security risk and needs to be disabled for production use", "None");
            }
        }

        _logger.LogInformation("LDS(-ME) registration interval set to {LdsRegistrationInterval} ms (0 means no registration)",
            _config.OpcUa.LdsRegistrationInterval);

        var microsoftLogger = _loggerFactory.CreateLogger("OpcUa");

        // set logger interface, disables TraceEvent
        Utils.SetLogger(microsoftLogger);

        // log certificate status
        var certificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
        if (certificate == null)
        {
            _logger.LogInformation("No existing application certificate found. Creating a self-signed application certificate valid since yesterday for {DefaultLifeTime} months, " +
                "with a {DefaultKeySize} bit key and {DefaultHashSize} bit hash",
                CertificateFactory.DefaultLifeTime,
                CertificateFactory.DefaultKeySize,
                CertificateFactory.DefaultHashSize);
        }
        else
        {
            _logger.LogInformation("Application certificate with thumbprint {Thumbprint} found in the application certificate store",
                certificate.Thumbprint);
        }

        // Check the certificate, create new self-signed certificate if necessary.
        bool isCertValid = await application.CheckApplicationInstanceCertificates(silent: true, lifeTimeInMonths: CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
        if (!isCertValid)
        {
            throw new Exception("Application certificate invalid.");
        }

        if (certificate == null)
        {
            certificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            _logger.LogInformation("Application certificate with thumbprint {Thumbprint} created",
                certificate.Thumbprint);
        }

        _logger.LogInformation("Application certificate is for ApplicationUri {ApplicationUri}, ApplicationName {ApplicationName} and Subject is {Subject}",
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

        _logger.LogInformation("Application configured with MaxSessionCount {MaxSessionCount} and MaxSubscriptionCount {MaxSubscriptionCount}",
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

        if (_config.OpcUa.OpcOwnCertStoreType == FlatDirectoryCertificateStore.StoreTypeName)
        {
            // Register FlatDirectoryCertificateStoreType as known certificate store type.
            var certStoreTypeName = CertificateStoreType.GetCertificateStoreTypeByName(FlatDirectoryCertificateStore.StoreTypeName);

            if (certStoreTypeName is null)
            {
                CertificateStoreType.RegisterCertificateStoreType(
                    FlatDirectoryCertificateStore.StoreTypeName,
                    new FlatDirectoryCertificateStoreType());
            }
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

        if (_config.OpcUa.OpcOwnCertStoreType == FlatDirectoryCertificateStore.StoreTypeName)
        {
            securityConfiguration.ApplicationCertificate.StoreType = _config.OpcUa.OpcOwnCertStoreType;
            securityConfiguration.ApplicationCertificate.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcOwnCertStorePath;

            // configure trusted issuer certificates store
            securityConfiguration.TrustedIssuerCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.TrustedIssuerCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcIssuerCertStorePath;

            // configure trusted peer certificates store
            securityConfiguration.TrustedPeerCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.TrustedPeerCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcTrustedCertStorePath;

            // configure trusted user certificates store
            securityConfiguration.TrustedUserCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.TrustedUserCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcTrustedUserCertStorePath;

            // configure user issuer certificates store
            securityConfiguration.UserIssuerCertificates.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.UserIssuerCertificates.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcUserIssuerCertStorePath;

            // configure rejected certificates store
            securityConfiguration.RejectedCertificateStore.StoreType = FlatDirectoryCertificateStore.StoreTypeName;
            securityConfiguration.RejectedCertificateStore.StorePath = FlatDirectoryCertificateStore.StoreTypePrefix + _config.OpcUa.OpcRejectedCertStorePath;
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

        _config.OpcUa.ApplicationConfiguration = await options.Create().ConfigureAwait(false);

        _logger.LogInformation("Application Certificate store type is: {StoreType}", securityConfiguration.ApplicationCertificate.StoreType);
        _logger.LogInformation("Application Certificate store path is: {StorePath}", securityConfiguration.ApplicationCertificate.StorePath);

        _logger.LogInformation("Rejection of SHA1 signed certificates is {Status}",
            securityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled");

        _logger.LogInformation("Minimum certificate key size set to {MinimumCertificateKeySize}",
            securityConfiguration.MinimumCertificateKeySize);

        _logger.LogInformation("Trusted Issuer store type is: {StoreType}", securityConfiguration.TrustedIssuerCertificates.StoreType);
        _logger.LogInformation("Trusted Issuer Certificate store path is: {StorePath}", securityConfiguration.TrustedIssuerCertificates.StorePath);

        _logger.LogInformation("Trusted Peer Certificate store type is: {StoreType}", securityConfiguration.TrustedPeerCertificates.StoreType);
        _logger.LogInformation("Trusted Peer Certificate store path is: {StorePath}", securityConfiguration.TrustedPeerCertificates.StorePath);

        _logger.LogInformation("Trusted User Certificate store type is: {StoreType}", securityConfiguration.TrustedUserCertificates.StoreType);
        _logger.LogInformation("Trusted User Certificate store path is: {StorePath}", securityConfiguration.TrustedUserCertificates.StorePath);

        _logger.LogInformation("User Issuer Certificate store type is: {StoreType}", securityConfiguration.UserIssuerCertificates.StoreType);
        _logger.LogInformation("User Issuer Certificate store path is: {StorePath}", securityConfiguration.UserIssuerCertificates.StorePath);

        _logger.LogInformation("Rejected certificate store type is: {StoreType}", securityConfiguration.RejectedCertificateStore.StoreType);
        _logger.LogInformation("Rejected Certificate store path is: {StorePath}", securityConfiguration.RejectedCertificateStore.StorePath);

        // handle cert validation
        if (_config.OpcUa.AutoAcceptCerts)
        {
            _logger.LogWarning("Automatically accepting all client certificates, this is a security risk!");
        }

        _config.OpcUa.ApplicationConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;

        // remove issuer and trusted certificates with the given thumbprints
        if (_config.OpcUa.ThumbprintsToRemove?.Count > 0 &&
            !await RemoveCertificatesAsync(_config.OpcUa.ThumbprintsToRemove).ConfigureAwait(false))
        {
            throw new Exception("Removing certificates failed.");
        }

        // add trusted issuer certificates
        if ((_config.OpcUa.IssuerCertificateBase64Strings?.Count > 0 || _config.OpcUa.IssuerCertificateFileNames?.Count > 0) &&
            !await AddCertificatesAsync(_config.OpcUa.IssuerCertificateBase64Strings, _config.OpcUa.IssuerCertificateFileNames, true).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted issuer certificate(s) failed.");
        }

        // add trusted peer certificates
        if ((_config.OpcUa.TrustedCertificateBase64Strings?.Count > 0 || _config.OpcUa.TrustedCertificateFileNames?.Count > 0) &&
            !await AddCertificatesAsync(_config.OpcUa.TrustedCertificateBase64Strings, _config.OpcUa.TrustedCertificateFileNames, false).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted peer certificate(s) failed.");
        }

        // add user issuer certificates (for user certificate chain validation)
        if ((_config.OpcUa.UserIssuerCertificateBase64Strings?.Count > 0 || _config.OpcUa.UserIssuerCertificateFileNames?.Count > 0) &&
            !await AddUserCertificatesAsync(_config.OpcUa.UserIssuerCertificateBase64Strings, _config.OpcUa.UserIssuerCertificateFileNames, issuerCertificate: true).ConfigureAwait(false))
        {
            throw new Exception("Adding user issuer certificate(s) failed.");
        }

        // add trusted user certificates (user identity certificates)
        if ((_config.OpcUa.TrustedUserCertificateBase64Strings?.Count > 0 || _config.OpcUa.TrustedUserCertificateFileNames?.Count > 0) &&
            !await AddUserCertificatesAsync(_config.OpcUa.TrustedUserCertificateBase64Strings, _config.OpcUa.TrustedUserCertificateFileNames, issuerCertificate: false).ConfigureAwait(false))
        {
            throw new Exception("Adding trusted user certificate(s) failed.");
        }

        // update CRL if requested
        if ((!string.IsNullOrEmpty(_config.OpcUa.CrlBase64String) || !string.IsNullOrEmpty(_config.OpcUa.CrlFileName)) &&
            !await UpdateCrlAsync(_config.OpcUa.CrlBase64String, _config.OpcUa.CrlFileName).ConfigureAwait(false))
        {
            throw new Exception("CRL update failed.");
        }

        // update application certificate if requested or use the existing certificate
        if ((!string.IsNullOrEmpty(_config.OpcUa.NewCertificateBase64String) || !string.IsNullOrEmpty(_config.OpcUa.NewCertificateFileName)) &&
            !await UpdateApplicationCertificateAsync(_config.OpcUa.NewCertificateBase64String, _config.OpcUa.NewCertificateFileName, _config.OpcUa.CertificatePassword, _config.OpcUa.PrivateKeyBase64String, _config.OpcUa.PrivateKeyFileName).ConfigureAwait(false))
        {
            throw new Exception("Update/Setting of the application certificate failed.");
        }

        return _config.OpcUa.ApplicationConfiguration;
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
                    certificate = await _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(null).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while loading private key");
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
                _logger.LogError(e, "Error while creating signing request");
                return;
            }

            _logger.LogInformation("----------------------- CreateSigningRequest information ------------------");
            _logger.LogInformation("ApplicationUri: {ApplicationUri}", _config.OpcUa.ApplicationConfiguration.ApplicationUri);
            _logger.LogInformation("ApplicationName: {ApplicationName}", _config.OpcUa.ApplicationConfiguration.ApplicationName);
            _logger.LogInformation("ApplicationType: {ApplicationType}", _config.OpcUa.ApplicationConfiguration.ApplicationType);
            _logger.LogInformation("ProductUri: {ProductUri}", _config.OpcUa.ApplicationConfiguration.ProductUri);

            if (_config.OpcUa.ApplicationConfiguration.ApplicationType != ApplicationType.Client)
            {
                int serverNum = 0;
                foreach (var endpoint in _config.OpcUa.ApplicationConfiguration.ServerConfiguration.BaseAddresses)
                {
                    _logger.LogInformation("DiscoveryUrl[{ServerNumber}]: {Endpoint}", serverNum++, endpoint);
                }

                foreach (var endpoint in _config.OpcUa.ApplicationConfiguration.ServerConfiguration.AlternateBaseAddresses)
                {
                    _logger.LogInformation("DiscoveryUrl[{ServerNumber}]: {Endpoint}", serverNum++, endpoint);
                }

                string[] serverCapabilities = _config.OpcUa.ApplicationConfiguration.ServerConfiguration.ServerCapabilities.ToArray();
                _logger.LogInformation("ServerCapabilities: {ServerCapabilities}", string.Join(", ", serverCapabilities));
            }

            _logger.LogInformation("CSR (base64 encoded):");
            Console.WriteLine(Convert.ToBase64String(certificateSigningRequest));
            _logger.LogInformation("---------------------------------------------------------------------------");

            try
            {
                await File.WriteAllBytesAsync($"{_config.OpcUa.ApplicationConfiguration.ApplicationName}.csr", certificateSigningRequest).ConfigureAwait(false);
                _logger.LogInformation("Binary CSR written to '{CsrFileName}'", $"{_config.OpcUa.ApplicationConfiguration.ApplicationName}.csr");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while writing .csr file");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in CSR creation");
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
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Application store contains {Count} certs", certs.Count);

            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from application store");
        }

        // show trusted issuer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Trusted issuer store contains {Count} certs", certs.Count);
            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("Trusted issuer store has {Count} CRLs", crls.Count);

                foreach (var crl in crls)
                {
                    _logger.LogInformation("{Index}: Issuer {Issuer}, Next update time {NextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from trusted issuer store");
        }

        // show trusted peer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Trusted peer store contains {Count} certs", certs.Count);

            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("Trusted peer store has {Count} CRLs", crls.Count);

                foreach (var crl in crls)
                {
                    _logger.LogInformation("{Index}: Issuer {Issuer}, Next update time {NextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from trusted peer store");
        }

        // show trusted user certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedUserCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Trusted user store contains {Count} certs", certs.Count);

            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("Trusted user store has {Count} CRLs", crls.Count);

                foreach (var crl in crls)
                {
                    _logger.LogInformation("{Index}: Issuer {Issuer}, Next update time {NextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from trusted user store");
        }

        // show user issuer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.UserIssuerCertificates.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("User issuer store contains {Count} certs", certs.Count);

            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }

            if (certStore.SupportsCRLs)
            {
                var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                int crlNum = 1;
                _logger.LogInformation("User issuer store has {Count} CRLs", crls.Count);

                foreach (var crl in crls)
                {
                    _logger.LogInformation("{Index}: Issuer {Issuer}, Next update time {NextUpdate}",
                        $"{crlNum++:D2}",
                        crl.Issuer,
                        crl.NextUpdate);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from user issuer store");
        }

        // show rejected peer certs
        try
        {
            using ICertificateStore certStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.OpenStore();
            var certs = await certStore.Enumerate().ConfigureAwait(false);
            int certNum = 1;
            _logger.LogInformation("Rejected certificate store contains {Count} certs", certs.Count);

            foreach (var cert in certs)
            {
                _logger.LogInformation("{Index}: Subject {Subject} (thumbprint: {Thumbprint})",
                    $"{certNum++:D2}",
                    cert.Subject,
                    cert.GetCertHashString());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to read information from rejected certificate store");
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
                _logger.LogWarning("Trusting certificate {CertificateSubject} because of corresponding command line option", e.Certificate.Subject);
            }
            else
            {
                _logger.LogError(
                    "Rejecting OPC application with certificate {CertificateSubject}. If you want to trust this certificate, please copy it from the directory {RejectedCertificateStore} to {TrustedPeerCertificates}",
                    e.Certificate.Subject,
                    $"{_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}{Path.DirectorySeparatorChar}certs",
                    $"{_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}{Path.DirectorySeparatorChar}certs");
            }
        }
    }

    /// <summary>
    /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store.
    /// </summary>
    private async Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
    {
        bool result = true;

        if (thumbprintsToRemove.Count == 0)
        {
            _logger.LogError("There is no thumbprint specified for certificates to remove. Please check your command line options.");
            return false;
        }

        // search the trusted peer store and remove certificates with a specified thumbprint
        try
        {
            _logger.LogInformation("Starting to remove certificate(s) from trusted peer and trusted issuer store");
            using ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await trustedStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await trustedStore.Delete(thumbprint).ConfigureAwait(false))
                    {
                        _logger.LogWarning("Failed to remove certificate with thumbprint '{Thumbprint}' from the trusted peer store", thumbprint);
                    }
                    else
                    {
                        _logger.LogInformation("Removed certificate with thumbprint '{Thumbprint}' from the trusted peer store", thumbprint);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to remove certificate(s) from the trusted peer store");
            result = false;
        }

        // search the trusted issuer store and remove certificates with a specified thumbprint
        try
        {
            using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore();
            foreach (var thumbprint in thumbprintsToRemove)
            {
                var certToRemove = await issuerStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certToRemove?.Count > 0)
                {
                    if (!await issuerStore.Delete(thumbprint).ConfigureAwait(false))
                    {
                        _logger.LogWarning("Failed to delete certificate with thumbprint '{Thumbprint}' from the trusted issuer store", thumbprint);
                    }
                    else
                    {
                        _logger.LogInformation("Removed certificate with thumbprint '{Thumbprint}' from the trusted issuer store", thumbprint);
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while trying to remove certificate(s) from the trusted issuer store");
            result = false;
        }
        return result;
    }

    /// <summary>
    /// Validate and add certificates to the trusted issuer or trusted peer store.
    /// </summary>
    private async Task<bool> AddCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        bool result = true;

        if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
        {
            _logger.LogError("There is no certificate provided. Please check your command line options.");
            return false;
        }

        _logger.LogInformation("Starting to add certificate(s) to the {StoreType} store", issuerCertificate ? "trusted issuer" : "trusted peer");
        var certificatesToAdd = new X509Certificate2Collection();
        try
        {
            // validate the input and build issuer cert collection
            if (certificateFileNames?.Count > 0)
            {
                foreach (var certificateFileName in certificateFileNames)
                {
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(certificateFileName);
                    certificatesToAdd.Add(certificate);
                }
            }
            if (certificateBase64Strings?.Count > 0)
            {
                foreach (var certificateBase64String in certificateBase64Strings)
                {
                    byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(certificateBase64String, buffer, out _))
                    {
                        var certificate = X509CertificateLoader.LoadCertificate(buffer);
                        certificatesToAdd.Add(certificate);
                    }
                    else
                    {
                        _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The issuer certificate data is invalid. Please check your command line options");
            return false;
        }

        // add the certificate to the right store
        if (issuerCertificate)
        {
            try
            {
                using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await issuerStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted issuer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted issuer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted issuer store");
                result = false;
            }
        }
        else
        {
            try
            {
                using ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await trustedStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted peer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        // ignore error if cert already exists in store
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted peer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted peer store");
                result = false;
            }
        }
        return result;
    }

    /// <summary>
    /// Validate and add certificates to the trusted user or user issuer store.
    /// </summary>
    private async Task<bool> AddUserCertificatesAsync(
        List<string> certificateBase64Strings,
        List<string> certificateFileNames,
        bool issuerCertificate = true)
    {
        bool result = true;

        if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
        {
            _logger.LogError("There is no certificate provided. Please check your command line options.");
            return false;
        }

        _logger.LogInformation("Starting to add certificate(s) to the {StoreType} store", issuerCertificate ? "user issuer" : "trusted user");
        var certificatesToAdd = new X509Certificate2Collection();
        try
        {
            // validate the input and build cert collection
            if (certificateFileNames?.Count > 0)
            {
                foreach (var certificateFileName in certificateFileNames)
                {
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(certificateFileName);
                    certificatesToAdd.Add(certificate);
                }
            }
            if (certificateBase64Strings?.Count > 0)
            {
                foreach (var certificateBase64String in certificateBase64Strings)
                {
                    byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(certificateBase64String, buffer, out _))
                    {
                        var certificate = X509CertificateLoader.LoadCertificate(buffer);
                        certificatesToAdd.Add(certificate);
                    }
                    else
                    {
                        _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", certificateBase64String.Substring(0, 10));
                        return false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The user certificate data is invalid. Please check your command line options");
            return false;
        }

        // add to the configured user stores
        if (issuerCertificate)
        {
            try
            {
                using ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.UserIssuerCertificates.OpenStore();
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await issuerStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the user issuer store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in user issuer store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the user issuer store");
                result = false;
            }
        }
        else
        {
            try
            {
                using ICertificateStore trustedUserStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedUserCertificates.OpenStore();
                foreach (var certificateToAdd in certificatesToAdd)
                {
                    try
                    {
                        await trustedUserStore.Add(certificateToAdd).ConfigureAwait(false);
                        _logger.LogInformation("Certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the trusted user store", certificateToAdd.SubjectName.Name, certificateToAdd.Thumbprint);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogInformation(ex, "Certificate '{SubjectName}' already exists in trusted user store", certificateToAdd.SubjectName.Name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while adding a certificate to the trusted user store");
                result = false;
            }
        }

        return result;
    }

    /// <summary>
    /// Update the CRL in the corresponding store.
    /// </summary>
    private async Task<bool> UpdateCrlAsync(string newCrlBase64String, string newCrlFileName)
    {
        bool result = true;

        if (string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
        {
            _logger.LogError("There is no CRL specified. Please check your command line options");
            return false;
        }

        // validate input and create the new CRL
        _logger.LogInformation("Starting to update the current CRL");
        X509CRL newCrl;
        try
        {
            if (!string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
            {
                byte[] buffer = new byte[newCrlBase64String.Length * 3 / 4];
                if (Convert.TryFromBase64String(newCrlBase64String, buffer, out _))
                {
                    newCrl = new X509CRL(buffer);
                }
                else
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", newCrlBase64String.Substring(0, 10));
                    return false;
                }
            }
            else
            {
                newCrl = new X509CRL(newCrlFileName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The new CRL data is invalid");
            return false;
        }

        // check if CRL was signed by a trusted peer cert
        using (ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
        {
            bool trustedCrlIssuer = false;
            var trustedCertificates = await trustedStore.Enumerate().ConfigureAwait(false);

            foreach (var trustedCertificate in trustedCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, trustedCertificate.Subject) && newCrl.VerifySignature(trustedCertificate, throwOnError: false))
                    {
                        // The issuer of the new CRL is trusted. Delete the CRLs of the issuer in the trusted store.
                        _logger.LogInformation("Remove the current CRL from the trusted peer store");
                        trustedCrlIssuer = true;

                        var crlsToRemove = await trustedStore.EnumerateCRLs(trustedCertificate).ConfigureAwait(false);
                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await trustedStore.DeleteCRL(crlToRemove).ConfigureAwait(false))
                                {
                                    _logger.LogWarning("Failed to remove CRL issued by '{Issuer}' from the trusted peer store", crlToRemove.Issuer);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Error while removing the current CRL issued by '{Issuer}' from the trusted peer store", crlToRemove.Issuer);
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while removing the current CRL from the trusted peer store");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await trustedStore.AddCRL(newCrl).ConfigureAwait(false);
                    _logger.LogInformation("The new CRL issued by '{Issuer}' was added to the trusted peer store", newCrl.Issuer);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while adding the new CRL to the trusted peer store");
                    result = false;
                }
            }
        }

        // check if CRL was signed by a trusted issuer cert
        using (ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore())
        {
            bool trustedCrlIssuer = false;
            var issuerCertificates = await issuerStore.Enumerate().ConfigureAwait(false);

            foreach (var issuerCertificate in issuerCertificates)
            {
                try
                {
                    if (X509Utils.CompareDistinguishedName(newCrl.Issuer, issuerCertificate.Subject) && newCrl.VerifySignature(issuerCertificate, false))
                    {
                        // The issuer of the new CRL is trusted. Delete the CRLs of the issuer in the trusted store.
                        _logger.LogInformation("Remove the current CRL from the trusted issuer store");
                        trustedCrlIssuer = true;
                        var crlsToRemove = await issuerStore.EnumerateCRLs(issuerCertificate).ConfigureAwait(false);

                        foreach (var crlToRemove in crlsToRemove)
                        {
                            try
                            {
                                if (!await issuerStore.DeleteCRL(crlToRemove).ConfigureAwait(false))
                                {
                                    _logger.LogWarning("Failed to remove the current CRL issued by '{Issuer}' from the trusted issuer store", crlToRemove.Issuer);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Error while removing the current CRL issued by '{Issuer}' from the trusted issuer store", crlToRemove.Issuer);
                                result = false;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while removing the current CRL from the trusted issuer store");
                    result = false;
                }
            }

            // add the CRL if we trust the issuer
            if (trustedCrlIssuer)
            {
                try
                {
                    await issuerStore.AddCRL(newCrl).ConfigureAwait(false);
                    _logger.LogInformation("The new CRL issued by '{Issuer}' was added to the trusted issuer store", newCrl.Issuer);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while adding the new CRL issued by '{Issuer}' to the trusted issuer store", newCrl.Issuer);
                    result = false;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Validate and update the application.
    /// </summary>
    private async Task<bool> UpdateApplicationCertificateAsync(
        string newCertificateBase64String,
        string newCertificateFileName,
        string certificatePassword,
        string privateKeyBase64String,
        string privateKeyFileName)
    {
        if (string.IsNullOrEmpty(newCertificateFileName) && string.IsNullOrEmpty(newCertificateBase64String))
        {
            _logger.LogError("There is no new application certificate data provided. Please check your command line options.");
            return false;
        }

        // validate input and create the new application cert
        X509Certificate2 newCertificate;
        try
        {
            if (string.IsNullOrEmpty(newCertificateFileName))
            {
                byte[] buffer = new byte[newCertificateBase64String.Length * 3 / 4];
                if (Convert.TryFromBase64String(newCertificateBase64String, buffer, out _))
                {
                    newCertificate = X509CertificateLoader.LoadCertificate(buffer);
                }
                else
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", newCertificateBase64String.Substring(0, 10));
                    return false;
                }
            }
            else
            {
                newCertificate = X509CertificateLoader.LoadCertificateFromFile(newCertificateFileName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The new application certificate data is invalid");
            return false;
        }

        // validate input and create the private key
        _logger.LogInformation("Start updating the current application certificate");
        byte[] privateKey = null;
        try
        {
            if (!string.IsNullOrEmpty(privateKeyBase64String))
            {
                privateKey = new byte[privateKeyBase64String.Length * 3 / 4];
                if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out _))
                {
                    _logger.LogError("The provided string '{PartialString}...' is not a valid base64 string", privateKeyBase64String.Substring(0, 10));
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(privateKeyFileName))
            {
                privateKey = await File.ReadAllBytesAsync(privateKeyFileName).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "The private key data is invalid");
            return false;
        }

        // check if there is an application certificate and fetch its data
        bool hasApplicationCertificate = false;
        X509Certificate2 currentApplicationCertificate = null;
        string currentSubjectName = null;
        if (_config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate?.Certificate != null)
        {
            hasApplicationCertificate = true;
            currentApplicationCertificate = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
            currentSubjectName = currentApplicationCertificate.SubjectName.Name;
            _logger.LogInformation("The current application certificate has SubjectName '{CurrentSubjectName}' and thumbprint '{CurrentThumbprint}'", currentSubjectName, currentApplicationCertificate.Thumbprint);
        }
        else
        {
            _logger.LogInformation("There is no existing application certificate");
        }

        // for a cert update subject names of current and new certificate must match
        if (hasApplicationCertificate && !X509Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
        {
            _logger.LogError("The SubjectName '{NewSubjectName}' of the new certificate doesn't match the current certificates SubjectName '{CurrentSubjectName}'", newCertificate.SubjectName.Name, currentSubjectName);
            return false;
        }

        // if the new cert is not self-signed verify with the trusted peer and trusted issuer certificates
        try
        {
            if (!X509Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
            {
                // verify the new certificate was signed by a trusted issuer or trusted peer
                var certValidator = new CertificateValidator();
                var verificationTrustList = new CertificateTrustList();
                var verificationCollection = new CertificateIdentifierCollection();
                using (ICertificateStore issuerStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
                {
                    var certs = await issuerStore.Enumerate().ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                using (ICertificateStore trustedStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
                {
                    var certs = await trustedStore.Enumerate().ConfigureAwait(false);
                    foreach (var cert in certs)
                    {
                        verificationCollection.Add(new CertificateIdentifier(cert));
                    }
                }
                verificationTrustList.TrustedCertificates = verificationCollection;
                certValidator.Update(verificationTrustList, verificationTrustList, null);
                certValidator.Validate(newCertificate);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to verify integrity of the new certificate and the trusted issuer list");
            return false;
        }

        // detect format of new cert and create/update the application certificate
        X509Certificate2 newCertificateWithPrivateKey = null;
        string newCertFormat = null;
        // check if new cert is PFX
        if (string.IsNullOrEmpty(newCertFormat))
        {
            try
            {
                X509Certificate2 certWithPrivateKey = X509Utils.CreateCertificateFromPKCS12(privateKey, certificatePassword);
                newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                newCertFormat = "PFX";
                _logger.LogInformation("The private key for the new certificate was passed in using PFX format");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Certificate file is not PFX");
            }
        }
        // check if new cert is PEM
        if (string.IsNullOrEmpty(newCertFormat))
        {
            try
            {
                newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCertificate, privateKey, certificatePassword);
                newCertFormat = "PEM";
                _logger.LogInformation("The private key for the new certificate was passed in using PEM format");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Certificate file is not PEM");
            }
        }

        if (string.IsNullOrEmpty(newCertFormat))
        {
            // check if new cert is DER and there is an existing application certificate
            try
            {
                if (hasApplicationCertificate)
                {
                    X509Certificate2 certWithPrivateKey = await _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(certificatePassword).ConfigureAwait(false);
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                    newCertFormat = "DER";
                }
                else
                {
                    _logger.LogError("There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Application certificate format is not DER");
            }
        }

        // if there is no current application cert, we need a new cert with a private key
        if (hasApplicationCertificate)
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                _logger.LogError("The provided format of the private key is not supported (must be PEM or PFX) or the provided cert password is wrong");
                return false;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(newCertFormat))
            {
                _logger.LogError("There is no application certificate we can update and for the new application certificate there was not usable private key (must be PEM or PFX format) provided or the provided cert password is wrong");
                return false;
            }
        }

        // remove the existing and add the new application cert
        using (ICertificateStore appStore = _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
        {
            _logger.LogInformation("Remove the existing application certificate");
            try
            {
                if (hasApplicationCertificate && !await appStore.Delete(currentApplicationCertificate.Thumbprint).ConfigureAwait(false))
                {
                    _logger.LogWarning("Removing the existing application certificate with thumbprint '{CurrentThumbprint}' failed", currentApplicationCertificate.Thumbprint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove the existing application certificate from the ApplicationCertificate store");
            }
            try
            {
                await appStore.Add(newCertificateWithPrivateKey).ConfigureAwait(false);
                _logger.LogInformation("The new application certificate '{SubjectName}' and thumbprint '{Thumbprint}' was added to the application certificate store", newCertificateWithPrivateKey.SubjectName.Name, newCertificateWithPrivateKey.Thumbprint);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add the new application certificate to the application certificate store");
                return false;
            }
        }

        // update the application certificate
        try
        {
            _logger.LogInformation("Activating the new application certificate with thumbprint '{NewThumbprint}'", newCertificateWithPrivateKey.Thumbprint);
            _config.OpcUa.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificate;
            await _config.OpcUa.ApplicationConfiguration.CertificateValidator.UpdateCertificateAsync(_config.OpcUa.ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to activate the new application certificate");
            return false;
        }

        return true;
    }
}
