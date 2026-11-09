using System.Collections.Concurrent;
using System.Text.Json;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// The outbox of the unit tests: it records what is registered, in the form the PostgreSQL outbox stores it, and
/// delivers nothing — delivery is tested against the real outbox in the integration tests.
/// </summary>
public sealed class InMemoryEventOutbox(TimeProvider clock) : IEventOutbox
{
    private readonly ConcurrentDictionary<Guid, EventPublication> _publications = new();

    public Task<EventPublication> RegisterAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default) =>
        Task.FromResult(Register(@event, @event.EventId));

    public Task<EventPublication> RegisterAsync(IDomainEvent @event, CancellationToken cancellationToken = default) =>
        Task.FromResult(Register(@event, Guid.NewGuid()));

    private EventPublication Register(object @event, Guid id) =>
        _publications.GetOrAdd(id, _ => new EventPublication(id, JsonSerializer.Serialize(@event, @event.GetType(), EventJson.Options),
            @event.GetType(), clock.GetUtcNow(), PublicationStatus.Pending, 0, null, null));

    public void Release(Guid publicationId)
    {
    }

    public Task<EventPublication?> FindAsync(Guid publicationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_publications.GetValueOrDefault(publicationId));

    public Task<IReadOnlyList<EventPublication>> AllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EventPublication>>(_publications.Values.OrderByDescending(p => p.RegisteredOn).ToList());

    public IAsyncEnumerable<EventPublication> ReadDueAsync(CancellationToken cancellationToken = default) => throw Delivers();

    public Task<EventPublication> InitializeConsumersAsync(Guid publicationId, IEnumerable<string> consumers, CancellationToken cancellationToken = default) => throw Delivers();

    public Task MarkConsumerCompletedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default) => throw Delivers();

    public Task<EventPublication> RecordConsumerFailureAsync(Guid publicationId, string consumer, string error, int maxAttempts, TimeSpan nextDelay, CancellationToken cancellationToken = default) => throw Delivers();

    public Task<EventPublication> MarkCompletedAsync(Guid publicationId, CancellationToken cancellationToken = default) => throw Delivers();

    public Task ReplayFailedAsync(Guid publicationId, CancellationToken cancellationToken = default) => throw Delivers();

    public Task ReplayFailedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default) => throw Delivers();

    private static NotSupportedException Delivers() =>
        new("The unit tests' outbox only records; delivery is tested against the PostgreSQL outbox.");
}