namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Gds.Client;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

[TestFixture]
public class GdsPushConfigurationFlatDirectoryTests
{
    private readonly string _storeRootPath;
    private readonly string _appStorePath;
    private readonly PlcSimulatorFixture _simulator;

    public GdsPushConfigurationFlatDirectoryTests()
    {
        _storeRootPath = Path.Combine(Path.GetTempPath(), $"opcplc-gds-flat-{Guid.NewGuid():N}");
        _appStorePath = Path.Combine(_storeRootPath, "own");

        _simulator = new PlcSimulatorFixture(
        [
            "--at=FlatDirectory",
            $"--ap={_appStorePath}",
            $"--tp={Path.Combine(_storeRootPath, "trusted")}",
            $"--ip={Path.Combine(_storeRootPath, "issuer")}",
            $"--rp={Path.Combine(_storeRootPath, "rejected")}",
            $"--tup={Path.Combine(_storeRootPath, "trusteduser")}",
            $"--uip={Path.Combine(_storeRootPath, "userissuer")}",
        ]);
    }

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        await _simulator.StartAsync().ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _simulator.StopAsync().ConfigureAwait(false);

        // The server may still hold open handles during shutdown races on Windows.
        // Best effort cleanup keeps temp folders from piling up across runs.
        try
        {
            if (Directory.Exists(_storeRootPath))
            {
                Directory.Delete(_storeRootPath, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup failures in tests
        }
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_SucceedsWithFlatDirectoryStore()
    {
        var client = new ServerPushConfigurationClient(_simulator.ClientConfiguration)
        {
            AdminCredentials = new UserIdentity(new UserNameIdentityToken
            {
                UserName = "sysadmin",
                DecryptedPassword = Encoding.UTF8.GetBytes("demo")
            })
        };

        await client.ConnectAsync(_simulator.EndpointUrl).ConfigureAwait(false);

        byte[] certificateRequest = await client.CreateSigningRequestAsync(
            client.DefaultApplicationGroup,
            client.ApplicationCertificateType,
            subjectName: null,
            regeneratePrivateKey: false,
            nonce: [21, 22, 23, 24]).ConfigureAwait(false);

        certificateRequest.Should().NotBeNullOrEmpty();
        certificateRequest[0].Should().Be(0x30, "DER encoded CSR starts with ASN.1 SEQUENCE");

        await client.ApplyChangesAsync().ConfigureAwait(false);
        await client.DisconnectAsync().ConfigureAwait(false);

        Directory.Exists(_appStorePath).Should().BeTrue();
        Directory.EnumerateFiles(_appStorePath).Should().NotBeEmpty("flat directory store should contain certificates directly in the root folder");
        Directory.EnumerateDirectories(_appStorePath).Should().BeEmpty("flat directory store should not create cert/private/crl subfolders");
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_FailsForNonAdminWithFlatDirectoryStore()
    {
        var client = new ServerPushConfigurationClient(_simulator.ClientConfiguration)
        {
            AdminCredentials = new UserIdentity(new UserNameIdentityToken
            {
                UserName = "user1",
                DecryptedPassword = Encoding.UTF8.GetBytes("password")
            })
        };

        await client.ConnectAsync(_simulator.EndpointUrl).ConfigureAwait(false);

        Func<Task> act = async () => await client.CreateSigningRequestAsync(
            client.DefaultApplicationGroup,
            client.ApplicationCertificateType,
            subjectName: null,
            regeneratePrivateKey: false,
            nonce: [25, 26, 27, 28]).ConfigureAwait(false);

        await act.Should()
            .ThrowAsync<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadUserAccessDenied)
            .ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_FailsForAnonymousWithFlatDirectoryStore()
    {
        var client = new ServerPushConfigurationClient(_simulator.ClientConfiguration);

        await client.ConnectAsync(_simulator.EndpointUrl).ConfigureAwait(false);

        Func<Task> act = async () => await client.CreateSigningRequestAsync(
            client.DefaultApplicationGroup,
            client.ApplicationCertificateType,
            subjectName: null,
            regeneratePrivateKey: false,
            nonce: [29, 30, 31, 32]).ConfigureAwait(false);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires administrator credentials*")
            .ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }
}
