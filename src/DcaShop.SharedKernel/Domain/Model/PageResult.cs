using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Framework-independent pagination result: one page of content plus the total across all pages.</summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed record PageResult<T> : IValue
{
    public PageResult(IReadOnlyList<T> content, long totalElements, int pageNumber, int pageSize)
    {
        if (totalElements < 0)
        {
            throw new ArgumentException("Total elements cannot be negative", nameof(totalElements));
        }

        if (pageNumber < 0)
        {
            throw new ArgumentException("Page number cannot be negative", nameof(pageNumber));
        }

        if (pageSize < 1)
        {
            throw new ArgumentException("Page size must be positive", nameof(pageSize));
        }

        Content = content.ToList();
        TotalElements = totalElements;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Content { get; }

    public long TotalElements { get; }

    public int PageNumber { get; }

    public int PageSize { get; }
}
