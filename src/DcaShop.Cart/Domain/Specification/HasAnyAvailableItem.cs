using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>
/// The cart holds at least one item that counts as available.
/// </summary>
/// <remarks>
/// Availability is the catalog's statement, not the aggregate's; an in-memory evaluation therefore stays
/// neutral (<see langword="true"/>). A persistence adapter pushes this down where it can see stock.
/// </remarks>
public sealed record HasAnyAvailableItem : ICartSpecification
{
    public bool IsSatisfiedBy(ShoppingCart candidate) => true;

    /// <summary>
    /// Hands this leaf to a <see cref="ICartSpecificationVisitor{TResult}"/> that knows it. A visitor that only
    /// knows the generic composition sees the leaf wrapped in an <see cref="AndSpecification{T}"/> with itself —
    /// the same truth value, expressed in the vocabulary such a visitor does understand.
    /// </summary>
    public TResult Accept<TResult>(ISpecificationVisitor<ShoppingCart, TResult> visitor) =>
        visitor is ICartSpecificationVisitor<TResult> cartVisitor
            ? cartVisitor.Visit(this)
            : visitor.Visit(new AndSpecification<ShoppingCart>(this, this));
}
