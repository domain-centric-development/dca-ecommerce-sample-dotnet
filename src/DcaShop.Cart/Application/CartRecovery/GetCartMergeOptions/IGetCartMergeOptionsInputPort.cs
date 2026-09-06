using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.GetCartMergeOptions;

public interface IGetCartMergeOptionsInputPort : IUseCase<GetCartMergeOptionsQuery, GetCartMergeOptionsResult>
{
}
