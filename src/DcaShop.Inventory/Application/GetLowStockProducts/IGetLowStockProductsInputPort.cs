using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.GetLowStockProducts;

/// <summary>Driving port: the products holding less stock than a given threshold.</summary>
public interface IGetLowStockProductsInputPort : IUseCase<GetLowStockProductsQuery, GetLowStockProductsResult>
{
}
