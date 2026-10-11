namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>
/// Outcome of a reduction, line by line; a missing stock record or insufficient stock is reported, not thrown.
/// <paramref name="AlreadyReduced"/> when the event had reduced the stock before — then no line was touched.
/// </summary>
public sealed record ReduceStockResult(IReadOnlyList<ReduceStockResult.LineOutcome> Lines, bool AlreadyReduced = false)
{
    public static ReduceStockResult Duplicate() => new([], AlreadyReduced: true);

    /// <summary>One line's outcome.</summary>
    public sealed record LineOutcome(Guid ProductId, bool Success, int PreviousStock, int RemainingStock, string? ErrorMessage)
    {
        public static LineOutcome Reduced(Guid productId, int previousStock, int remainingStock) =>
            new(productId, true, previousStock, remainingStock, null);

        public static LineOutcome Failure(Guid productId, string errorMessage) =>
            new(productId, false, 0, 0, errorMessage);
    }
}