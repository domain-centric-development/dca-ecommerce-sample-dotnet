using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Inventory.Domain.Specification;

/// <summary>The available quantity of a stock level is below the given threshold.</summary>
public sealed record AvailableQuantityBelow : IStockLevelSpecification
{
    public AvailableQuantityBelow(StockQuantity threshold)
    {
        Threshold = threshold;
    }

    public StockQuantity Threshold { get; }

    /// <summary>
    /// Compares the quantity the warehouse holds — reservations do not enter the comparison, and the threshold
    /// itself is not low.
    /// </summary>
    public bool IsSatisfiedBy(StockLevel candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return candidate.AvailableQuantity.Value < Threshold.Value;
    }

    /// <summary>
    /// Hands this leaf to a <see cref="IStockLevelSpecificationVisitor{TResult}"/> that knows it. A visitor that
    /// only knows the generic composition sees the leaf wrapped in an <see cref="AndSpecification{T}"/> with
    /// itself — the same truth value, expressed in the vocabulary such a visitor does understand.
    /// </summary>
    public TResult Accept<TResult>(ISpecificationVisitor<StockLevel, TResult> visitor) =>
        visitor is IStockLevelSpecificationVisitor<TResult> stockVisitor
            ? stockVisitor.Visit(this)
            : visitor.Visit(new AndSpecification<StockLevel>(this, this));
}
