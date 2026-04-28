namespace OpcPlc.Tests;

using FluentAssertions;
using NUnit.Framework;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Gds.Client;
using System;
using System.Text;
using System.Threading.Tasks;

[TestFixture]
public class GdsPushConfigurationTests
{
    private readonly PlcSimulatorFixture _simulator = new([]);

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        await _simulator.StartAsync().ConfigureAwait(false);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _simulator.StopAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_And_ApplyChanges_SucceedsForAdmin()
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
            nonce: [1, 2, 3, 4]).ConfigureAwait(false);

        certificateRequest.Should().NotBeNullOrEmpty();
        certificateRequest[0].Should().Be(0x30, "DER encoded CSR starts with ASN.1 SEQUENCE");

        var rejectedCertificates = await client.GetRejectedListAsync().ConfigureAwait(false);
        rejectedCertificates.Should().NotBeNull();

        await client.ApplyChangesAsync().ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_FailsForNonAdmin()
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
            nonce: [5, 6, 7, 8]).ConfigureAwait(false);

        await act.Should()
            .ThrowAsync<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadUserAccessDenied)
            .ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_UpdateTrustList_SucceedsForAdmin()
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

        TrustListDataType trustList = await client.ReadTrustListAsync().ConfigureAwait(false);
        trustList.Should().NotBeNull();

        bool reboot = await client.UpdateTrustListAsync(trustList).ConfigureAwait(false);
        reboot.Should().BeFalse("updating the trust list with the existing entries should not require a reboot");

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_UpdateTrustList_FailsForNonAdmin()
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

        Func<Task> act = async () => await client.ReadTrustListAsync().ConfigureAwait(false);

        await act.Should()
            .ThrowAsync<ServiceResultException>()
            .Where(e => e.StatusCode == StatusCodes.BadUserAccessDenied)
            .ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_CreateSigningRequest_FailsForAnonymous()
    {
        var client = new ServerPushConfigurationClient(_simulator.ClientConfiguration);

        await client.ConnectAsync(_simulator.EndpointUrl).ConfigureAwait(false);

        Func<Task> act = async () => await client.CreateSigningRequestAsync(
            client.DefaultApplicationGroup,
            client.ApplicationCertificateType,
            subjectName: null,
            regeneratePrivateKey: false,
            nonce: [9, 10, 11, 12]).ConfigureAwait(false);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires administrator credentials*")
            .ConfigureAwait(false);

        await client.DisconnectAsync().ConfigureAwait(false);
    }

    [Test]
    public async Task ServerPushClient_TrustList_AdvertisesWritable()
    {
        var client = new ServerPushConfigurationClient(_simulator.ClientConfiguration);

        await client.ConnectAsync(_simulator.EndpointUrl).ConfigureAwait(false);

        try
        {
            (NodeId Writable, NodeId UserWritable, string GroupName)[] groups =
            [
                (VariableIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_Writable,
                 VariableIds.ServerConfiguration_CertificateGroups_DefaultApplicationGroup_TrustList_UserWritable,
                 "DefaultApplicationGroup"),
                (VariableIds.ServerConfiguration_CertificateGroups_DefaultHttpsGroup_TrustList_Writable,
                 VariableIds.ServerConfiguration_CertificateGroups_DefaultHttpsGroup_TrustList_UserWritable,
                 "DefaultHttpsGroup"),
                (VariableIds.ServerConfiguration_CertificateGroups_DefaultUserTokenGroup_TrustList_Writable,
                 VariableIds.ServerConfiguration_CertificateGroups_DefaultUserTokenGroup_TrustList_UserWritable,
                 "DefaultUserTokenGroup"),
            ];

            foreach ((NodeId writableId, NodeId userWritableId, string groupName) in groups)
            {
                DataValue writable = await client.Session.ReadValueAsync(writableId).ConfigureAwait(false);
                DataValue userWritable = await client.Session.ReadValueAsync(userWritableId).ConfigureAwait(false);

                writable.Value.Should().Be(true,
                    $"GDS push orchestrators (e.g. Azure IoT Operations) refuse to attempt a TrustList update unless the {groupName} TrustList file node advertises Writable = true");
                userWritable.Value.Should().Be(true,
                    $"GDS push orchestrators may also probe UserWritable on the {groupName} TrustList before attempting a trust list update");
            }
        }
        finally
        {
            await client.DisconnectAsync().ConfigureAwait(false);
        }
    }
}
