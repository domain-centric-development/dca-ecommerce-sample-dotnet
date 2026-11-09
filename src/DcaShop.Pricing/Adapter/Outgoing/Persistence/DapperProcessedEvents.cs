using Dapper;

using DcaShop.Pricing.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Persistence;

namespace DcaShop.Pricing.Adapter.Outgoing.Persistence;

/// <summary>
/// The produced effects as rows of <c>pricing_processed_events</c>, keyed by effect and event id. Written in the
/// transaction of the effect it records, so an effect and its record commit or roll back together.
/// </summary>
public sealed class DapperProcessedEvents : IProcessedEvents
{
    private readonly Database _database;

    public DapperProcessedEvents(Database database) => _database = database;

    public Task<bool> ContainsAsync(string effect, Guid eventId, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM pricing_processed_events WHERE effect = @Effect AND event_id = @EventId)",
            new { Effect = effect, EventId = eventId }, transaction, cancellationToken: cancellationToken)), cancellationToken);

    public Task MarkProcessedAsync(string effect, Guid eventId, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO pricing_processed_events (effect, event_id) VALUES (@Effect, @EventId) ON CONFLICT DO NOTHING",
            new { Effect = effect, EventId = eventId }, transaction, cancellationToken: cancellationToken)), cancellationToken);
}