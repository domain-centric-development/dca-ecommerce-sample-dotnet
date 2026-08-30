namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>Outcome of a reduction; a missing stock record or insufficient stock is reported, not thrown.</summary>
public sealed record ReduceStockResult(Guid ProductId, bool Success, int PreviousStock, int RemainingStock, string? ErrorMessage)
{
    public static ReduceStockResult Reduced(Guid productId, int previousStock, int remainingStock) =>
        new(productId, true, previousStock, remainingStock, null);

    public static ReduceStockResult Failure(Guid productId, string errorMessage) =>
        new(productId, false, 0, 0, errorMessage);
}
