using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Inventory.Domain.Specification;

/// <summary>
/// A stock-related specification.
/// </summary>
/// <remarks>
/// Extends the generic <see cref="ICompositeSpecification{T}"/> for <see cref="StockLevel"/> so persistence
/// adapters can translate the individual leaf specifications without any query technology leaking into the
/// domain.
/// </remarks>
public interface IStockLevelSpecification : ICompositeSpecification<StockLevel>
{
}
