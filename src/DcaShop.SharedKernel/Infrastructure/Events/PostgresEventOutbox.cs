using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

using Dapper;

using DcaShop.SharedKernel.Infrastructure.Persistence;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Npgsql;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// The outbox as two tables: <c>event_publications</c> holds each snapshot, <c>event_publication_deliveries</c> each
/// consumer's progress. A publication is inserted in the transaction of the work that caused it, so it commits and
/// rolls back with that work; delivery state changes in a transaction of the boundary's, under a row lock on the
/// publication.
/// </summary>
/// <remarks>
/// The dispatcher is woken in process (<see cref="Release"/> after the commit, a timer for a retry that is due later)
/// and reads every pending publication when it starts, so a publication a stopped process left behind is delivered
/// by the next one.
/// </remarks>
public sealed class PostgresEventOutbox : IEventOutbox
{
    private const string Columns =
        "id AS Id, event_type AS EventType, payload AS Payload, registered_on AS RegisteredOn, status AS Status, " +
        "attempts AS Attempts, last_error AS LastError, completed_on AS CompletedOn, consumers_initialized AS ConsumersInitialized";

    private readonly Database _database;
    private readonly PostgresTransactionBoundary _boundary;
    private readonly TimeProvider _clock;
    private readonly Channel<Guid> _due = Channel.CreateUnbounded<Guid>();

    public PostgresEventOutbox(Database database, PostgresTransactionBoundary boundary, TimeProvider clock)
    {
        _database = database;
        _boundary = boundary;
        _clock = clock;
    }

