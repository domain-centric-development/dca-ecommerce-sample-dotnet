using System.Text.Json;
using DcaShop.Backoffice.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

/// <summary>
/// Reads the publication log out of the integration-event outbox.
/// </summary>
/// <remarks>
/// <para>
/// This is where the two samples genuinely differ, and the page says so. The Java sample reads Spring Modulith's
/// <c>EVENT_PUBLICATION</c> table: one row per <i>domain</i> event per listener, completed when that listener
/// returned. This shop has no such registry — its domain events are dispatched in-process and leave no record.
/// What it does keep is the outbox: one row per <i>integration</i> event, with a status, an attempt count and the
/// error of the last failure.
/// </para>
/// <para>
/// So the numbers mean the same thing to an operator — how much is published, how much is through, how much is
/// still owed — but they count different things. A failed publication is reported as incomplete, which is what it
/// is from the log's point of view: it has not been delivered.
/// </para>
/// </remarks>
public sealed class OutboxEventPublicationLogStore : IEventPublicationLogStore
{
    /// <summary>
    /// The outbox delivers to whoever listens rather than to a named listener, so there is no listener id to
    /// show. It is spelled out rather than left blank, because an empty column reads like a defect.
    /// </summary>
    private const string OutboxListener = "integration-event-dispatcher";

    private static readonly JsonSerializerOptions PayloadFormat = new() { WriteIndented = true };

    private readonly IIntegrationEventOutbox _outbox;

    public OutboxEventPublicationLogStore(IIntegrationEventOutbox outbox) => _outbox = outbox;

    public Task<IReadOnlyList<EventPublicationEntry>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = _outbox.All()
            .OrderByDescending(publication => publication.RegisteredOn)
            .Select(ToEntry)
            .ToList();

        return Task.FromResult<IReadOnlyList<EventPublicationEntry>>(entries);
    }

    private static EventPublicationEntry ToEntry(IntegrationEventPublication publication) =>
        new(
            publication.Id,
            publication.Event.GetType().FullName ?? publication.Event.GetType().Name,
            Payload(publication),
            OutboxListener,
            publication.RegisteredOn,
            publication.CompletedOn);

    private static string Payload(IntegrationEventPublication publication)
    {
        var body = Serialize(publication.Event);
        return publication.LastError is { } error
            ? $"{body}{Environment.NewLine}{Environment.NewLine}Last error after {publication.Attempts} attempt(s): {error}"
            : body;
    }

    private static string Serialize(object @event)
    {
        try
        {
            return JsonSerializer.Serialize(@event, @event.GetType(), PayloadFormat);
        }
        catch (NotSupportedException e)
        {
            // The log must render whatever the outbox holds; an event that will not serialize is a curiosity to
            // show, not a reason to fail the page.
            return $"<not serializable: {e.Message}>";
        }
    }
}
