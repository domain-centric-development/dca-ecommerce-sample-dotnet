using System.Collections.Concurrent;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Persistence;

public sealed class InMemoryCheckoutSessionRepository : ICheckoutSessionRepository
{
    private readonly ConcurrentDictionary<CheckoutSessionId, CheckoutSession> _sessions = new();

    public Task<CheckoutSession?> FindByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.TryGetValue(id, out var session) ? session : null);

    public Task<CheckoutSession> SaveAsync(CheckoutSession aggregate, CancellationToken cancellationToken = default)
    {
        _sessions[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.Values.FirstOrDefault(s => s.CartId == cartId && s.IsActive));
}
