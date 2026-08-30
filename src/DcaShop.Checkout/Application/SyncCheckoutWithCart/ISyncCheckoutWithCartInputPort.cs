using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.SyncCheckoutWithCart;

/// <summary>
/// Brings an active checkout session back in line with its cart. The cart stays modifiable during checkout,
/// so its contents may change while the customer walks through the steps. A cart without an active session
/// is a no-op.
/// </summary>
public interface ISyncCheckoutWithCartInputPort : IUseCase<SyncCheckoutWithCartCommand, SyncCheckoutWithCartResult>
{
}
