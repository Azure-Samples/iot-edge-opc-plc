// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE.md in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPlc.Configuration;

using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class ApplicationCertificateLifecycle
{
    public static async Task PersistAndActivateAsync(
        ApplicationConfiguration configuration, Certificate candidate,
        IEnumerable<ICertificateStoreProvider> providers, ITelemetryContext telemetry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();
        if (!candidate.HasPrivateKey)
        {
            throw new InvalidOperationException("Application certificate has no private key.");
        }

        CertificateIdentifier identifier = configuration.SecurityConfiguration.ApplicationCertificates[0];
        ICertificateManager manager = configuration.CertificateManager;
        using CertificateEntry previous = manager.AcquireApplicationCertificateByType(identifier.CertificateType);
        if (previous is not null && !X509Utils.CompareDistinguishedName(
            previous.Certificate.Subject, candidate.Subject))
        {
            throw new InvalidOperationException("Application certificate subject does not match the current identity.");
        }

        using ICertificateStore store = CertificateStoreIdentifier.CreateStore(
            identifier.StoreType, telemetry, providers);
        store.Open(identifier.StorePath, noPrivateKeys: false);
        using (CertificateCollection existing = await store.FindByThumbprintAsync(
            candidate.Thumbprint, cancellationToken).ConfigureAwait(false))
        {
            if (existing.Count == 0)
            {
                await store.AddAsync(candidate, ct: cancellationToken).ConfigureAwait(false);
            }
        }

        using Certificate persisted = await store.LoadPrivateKeyAsync(candidate.Thumbprint, candidate.Subject,
            configuration.ApplicationUri, identifier.CertificateType, null, cancellationToken).ConfigureAwait(false);
        if (persisted is null || !persisted.HasPrivateKey || persisted.Thumbprint != candidate.Thumbprint)
        {
            throw new InvalidOperationException("Persisted application certificate private key could not be loaded.");
        }

        await manager.UpdateApplicationCertificateAsync(identifier.CertificateType, persisted, ct: cancellationToken)
            .ConfigureAwait(false);
        identifier.Thumbprint = persisted.Thumbprint;
        identifier.SubjectName = persisted.Subject;

        if (previous is not null && previous.Certificate.Thumbprint != persisted.Thumbprint &&
            !await store.DeleteAsync(previous.Certificate.Thumbprint, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "New application certificate is active but the old certificate remains.");
        }
    }

    public static async Task<CertificateEntry> EnsureAndAcquireAsync(
        IApplicationInstance application, bool customCertificateProvided, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        cancellationToken.ThrowIfCancellationRequested();
        if (!customCertificateProvided && !await application.CheckApplicationInstanceCertificatesAsync(
            silent: true, lifeTimeInMonths: CertificateFactory.DefaultLifeTime, cancellationToken)
            .ConfigureAwait(false))
        {
            throw new InvalidOperationException("Application certificate invalid.");
        }

        CertificateEntry entry = application.ApplicationConfiguration.CertificateManager
            .AcquireApplicationCertificateByType(ObjectTypeIds.RsaSha256ApplicationCertificateType);
        if (entry is null)
        {
            throw new InvalidOperationException(customCertificateProvided
                ? "Custom application certificate was provided but could not be loaded."
                : "Application certificate could not be loaded.");
        }

        if (!entry.Certificate.HasPrivateKey)
        {
            entry.Dispose();
            throw new InvalidOperationException("Application certificate has no private key.");
        }

        return entry;
    }

    public static byte[] CreateSigningRequest(Certificate certificate, ICertificateRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (certificate.HasPrivateKey)
        {
            return DefaultCertificateFactory.Instance.CreateSigningRequest(certificate);
        }

        ArgumentNullException.ThrowIfNull(registry);
        using CertificateEntry entry = registry.AcquireApplicationCertificateByType(
            ObjectTypeIds.RsaSha256ApplicationCertificateType);
        if (entry is null || !entry.Certificate.HasPrivateKey ||
            !string.Equals(entry.Certificate.Thumbprint, certificate.Thumbprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No matching application certificate private key is loaded.");
        }

        return DefaultCertificateFactory.Instance.CreateSigningRequest(entry.Certificate);
    }

    public static async Task<CertificateCollection> EnumerateApplicationCertificatesAsync(
        CertificateIdentifier identifier, IEnumerable<ICertificateStoreProvider> providers,
        ITelemetryContext telemetry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        cancellationToken.ThrowIfCancellationRequested();
        using ICertificateStore store = CertificateStoreIdentifier.CreateStore(
            identifier.StoreType, telemetry, providers);
        store.Open(identifier.StorePath, noPrivateKeys: true);
        return await store.EnumerateAsync(cancellationToken).ConfigureAwait(false);
    }
}