using DcaShop.Inventory.Api;
using DcaShop.Inventory.Application.ReserveStock;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The reservation of an order's stock through its input port, in the wired shop: every line or none. Each test starts
/// a shop of its own, so a reservation never reaches another test.
/// </summary>
public sealed class StockReservationTest
{
    [Fact]
    public async Task AnOrderTheUnreservedStockDoesNotCoverIsRefusedAndReservesNothing()
    {
        using var shop = new ShopFactory();
        var covered = ProductId.Of(await shop.ProductIdAsync("Domain-Driven Design"));
        var shortOf = ProductId.Of(await shop.ProductIdAsync("Implementing Domain-Driven Design"));
        using var scope = shop.Services.CreateScope();
        var reserveStock = scope.ServiceProvider.GetRequiredService<IReserveStockInputPort>();
        var inStock = (await scope.ServiceProvider.GetRequiredService<InventoryService>().GetStockAsync(covered))!.AvailableStock;

        var refused = await reserveStock.ExecuteAsync(new ReserveStockCommand(
            [new ReserveStockCommand.Line(covered.Value, 1), new ReserveStockCommand.Line(shortOf.Value, 1_000_000)]));

        var insufficient = Assert.IsType<ReserveStockResult.InsufficientUnreservedStock>(refused.Value);
        Assert.Equal(shortOf.Value, Assert.Single(insufficient.Shortages).ProductId);
        var all = await reserveStock.ExecuteAsync(new ReserveStockCommand([new ReserveStockCommand.Line(covered.Value, inStock)]));
        Assert.IsType<ReserveStockResult.Reserved>(all.Value);
    }
}