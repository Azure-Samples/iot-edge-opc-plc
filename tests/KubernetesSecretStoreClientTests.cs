namespace OpcPlc.Tests.Configuration;

using FluentAssertions;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpcPlc.Certs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestFixture]
public class KubernetesSecretStoreClientTests
{
    [TestCase("kubernetes.io/tls", false, false)]
    [TestCase("kubernetes.io/tls", true, false)]
    [TestCase("kubernetes.io/tls", true, true)]
    [TestCase("Opaque", false, false)]
    public async Task Store_DeleteAsync_PreservesSecretTypeAndRequiredTlsKeys(
        string secretType, bool keepOtherCertificate, bool sharedKey)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=tls-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var otherKey = RSA.Create(2048);
        var otherRequest = new CertificateRequest("CN=tls-other", sharedKey ? key : otherKey, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var other = otherRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        var secret = new V1Secret
        {
            ApiVersion = "v1",
            Kind = "Secret",
            Metadata = new V1ObjectMeta
            {
                Name = "tls-store", NamespaceProperty = "opcplc-tests", ResourceVersion = "1",
                Labels = new Dictionary<string, string> { ["deployment"] = "preserved" }
            },
            Type = secretType,
            Data = new Dictionary<string, byte[]>
            {
                ["tls.crt"] = Encoding.UTF8.GetBytes(certificate.ExportCertificatePem() +
                    (keepOtherCertificate ? "\n" + other.ExportCertificatePem() : string.Empty)),
                ["tls.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())
            }
        };
        using var handler = new ValidatingSecretHandler(secret);
        using var kubernetes = new Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, handler);
        using var client = new KubernetesSecretStoreClient(NullLogger.Instance, kubernetes);
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open("KubernetesSecret:tls-store", noPrivateKeys: false);

        (await store.DeleteAsync(certificate.Thumbprint).ConfigureAwait(false)).Should().BeTrue();

        handler.Writes.Should().Be(1);
        handler.Secret.Type.Should().Be(secretType);
        handler.Secret.Metadata.Labels.Should().Equal(secret.Metadata.Labels);
        if (secretType == "kubernetes.io/tls")
        {
            handler.Secret.Data.Keys.Should().BeEquivalentTo(["tls.crt", "tls.key"]);
            if (!keepOtherCertificate)
            {
                handler.Secret.Data["tls.crt"].Should().BeEmpty();
                handler.Secret.Data["tls.key"].Should().BeEmpty();
            }
            else if (sharedKey)
            {
                handler.Secret.Data["tls.key"].Should().Equal(secret.Data["tls.key"]);
                using var loaded = await store.LoadPrivateKeyAsync(other.Thumbprint, other.Subject, string.Empty)
                    .ConfigureAwait(false);
                loaded.HasPrivateKey.Should().BeTrue();
            }
            else
            {
                handler.Secret.Data["tls.key"].Should().BeEmpty();
            }
        }
        else
        {
            handler.Secret.Data.Should().BeEmpty();
        }
        using var certificates = await store.EnumerateAsync().ConfigureAwait(false);
        certificates.Count.Should().Be(keepOtherCertificate ? 1 : 0);
    }

    [Test]
    public async Task Store_AddAsync_RestoresTlsSecretDeletedBeforeRollback()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=tls-rollback", key, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var original = new Dictionary<string, byte[]>
        {
            ["tls.crt"] = Encoding.UTF8.GetBytes(certificate.ExportCertificatePem()),
            ["tls.key"] = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem())
        };
        var secret = new V1Secret
        {
            ApiVersion = "v1",
            Kind = "Secret",
            Metadata = new V1ObjectMeta
            {
                Name = "tls-store", NamespaceProperty = "opcplc-tests", ResourceVersion = "1"
            },
            Type = "kubernetes.io/tls",
            Data = new Dictionary<string, byte[]>(original)
        };
        using var handler = new ValidatingSecretHandler(secret);
        using var kubernetes = new Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, handler);
        using var client = new KubernetesSecretStoreClient(NullLogger.Instance, kubernetes);
        using var store = new KubernetesSecretCertificateStore(NullLogger.Instance, client, "opcplc-tests");
        store.Open("KubernetesSecret:tls-store");
        (await store.DeleteAsync(certificate.Thumbprint).ConfigureAwait(false)).Should().BeTrue();
        handler.Secret.Data["tls.key"].Should().BeEmpty();

        // The SDK rolls a failed ApplyChanges back by re-adding the enumerated public certificate.
        using (var publicCertificate = Opc.Ua.Security.Certificates.Certificate.FromRawData(certificate.RawData))
        {
            await store.AddAsync(publicCertificate).ConfigureAwait(false);
        }

        handler.Secret.Type.Should().Be("kubernetes.io/tls");
        handler.Secret.Data.Should().BeEquivalentTo(original);
    }

    private sealed class ValidatingSecretHandler(V1Secret secret) : DelegatingHandler
    {
        public V1Secret Secret { get; private set; } = secret;
        public int Writes { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Get)
            {
                return Json(request, new V1SecretList { ApiVersion = "v1", Kind = "SecretList", Items = [Secret] });
            }
            request.Method.Should().Be(HttpMethod.Put);
            var updated = KubernetesJson.Deserialize<V1Secret>(
                await request.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            updated.Type.Should().Be(Secret.Type, "Kubernetes Secret type is immutable");
            updated.Metadata.ResourceVersion.Should().Be(Secret.Metadata.ResourceVersion);
            if (updated.Type == "kubernetes.io/tls")
            {
                updated.Data.Should().ContainKey("tls.crt").And.ContainKey("tls.key",
                    "the Kubernetes API requires both entries for TLS Secrets");
            }
            Secret = updated;
            Writes++;
            return Json(request, Secret);
        }

        private static HttpResponseMessage Json<T>(HttpRequestMessage request, T value) => new(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(KubernetesJson.Serialize(value), Encoding.UTF8, "application/json")
        };
    }
}
