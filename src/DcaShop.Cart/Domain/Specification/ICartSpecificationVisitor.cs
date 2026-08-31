using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.Cart.Domain.Specification;

/// <summary>
/// Visitor that translates cart specifications into an adapter-specific form.
/// </summary>
/// <remarks>
/// A persistence adapter implements this to turn a domain-level cart specification into a query predicate.
/// </remarks>
/// <typeparam name="TResult">The representation the visitor produces.</typeparam>
public interface ICartSpecificationVisitor<out TResult> : ISpecificationVisitor<ShoppingCart, TResult>
{
    TResult Visit(ActiveCart specification);

    TResult Visit(LastUpdatedBefore specification);

    TResult Visit(HasMinTotal specification);

    TResult Visit(HasAnyAvailableItem specification);

    TResult Visit(CustomerAllowsMarketing specification);
}
