using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>The cart total (price at addition × quantity) reaches the given minimum.</summary>
public sealed record HasMinTotal : ICartSpecification
{
    public HasMinTotal(Money minimum)
    {
        Minimum = minimum ?? throw new ArgumentNullException(nameof(minimum));
    }

    public Money Minimum { get; }

    public bool IsSatisfiedBy(ShoppingCart candidate)
    {
        var total = candidate.CalculateTotal();

        // Amounts in different currencies cannot be compared here.
        return total.Currency == Minimum.Currency && total.Amount >= Minimum.Amount;
    }

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
