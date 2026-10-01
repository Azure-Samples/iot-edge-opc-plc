namespace OpcPlc.Configuration;

using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class CertificateTrustListOperations(ICertificateTrustListManager manager, ILogger logger)
{
    public async Task<bool> ReplaceCrlAsync(
        ITrustListFileAccess fileAccess, string base64, string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(base64) && string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        try
        {
            byte[] data = string.IsNullOrEmpty(fileName)
                ? Convert.FromBase64String(base64)
                : await File.ReadAllBytesAsync(fileName, cancellationToken).ConfigureAwait(false);
            var crl = new X509CRL(data);
            using TrustListData current = await fileAccess.ReadTrustListAsync(
                TrustListIdentifier.Peers, TrustListMasks.All, cancellationToken).ConfigureAwait(false);
            TrustListMasks changed = 0;
            if (ReplaceSignedCrls(current.TrustedCertificates, current.TrustedCrls, crl))
            {
                changed |= TrustListMasks.TrustedCrls;
            }
            if (ReplaceSignedCrls(current.IssuerCertificates, current.IssuerCrls, crl))
            {
                changed |= TrustListMasks.IssuerCrls;
            }
            if (changed == 0)
            {
                logger.LogError("No trusted signer found for CRL issued by {Issuer}", crl.Issuer);
                return false;
            }

            await fileAccess.WriteTrustListAsync(TrustListIdentifier.Peers, current, changed, cancellationToken)
                .ConfigureAwait(false);
            logger.LogInformation("CRL replacement completed for {Issuer} in {Stores}", crl.Issuer, changed);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to replace CRL");
            return false;
        }
    }

    private static bool ReplaceSignedCrls(CertificateCollection certificates, X509CRLCollection existing, X509CRL crl)
    {
        Certificate[] signers = certificates.Where(certificate =>
            X509Utils.CompareDistinguishedName(crl.Issuer, certificate.Subject) &&
            crl.VerifySignature(certificate, throwOnError: false)).ToArray();
        if (signers.Length == 0)
        {
            return false;
        }

        for (int index = existing.Count - 1; index >= 0; index--)
        {
            X509CRL previous = existing[index];
            if (signers.Any(signer => X509Utils.CompareDistinguishedName(previous.Issuer, signer.Subject) &&
                previous.VerifySignature(signer, throwOnError: false)))
            {
                existing.RemoveAt(index);
            }
        }
        existing.Add(crl);
        return true;
    }

    public async Task<bool> RemovePeerCertificatesAsync(
        List<string> thumbprints, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if ((thumbprints?.Count ?? 0) == 0)
        {
            logger.LogError("There is no thumbprint specified for certificates to remove.");
            return false;
        }

        try
        {
            var selected = new HashSet<string>(thumbprints, StringComparer.OrdinalIgnoreCase);
            ITrustListTransaction transaction = await manager.BeginUpdateAsync(
                TrustListIdentifier.Peers, cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                foreach (string thumbprint in selected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await transaction.RemoveTrustedCertificateAsync(thumbprint, cancellationToken)
                        .ConfigureAwait(false);
                    await transaction.RemoveIssuerCertificateAsync(thumbprint, cancellationToken).ConfigureAwait(false);
                }
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (bool issuer in new[] { false, true })
            {
                using ICertificateStore store = issuer
                    ? manager.OpenIssuerStore(TrustListIdentifier.Peers)
                    : manager.OpenTrustedStore(TrustListIdentifier.Peers);
                foreach (string thumbprint in selected)
                {
                    using CertificateCollection remaining = await store.FindByThumbprintAsync(
                        thumbprint, cancellationToken).ConfigureAwait(false);
                    if (remaining.Count > 0)
                    {
                        logger.LogError("Certificate {Thumbprint} remains after removal from {StoreKind}",
                            thumbprint, issuer ? "issuer" : "trusted");
                        return false;
                    }
                }
            }
            logger.LogInformation("Certificate removal committed for peer trusted and issuer stores");
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to remove peer certificates");
            return false;
        }
    }

    public async Task<bool> AddAsync(
        List<string> certificateBase64Strings, List<string> certificateFileNames,
        TrustListIdentifier trustList, bool issuerCertificate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if ((certificateBase64Strings?.Count ?? 0) == 0 && (certificateFileNames?.Count ?? 0) == 0)
        {
            logger.LogError("There is no certificate provided. Please check your command line options.");
            return false;
        }

        using var certificates = new CertificateCollection();
        try
        {
            foreach (string fileName in certificateFileNames ?? [])
            {
                byte[] data = await File.ReadAllBytesAsync(fileName, cancellationToken).ConfigureAwait(false);
                using var certificate = new Certificate(data);
                certificates.Add(certificate);
            }
            foreach (string base64 in certificateBase64Strings ?? [])
            {
                using var certificate = new Certificate(Convert.FromBase64String(base64));
                certificates.Add(certificate);
            }

            ITrustListTransaction transaction = await manager.BeginUpdateAsync(trustList, cancellationToken)
                .ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                using ICertificateStore store = issuerCertificate
                    ? manager.OpenIssuerStore(trustList) : manager.OpenTrustedStore(trustList);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Certificate certificate in certificates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!seen.Add(certificate.Thumbprint))
                    {
                        continue;
                    }
                    using CertificateCollection existing = await store.FindByThumbprintAsync(
                        certificate.Thumbprint, cancellationToken).ConfigureAwait(false);
                    if (existing.Count > 0)
                    {
                        continue;
                    }
                    if (issuerCertificate)
                    {
                        await transaction.AddIssuerCertificateAsync(certificate, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await transaction.AddTrustedCertificateAsync(certificate, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            logger.LogInformation("Certificate update committed for {TrustList} ({StoreKind})",
                trustList, issuerCertificate ? "issuer" : "trusted");
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to add certificates to {TrustList} ({StoreKind})",
                trustList, issuerCertificate ? "issuer" : "trusted");
            return false;
        }
    }
}