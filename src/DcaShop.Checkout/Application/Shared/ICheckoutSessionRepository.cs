using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

public interface ICheckoutSessionRepository : IRepository<CheckoutSession, CheckoutSessionId>
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<CartId, SemaphoreSlim> SessionLocks = new();
    async Task<T> InCartSessionAsync<T>(CartId cartId, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var gate = SessionLocks.GetOrAdd(cartId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await operation().ConfigureAwait(false); } finally { gate.Release(); }
    }

    Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default);

    Task<CheckoutSession?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>The customer's most recently confirmed (or completed) session — what the confirmation page shows.</summary>
    Task<CheckoutSession?> FindConfirmedOrCompletedByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}
