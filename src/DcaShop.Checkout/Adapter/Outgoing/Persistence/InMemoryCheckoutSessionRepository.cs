using System.Collections.Concurrent;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Persistence;

public sealed class InMemoryCheckoutSessionRepository : ICheckoutSessionRepository
{
    private readonly ConcurrentDictionary<CheckoutSessionId, CheckoutSession> _sessions = new();
    private readonly ConcurrentDictionary<CheckoutSessionId, long> _order = new();
    private long _sequence;

    public Task<CheckoutSession?> FindByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(id, out var session) ? session : null);

    public Task<CheckoutSession> SaveAsync(CheckoutSession aggregate, CancellationToken cancellationToken = default)
    {
        _sessions[aggregate.Id] = aggregate;
        _order[aggregate.Id] = Interlocked.Increment(ref _sequence);   // "latest" = last saved
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(id, out _);
        _order.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.Values.FirstOrDefault(s => s.CartId == cartId && s.IsActive));

    public Task<CheckoutSession?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Latest(s => s.CustomerId == customerId && s.IsActive));

    public Task<CheckoutSession?> FindConfirmedOrCompletedByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Latest(s => s.CustomerId == customerId && s.Status is CheckoutSessionStatus.Confirmed or CheckoutSessionStatus.Completed));

    private CheckoutSession? Latest(Func<CheckoutSession, bool> predicate) =>
        _sessions.Values.Where(predicate).OrderByDescending(s => _order.GetValueOrDefault(s.Id)).FirstOrDefault();
}
