using System.Collections.Concurrent;

using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory record of produced effects, keyed by effect and event id — demo stand-in for a table with that
/// composite key.
/// </summary>
public sealed class InMemoryProcessedEvents : IProcessedEvents
{
    private readonly ConcurrentDictionary<(string Effect, Guid EventId), byte> _processed = new();

    public Task<bool> ContainsAsync(string effect, Guid eventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_processed.ContainsKey((effect, eventId)));

    public Task MarkProcessedAsync(string effect, Guid eventId, CancellationToken cancellationToken = default)
    {
        _processed.TryAdd((effect, eventId), 0);
        return Task.CompletedTask;
    }
}