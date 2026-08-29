using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Product.Application.CreateProduct;

public interface ICreateProductInputPort : IUseCase<CreateProductCommand, CreateProductResult>
{
}
