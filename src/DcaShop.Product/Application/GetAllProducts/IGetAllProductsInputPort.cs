using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Product.Application.GetAllProducts;

public interface IGetAllProductsInputPort : IUseCase<GetAllProductsQuery, GetAllProductsResult>
{
}
