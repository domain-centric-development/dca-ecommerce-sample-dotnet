namespace DcaShop.SharedKernel.Domain.Specification;

/// <summary>
/// Visitor for translating domain specifications into other representations.
/// </summary>
/// <remarks>
/// Adapters implement this to convert an <see cref="ICompositeSpecification{T}"/> into a persistence- or
/// transport-specific form (SQL predicate, LINQ expression, document filter, …).
/// </remarks>
/// <typeparam name="T">The candidate type the specification evaluates.</typeparam>
/// <typeparam name="TResult">The representation the visitor produces.</typeparam>
public interface ISpecificationVisitor<T, out TResult>
{
    TResult Visit(AndSpecification<T> specification);

    TResult Visit(OrSpecification<T> specification);

    TResult Visit(NotSpecification<T> specification);
}
