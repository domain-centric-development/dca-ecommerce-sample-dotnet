using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Pricing.Application.GetPricesForProducts;

/// <summary>Driving port: current prices for a set of products.</summary>
public interface IGetPricesForProductsInputPort : IUseCase<GetPricesForProductsQuery, GetPricesForProductsResult>
{
}
