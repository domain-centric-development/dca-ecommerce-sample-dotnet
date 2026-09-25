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

    /// <summary>
    /// The session under that id that belongs to this customer, or nothing.
    /// </summary>
    /// <remarks>
    /// Every use case acting on a session a caller named asks this rather than <see cref="FindByIdAsync"/>: a
    /// session that is not theirs and a session that does not exist are indistinguishable on purpose, and a
    /// persistence adapter expresses it as one predicate. <c>FindByIdAsync</c> stays for the system paths that
    /// act on nobody's behalf.
    /// </remarks>
    Task<CheckoutSession?> FindByIdForCustomerAsync(CheckoutSessionId id, CustomerId customerId, CancellationToken cancellationToken = default);

    Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default);

    Task<CheckoutSession?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>The customer's most recently confirmed (or completed) session — what the confirmation page shows.</summary>
    Task<CheckoutSession?> FindConfirmedOrCompletedByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}