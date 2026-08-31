using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Specification;

/// <summary>
/// Framework-agnostic specification for domain use: composable and translatable.
/// </summary>
/// <remarks>
/// Extends the marker <see cref="ISpecification{T}"/> with composition and visitor support. Prefer this
/// interface for specifications an adapter may have to translate into a query (SQL predicate, filter, …)
/// instead of evaluating in memory.
/// </remarks>
/// <typeparam name="T">The candidate type the specification evaluates.</typeparam>
public interface ICompositeSpecification<T> : ISpecification<T>
{
    /// <summary>Accepts a visitor that translates this specification into another representation.</summary>
    TResult Accept<TResult>(ISpecificationVisitor<T, TResult> visitor);

    /// <summary>Both specifications must hold.</summary>
    ICompositeSpecification<T> And(ICompositeSpecification<T> other) => new AndSpecification<T>(this, other);

    /// <summary>Either specification must hold.</summary>
    ICompositeSpecification<T> Or(ICompositeSpecification<T> other) => new OrSpecification<T>(this, other);

    /// <summary>The specification must not hold.</summary>
    ICompositeSpecification<T> Not() => new NotSpecification<T>(this);
}
