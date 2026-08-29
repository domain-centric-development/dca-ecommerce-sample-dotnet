using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Application.Shared;

/// <summary>Current prices from the Pricing context, translated into the catalog's own <see cref="PriceData"/>.</summary>
public interface IPricingDataPort : IOutputPort
{
    Task<IReadOnlyDictionary<ProductId, PriceData>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}

public sealed record PriceData(ProductId ProductId, Money CurrentPrice);
