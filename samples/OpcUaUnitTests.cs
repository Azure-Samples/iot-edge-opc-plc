namespace UnitTests;

using System.Threading.Tasks;

public class OpcUaUnitTests : OpcPlcBase
{
    public OpcUaUnitTests()
        : base(
            new[] // Additional arguments.
            {
                "--gn=2",
            },
            port: 51234) // Port must be unique for each test class.
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
