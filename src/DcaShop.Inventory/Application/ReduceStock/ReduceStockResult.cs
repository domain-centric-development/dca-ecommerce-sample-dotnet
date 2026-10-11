namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>
/// Outcome of a reduction, line by line; a missing stock record or insufficient stock is reported, not thrown.
/// <paramref name="AlreadyReduced"/> when the event had reduced the stock before — then no line was touched.
/// </summary>
public sealed record ReduceStockResult(IReadOnlyList<ReduceStockResult.LineOutcome> Lines, bool AlreadyReduced = false)
{
    public static ReduceStockResult Duplicate() => new([], AlreadyReduced: true);

    /// <summary>Why a line was not reduced.</summary>
    public enum LineFailure
    {
        /// <summary>The inventory keeps no stock level for the product.</summary>
        StockLevelNotFound,

        /// <summary>Less is in stock than the line asks for; the stock stays as it was.</summary>
        InsufficientStock,
    }

    /// <summary>
    /// One line's outcome. A failure is named by its type and values, never in words: whoever reports it words it.
    /// For a failure, <paramref name="PreviousStock"/> and <paramref name="RemainingStock"/> are the stock as it is.
    /// </summary>
    public sealed record LineOutcome(Guid ProductId, int PreviousStock, int RemainingStock, LineFailure? Failure)
    {
        /// <summary>Whether the line was reduced.</summary>
        public bool Success => Failure is null;

        public static LineOutcome Reduced(Guid productId, int previousStock, int remainingStock) =>
            new(productId, previousStock, remainingStock, null);

        public static LineOutcome StockLevelNotFound(Guid productId) =>
            new(productId, 0, 0, LineFailure.StockLevelNotFound);

        public static LineOutcome InsufficientStock(Guid productId, int available) =>
            new(productId, available, available, LineFailure.InsufficientStock);
    }
}