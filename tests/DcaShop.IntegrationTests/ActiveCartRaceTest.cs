using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Eight callers asking for the same customer's cart at once, against the store the application context
/// actually wires.
/// </summary>
/// <remarks>
/// The unit test holds the adapter to the contract; this one holds the composition to it. Both are needed: the
/// recovery from a refused claim happens outside the transaction the claim was attempted in, and a sequential
/// test never enters that path at all.
/// </remarks>
public sealed class ActiveCartRaceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const int ConcurrentCallers = 8;
    private const int Rounds = 10;

    private readonly WebApplicationFactory<Program> _factory;

    public ActiveCartRaceTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task EightSimultaneousCallersShareOneCart()
    {
        // The host is built once, before the callers start: WebApplicationFactory builds it on first access to
        // Services, and eight threads reaching that at the same moment would each get a shop of their own.
        var services = _factory.Services;

        for (var round = 0; round < Rounds; round++)
        {
            var customerId = $"race-{Guid.NewGuid()}";
            using var startLine = new Barrier(ConcurrentCallers);

            var answers = await Task.WhenAll(Enumerable.Range(0, ConcurrentCallers).Select(_ => Task.Run(async () =>
            {
                // A scope per caller: every request has its own transaction boundary, as it does in the shop.
                using var scope = services.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>();
                startLine.SignalAndWait();
                return (await useCase.ExecuteAsync(new GetOrCreateActiveCartCommand(customerId))).CartId;
            })));

            Assert.Single(answers.Distinct());
        }
    }
}
