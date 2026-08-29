using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Product.Application.GetProductById;

public interface IGetProductByIdInputPort : IUseCase<GetProductByIdQuery, GetProductByIdResult>
{
}
