using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

public interface ICheckoutSessionRepository : IRepository<CheckoutSession, CheckoutSessionId>
{
    Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default);
}
