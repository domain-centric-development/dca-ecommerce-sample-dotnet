using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>
/// The cart was last touched before the given moment (exclusive).
/// </summary>
/// <remarks>
/// The aggregate carries no timestamp, so an in-memory evaluation cannot decide this and stays neutral
/// (<see langword="true"/>). Persistence adapters push the predicate down to their own row timestamps.
/// </remarks>
public sealed record LastUpdatedBefore(DateTimeOffset Threshold) : ICartSpecification
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
