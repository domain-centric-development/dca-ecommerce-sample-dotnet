using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

public interface ICheckoutSessionRepository : IRepository<CheckoutSession, CheckoutSessionId>
{
    Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default);

    Task<CheckoutSession?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>The customer's most recently confirmed (or completed) session — what the confirmation page shows.</summary>
    Task<CheckoutSession?> FindConfirmedOrCompletedByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}
