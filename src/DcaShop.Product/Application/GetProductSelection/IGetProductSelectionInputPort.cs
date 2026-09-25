using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Product.Application.GetProductSelection;

public interface IGetProductSelectionInputPort : IUseCase<GetProductSelectionQuery, GetProductSelectionResult>
{
}