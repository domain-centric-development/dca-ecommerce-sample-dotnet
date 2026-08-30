using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.GetStockForProducts;

/// <summary>Driving port: stock figures for a set of products.</summary>
public interface IGetStockForProductsInputPort : IUseCase<GetStockForProductsQuery, GetStockForProductsResult>
{
}
