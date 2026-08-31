using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>
/// The customer owning the cart accepts marketing communication.
/// </summary>
/// <remarks>
/// The cart aggregate has no view of customer preferences, so an in-memory evaluation stays neutral
/// (<see langword="true"/>). A persistence adapter with a customer read model can push the predicate down.
/// </remarks>
public sealed record CustomerAllowsMarketing : ICartSpecification
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
