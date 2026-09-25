using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The stub an integration test puts in place of an external system: a real HTTP server on a free
/// port that answers what the test arranges.
/// </summary>
public sealed class HttpStubSmokeTest : IDisposable
{
    private readonly WireMockServer _externalSystem = WireMockServer.Start();

    [Fact]
    public async Task AnswersWhatTheTestArranged()
    {
        _externalSystem.Given(Request.Create().WithPath("/status").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("available"));

        using var client = new HttpClient();
        var body = await client.GetStringAsync($"{_externalSystem.Url}/status");

        Assert.Equal("available", body);
    }

    public void Dispose() => _externalSystem.Stop();
}