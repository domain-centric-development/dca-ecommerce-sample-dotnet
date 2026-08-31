namespace DcaShop.SharedKernel.Domain.Specification;

/// <summary>Logical NOT of a domain specification.</summary>
public sealed class NotSpecification<T> : ICompositeSpecification<T>
{
    public NotSpecification(ICompositeSpecification<T> inner)
    {
        Inner = inner;
    }

    public ICompositeSpecification<T> Inner { get; }

    public bool IsSatisfiedBy(T candidate) => !Inner.IsSatisfiedBy(candidate);

    public TResult Accept<TResult>(ISpecificationVisitor<T, TResult> visitor) => visitor.Visit(this);
}
