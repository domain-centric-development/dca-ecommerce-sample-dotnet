namespace DcaShop.SharedKernel.Domain.Specification;

/// <summary>Logical OR of two domain specifications.</summary>
public sealed class OrSpecification<T> : ICompositeSpecification<T>
{
    public OrSpecification(ICompositeSpecification<T> left, ICompositeSpecification<T> right)
    {
        Left = left;
        Right = right;
    }

    public ICompositeSpecification<T> Left { get; }

    public ICompositeSpecification<T> Right { get; }

    public bool IsSatisfiedBy(T candidate) => Left.IsSatisfiedBy(candidate) || Right.IsSatisfiedBy(candidate);

    public TResult Accept<TResult>(ISpecificationVisitor<T, TResult> visitor) => visitor.Visit(this);
}
