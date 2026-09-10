using DcaShop.Inventory.Application.GetLowStockProducts;
using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The overview an operator asks for before a product runs out: which products hold less than the quantity
/// they name. Driven through the wired application, so the answer is the one a caller really gets.
/// </summary>
public sealed class LowStockOverviewTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LowStockOverviewTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NamesEveryProductHoldingLessThanTheGivenQuantity()
    {
        var nearlyGone = await StockedProductAsync(quantity: 2);
        var running = await StockedProductAsync(quantity: 3);

        var low = await LowStockAsync(threshold: 4);

        Assert.Contains(nearlyGone, low.Products.Select(product => product.ProductId));
        Assert.Contains(running, low.Products.Select(product => product.ProductId));
    }

    [Fact]
    public async Task LeavesOutProductsHoldingTheGivenQuantityOrMore()
    {
        var exactlyAtTheLimit = await StockedProductAsync(quantity: 4);
        var wellStocked = await StockedProductAsync(quantity: 9);

        var low = await LowStockAsync(threshold: 4);

        Assert.DoesNotContain(exactlyAtTheLimit, low.Products.Select(product => product.ProductId));
        Assert.DoesNotContain(wellStocked, low.Products.Select(product => product.ProductId));
    }

    [Fact]
    public async Task StatesHowMuchIsLeftOfEachProductItNames()
    {
        var nearlyGone = await StockedProductAsync(quantity: 3);

        var low = await LowStockAsync(threshold: 4);

        var listed = Assert.Single(low.Products, product => product.ProductId == nearlyGone);
        Assert.Equal(3, listed.AvailableQuantity);
    }

    [Fact]
    public async Task AnswersWithAnEmptyListWhenNoProductIsShort()
    {
        await StockedProductAsync(quantity: 6);

        var low = await LowStockAsync(threshold: 0);

        Assert.Empty(low.Products);
    }

    private async Task<ProductId> StockedProductAsync(int quantity)
    {
        var productId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISetStockLevelInputPort>()
            .ExecuteAsync(new SetStockLevelCommand(productId, quantity));
        return new ProductId(productId);
    }

    private async Task<GetLowStockProductsResult> LowStockAsync(int threshold)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IGetLowStockProductsInputPort>()
            .ExecuteAsync(new GetLowStockProductsQuery(threshold));
    }
}
