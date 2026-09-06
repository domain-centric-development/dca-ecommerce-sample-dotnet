using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartRecovery.MergeCarts;

public interface IMergeCartsInputPort : IUseCase<MergeCartsCommand, MergeCartsResult>
{
}
