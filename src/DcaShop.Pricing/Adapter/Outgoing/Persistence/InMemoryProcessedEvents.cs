using System.Collections.Concurrent;

using DcaShop.Pricing.Application.Shared;

namespace DcaShop.Pricing.Adapter.Outgoing.Persistence;

/// <summary>In-memory record of handled integration events — demo stand-in for a table keyed by event id (ADR-001).</summary>
public sealed class InMemoryProcessedEvents : IProcessedEvents
{
    private readonly ConcurrentDictionary<Guid, byte> _processed = new();

    public Task<bool> ContainsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_processed.ContainsKey(eventId));

    public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        _processed.TryAdd(eventId, 0);
        return Task.CompletedTask;
    }
}