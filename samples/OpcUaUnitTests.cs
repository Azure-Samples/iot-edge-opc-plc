namespace UnitTests;

using System.Threading.Tasks;

public class OpcUaUnitTests : OpcPlcBase
{
    public OpcUaUnitTests()
        : base(
            new[] // Additional arguments.
            {
                "--gn=2",
            })
    {
    }

    [Test]
    public async Task TestConnectedClientSession()
    {
        using var opcUaClient = await OpcUaClientFactory
            .GetConnectedClient(OpcPlcEndpointUrl)
            .ConfigureAwait(false);

        opcUaClient.Session.Connected.Should().BeTrue();
        opcUaClient.Session.Close();
    }
}
