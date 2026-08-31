using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>
/// A cart-related specification.
/// </summary>
/// <remarks>
/// Extends the generic <see cref="ICompositeSpecification{T}"/> for <see cref="ShoppingCart"/> so persistence
/// adapters can translate the individual leaf specifications without any query technology leaking into the
/// domain. The leaves are <see cref="ActiveCart"/>, <see cref="LastUpdatedBefore"/>, <see cref="HasMinTotal"/>,
/// <see cref="HasAnyAvailableItem"/> and <see cref="CustomerAllowsMarketing"/>.
/// </remarks>
public interface ICartSpecification : ICompositeSpecification<ShoppingCart>
{
}
