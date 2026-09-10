using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Inventory.Domain.Specification;

/// <summary>
/// Visitor that translates stock specifications into an adapter-specific form.
/// </summary>
/// <remarks>
/// A persistence adapter implements this to turn a domain-level stock specification into a query predicate.
/// </remarks>
/// <typeparam name="TResult">The representation the visitor produces.</typeparam>
public interface IStockLevelSpecificationVisitor<out TResult> : ISpecificationVisitor<StockLevel, TResult>
{
    TResult Visit(AvailableQuantityBelow specification);
}
