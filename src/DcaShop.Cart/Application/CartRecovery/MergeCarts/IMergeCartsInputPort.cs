using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.MergeCarts;

public interface IMergeCartsInputPort : IUseCase<MergeCartsCommand, MergeCartsResult>
{
}
