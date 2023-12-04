namespace UnitTests;

using System.Threading.Tasks;

public class OpcUaUnitTests : OpcPlcBase
{
    public OpcUaUnitTests()
        : base(new[] { "--gn=2" }) // Additional arguments.
    {
    }

    [Test]
    public async Task TestConnectedClientSession()
    {
        var opcUaClient = await OpcUaClientFactory
            .GetConnectedClient(OpcPlcEndpointUrl)
            .ConfigureAwait(false);

        opcUaClient.Session.Connected.Should().BeTrue();
        opcUaClient.Session.Close();
    }
}
