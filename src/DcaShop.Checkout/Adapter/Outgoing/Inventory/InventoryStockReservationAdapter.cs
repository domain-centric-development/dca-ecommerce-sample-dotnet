using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Inventory.Api;

namespace DcaShop.Checkout.Adapter.Outgoing.Inventory;

/// <summary>
/// Anti-corruption layer to Inventory's Open Host Service for holding an order's stock: checkout's line items become
/// Inventory's stock lines, Inventory's shortages become checkout's.
/// </summary>
public sealed class InventoryStockReservationAdapter : IStockReservationPort
{
    private readonly InventoryService _inventory;

    public InventoryStockReservationAdapter(InventoryService inventory)
    {
        _inventory = inventory;
    }

    public async Task<IReadOnlyList<IStockReservationPort.Shortage>> ReserveAsync(
        IReadOnlyCollection<CheckoutLineItem> lines, CancellationToken cancellationToken = default)
    {
        var shortages = await _inventory.ReserveStockAsync(StockLines(lines), cancellationToken).ConfigureAwait(false);
        return shortages.Select(shortage => new IStockReservationPort.Shortage(shortage.ProductId, shortage.Unreserved)).ToList();
    }

    public Task ReleaseAsync(IReadOnlyCollection<CheckoutLineItem> lines, CancellationToken cancellationToken = default) =>
        _inventory.ReleaseStockAsync(StockLines(lines), cancellationToken);

    private static List<InventoryService.StockLine> StockLines(IReadOnlyCollection<CheckoutLineItem> lines) =>
        lines.Select(line => new InventoryService.StockLine(line.ProductId, line.Quantity)).ToList();
}