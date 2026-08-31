using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Framework-independent pagination request: a zero-based page number and a page size.</summary>
public sealed record PagingRequest : IValue
{
    public PagingRequest(int pageNumber, int pageSize)
    {
        if (pageNumber < 0)
        {
            throw new ArgumentException("Page number cannot be negative", nameof(pageNumber));
        }

        if (pageSize < 1)
        {
            throw new ArgumentException("Page size must be positive", nameof(pageSize));
        }

        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public static PagingRequest Of(int pageNumber, int pageSize) => new(pageNumber, pageSize);

    /// <summary>The number of elements to skip to reach this page.</summary>
    public long Offset => (long)PageNumber * PageSize;
}
