using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartRecovery.GetCartMergeOptions;

public interface IGetCartMergeOptionsInputPort : IUseCase<GetCartMergeOptionsQuery, GetCartMergeOptionsResult>
{
}