    public Task<EventPublication> RegisterAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return RegisterAsync(@event, @event.EventId, cancellationToken);
    }

    /// <remarks>
    /// A publication of its own, not keyed by the event id: the integration event a translator makes of this domain
    /// event carries the same event id and is registered in the same transaction, and keyed alike the two would be one
    /// publication. The consumers' idempotency reads the event id from the event itself.
    /// </remarks>
    public Task<EventPublication> RegisterAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return RegisterAsync(@event, Guid.NewGuid(), cancellationToken);
    }

    private async Task<EventPublication> RegisterAsync(object @event, Guid publicationId, CancellationToken cancellationToken)
    {
        var publication = new EventPublication(publicationId, JsonSerializer.Serialize(@event, @event.GetType(), EventJson.Options),
            @event.GetType(), _clock.GetUtcNow(), PublicationStatus.Pending, 0, null, null);
        // Validate deserialization before registering an undeliverable snapshot.
        _ = publication.Event;
        var inserted = await _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO event_publications (id, event_type, payload, registered_on, status, attempts, consumers_initialized)
            VALUES (@Id, @EventType, @Payload, @RegisteredOn, @Status, 0, FALSE)
            ON CONFLICT (id) DO NOTHING
            """,
            new
            {
                publication.Id,
                EventType = TypeName(publication.EventType),
                publication.Payload,
                RegisteredOn = StoredTime.Write(publication.RegisteredOn),
                Status = publication.Status.ToString(),
            },
            transaction, cancellationToken: cancellationToken)), cancellationToken).ConfigureAwait(false);
        return inserted == 1
            ? publication
            : await FindAsync(publicationId, cancellationToken).ConfigureAwait(false) ?? publication;
    }

    public void Release(Guid publicationId) => _due.Writer.TryWrite(publicationId);

    public async IAsyncEnumerable<EventPublication> ReadDueAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Every dispatcher that starts enumerates what is retained; wakeups are dispensable.
        var pending = await _database.RunAsync((connection, transaction) => connection.QueryAsync<Guid>(new CommandDefinition(
            "SELECT id FROM event_publications WHERE status = 'Pending' ORDER BY registered_on",
            transaction: transaction, cancellationToken: cancellationToken)), cancellationToken).ConfigureAwait(false);
        foreach (var id in pending) Release(id);

        await foreach (var id in _due.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            var publication = await FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (publication?.Status != PublicationStatus.Pending) continue;
            var now = _clock.GetUtcNow();
            var waiting = publication.Consumers.Values.Where(c => c.Status == PublicationStatus.Pending).ToArray();
            var next = waiting.Length == 0 ? now : waiting.Min(c => c.NextAttemptOn ?? now);
            if (next > now)
            {
                _ = WakeLater(id, next - now, cancellationToken);
                continue;
            }

            yield return publication;
        }
    }

    private async Task WakeLater(Guid id, TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, _clock, token).ConfigureAwait(false);
            Release(id);
        }
        catch (OperationCanceledException)
        {
            // eligibility and due time remain in storage
        }
    }

    public Task<EventPublication> InitializeConsumersAsync(Guid publicationId, IEnumerable<string> consumers, CancellationToken cancellationToken = default)
    {
        var chosen = consumers.Distinct(StringComparer.Ordinal).ToArray();
        return UpdateAsync(publicationId, p => p.ConsumersInitialized ? p : p with
        {
            ConsumersInitialized = true,
            Consumers = chosen.ToImmutableDictionary(c => c, c => new ConsumerDelivery(c, PublicationStatus.Pending, 0, null, null, null)),
        }, cancellationToken);
    }

    public Task MarkConsumerCompletedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default) =>
        UpdateAsync(publicationId, p => Progress(p with
        {
            Consumers = p.Consumers.SetItem(consumer, p.Consumers[consumer] with
            {
                Status = PublicationStatus.Completed,
                CompletedOn = _clock.GetUtcNow(),
                LastError = null,
                NextAttemptOn = null,
            }),
        }), cancellationToken);

    public Task<EventPublication> RecordConsumerFailureAsync(Guid publicationId, string consumer, string error, int maxAttempts,
        TimeSpan nextDelay, CancellationToken cancellationToken = default) =>
        UpdateAsync(publicationId, p =>
        {
            var state = p.Consumers[consumer];
            var attempts = state.Attempts + 1;
            return Progress(p with
            {
                Attempts = p.Attempts + 1,
                LastError = error,
                Consumers = p.Consumers.SetItem(consumer, state with
                {
                    Attempts = attempts,
                    LastError = error,
                    Status = attempts >= maxAttempts ? PublicationStatus.Failed : PublicationStatus.Pending,
                    NextAttemptOn = attempts >= maxAttempts ? null : _clock.GetUtcNow() + nextDelay,
                }),
            });
        }, cancellationToken);

    public Task<EventPublication> MarkCompletedAsync(Guid publicationId, CancellationToken cancellationToken = default) =>
        UpdateAsync(publicationId, Progress, cancellationToken);

    private EventPublication Progress(EventPublication p)
    {
        var complete = p.ConsumersInitialized && p.Consumers.Values.All(c => c.Status == PublicationStatus.Completed);
        var pending = !p.ConsumersInitialized || p.Consumers.Values.Any(c => c.Status == PublicationStatus.Pending);
        return p with
        {
            Status = complete ? PublicationStatus.Completed : pending ? PublicationStatus.Pending : PublicationStatus.Failed,
            CompletedOn = complete ? _clock.GetUtcNow() : null,
            LastError = complete ? null : p.LastError,
        };
    }

    public Task ReplayFailedAsync(Guid publicationId, CancellationToken cancellationToken = default) =>
        ReplayAsync(publicationId, _ => true, cancellationToken);

    public Task ReplayFailedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default) =>
        ReplayAsync(publicationId, c => c.ConsumerId == consumer, cancellationToken);

    private async Task ReplayAsync(Guid publicationId, Func<ConsumerDelivery, bool> chosen, CancellationToken cancellationToken)
    {
        static bool Failed(ConsumerDelivery c) => c.Status == PublicationStatus.Failed;
        await UpdateAsync(publicationId, p => !p.Consumers.Values.Any(c => Failed(c) && chosen(c)) ? p : Progress(p with
        {
            Consumers = p.Consumers.ToImmutableDictionary(e => e.Key,
                e => Failed(e.Value) && chosen(e.Value) ? e.Value with { Status = PublicationStatus.Pending, Attempts = 0, NextAttemptOn = null } : e.Value),
        }), cancellationToken).ConfigureAwait(false);
        Release(publicationId);
    }

    public Task<EventPublication?> FindAsync(Guid publicationId, CancellationToken cancellationToken = default) =>
        _database.RunAsync(async (connection, transaction) =>
            (await LoadAsync(connection, transaction, "WHERE id = @Id", new { Id = publicationId }, cancellationToken)
                .ConfigureAwait(false)).SingleOrDefault(), cancellationToken);

    public Task<IReadOnlyList<EventPublication>> AllAsync(CancellationToken cancellationToken = default) =>
        _database.RunAsync<IReadOnlyList<EventPublication>>(
            (connection, transaction) => LoadAsync(connection, transaction, "", null, cancellationToken), cancellationToken);

    /// <summary>Applies <paramref name="change"/> under a row lock and writes the result back, in one transaction.</summary>
    private Task<EventPublication> UpdateAsync(Guid publicationId, Func<EventPublication, EventPublication> change, CancellationToken cancellationToken) =>
        _boundary.InTransactionAsync(ct => _database.RunAsync(async (connection, transaction) =>
        {
            var current = (await LoadAsync(connection, transaction, "WHERE id = @Id FOR UPDATE", new { Id = publicationId }, cancellationToken)
                .ConfigureAwait(false)).SingleOrDefault() ?? throw new KeyNotFoundException($"Unknown publication: {publicationId}");
            var updated = change(current);
            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE event_publications
                SET status = @Status, attempts = @Attempts, last_error = @LastError, completed_on = @CompletedOn,
                    consumers_initialized = @ConsumersInitialized
                WHERE id = @Id
                """,
                new
                {
                    updated.Id,
                    Status = updated.Status.ToString(),
                    updated.Attempts,
                    updated.LastError,
                    CompletedOn = StoredTime.Write(updated.CompletedOn),
                    updated.ConsumersInitialized,
                },
                transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            foreach (var delivery in updated.Consumers.Values)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO event_publication_deliveries
                        (publication_id, consumer_id, status, attempts, last_error, completed_on, next_attempt_on)
                    VALUES (@PublicationId, @ConsumerId, @Status, @Attempts, @LastError, @CompletedOn, @NextAttemptOn)
                    ON CONFLICT (publication_id, consumer_id) DO UPDATE
                    SET status = EXCLUDED.status, attempts = EXCLUDED.attempts, last_error = EXCLUDED.last_error,
                        completed_on = EXCLUDED.completed_on, next_attempt_on = EXCLUDED.next_attempt_on
                    """,
                    new
                    {
                        PublicationId = updated.Id,
                        delivery.ConsumerId,
                        Status = delivery.Status.ToString(),
                        delivery.Attempts,
                        delivery.LastError,
                        CompletedOn = StoredTime.Write(delivery.CompletedOn),
                        NextAttemptOn = StoredTime.Write(delivery.NextAttemptOn),
                    },
                    transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            return updated;
        }, ct), cancellationToken);

    private static async Task<IReadOnlyList<EventPublication>> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string condition, object? parameters, CancellationToken cancellationToken)
    {
        var rows = (await connection.QueryAsync<PublicationRow>(new CommandDefinition(
            $"SELECT {Columns} FROM event_publications {condition}",
            parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
        if (rows.Count == 0) return [];

        var deliveries = (await connection.QueryAsync<DeliveryRow>(new CommandDefinition(
                """
                SELECT publication_id AS PublicationId, consumer_id AS ConsumerId, status AS Status, attempts AS Attempts,
                       last_error AS LastError, completed_on AS CompletedOn, next_attempt_on AS NextAttemptOn
                FROM event_publication_deliveries WHERE publication_id = ANY(@Ids)
                """,
                new { Ids = rows.Select(r => r.Id).ToArray() }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false))
            .ToLookup(d => d.PublicationId);

        return rows
            .Select(row => new EventPublication(row.Id, row.Payload, ResolveType(row.EventType), StoredTime.Read(row.RegisteredOn),
                Enum.Parse<PublicationStatus>(row.Status), row.Attempts, row.LastError, StoredTime.Read(row.CompletedOn))
            {
                ConsumersInitialized = row.ConsumersInitialized,
                Consumers = deliveries[row.Id].ToImmutableDictionary(d => d.ConsumerId, d => new ConsumerDelivery(d.ConsumerId,
                    Enum.Parse<PublicationStatus>(d.Status), d.Attempts, d.LastError, StoredTime.Read(d.CompletedOn), StoredTime.Read(d.NextAttemptOn))),
            })
            .OrderByDescending(p => p.RegisteredOn)
            .ToList();
    }

    /// <summary>The type's full name and its assembly, without a version: a new build still reads what an old one wrote.</summary>
    private static string TypeName(Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";

    private static Type ResolveType(string name) =>
        Type.GetType(name) ?? throw new InvalidOperationException($"The outbox holds an event of an unknown type: {name}");

    private sealed class PublicationRow
    {
        public Guid Id { get; init; }
        public string EventType { get; init; } = "";
        public string Payload { get; init; } = "";
        public DateTime RegisteredOn { get; init; }
        public string Status { get; init; } = "";
        public int Attempts { get; init; }
        public string? LastError { get; init; }
        public DateTime? CompletedOn { get; init; }
        public bool ConsumersInitialized { get; init; }
    }

    private sealed class DeliveryRow
    {
        public Guid PublicationId { get; init; }
        public string ConsumerId { get; init; } = "";
        public string Status { get; init; } = "";
        public int Attempts { get; init; }
        public string? LastError { get; init; }
        public DateTime? CompletedOn { get; init; }
        public DateTime? NextAttemptOn { get; init; }
    }
}