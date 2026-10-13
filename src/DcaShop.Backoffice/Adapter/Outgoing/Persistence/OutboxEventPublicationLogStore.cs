using System.Text.Json;

using DcaShop.Backoffice.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

/// <summary>
/// Reads the publication log out of the integration-event outbox.
/// </summary>
/// <remarks>
/// The outbox retains one immutable integration-event snapshot with per-consumer completion; the log lists one entry
/// per event and listener, each with that listener's own state, as Java's registry does with one row per listener.
/// Staged records are visible to operators but not eligible for dispatch until commit.
/// </remarks>
public sealed class OutboxEventPublicationLogStore : IEventPublicationLogStore
{
    // Before the first dispatch captures consumer identities, display the dispatcher placeholder.
    private const string OutboxListener = "integration-event-dispatcher";

    private static readonly JsonSerializerOptions PayloadFormat = new() { WriteIndented = true };

    private readonly IIntegrationEventOutbox _outbox;

    public OutboxEventPublicationLogStore(IIntegrationEventOutbox outbox) => _outbox = outbox;

    public Task<IReadOnlyList<EventPublicationEntry>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = _outbox.All()
            .OrderByDescending(publication => publication.RegisteredOn)
            .SelectMany(ToEntries)
            .ToList();

        return Task.FromResult<IReadOnlyList<EventPublicationEntry>>(entries);
    }

    private static IEnumerable<EventPublicationEntry> ToEntries(IntegrationEventPublication publication) =>
        Deliveries(publication).Select(delivery => ToEntry(publication, delivery));

    /// <summary>Each consumer's delivery; before any is known, the publication's own state under the placeholder.</summary>
    private static IEnumerable<ConsumerDelivery> Deliveries(IntegrationEventPublication publication) =>
        publication.Consumers.Count == 0
            ? [new ConsumerDelivery(OutboxListener, publication.Status, publication.Attempts, publication.LastError, publication.CompletedOn, null)]
            : publication.Consumers.Values.OrderBy(delivery => delivery.ConsumerId, StringComparer.Ordinal);

    private static EventPublicationEntry ToEntry(IntegrationEventPublication publication, ConsumerDelivery delivery) =>
        new(
            publication.Id,
            publication.Event.GetType().FullName ?? publication.Event.GetType().Name,
            Payload(publication, delivery),
            delivery.ConsumerId,
            publication.RegisteredOn,
            delivery.CompletedOn, delivery.Status.ToString());

    private static string Payload(IntegrationEventPublication publication, ConsumerDelivery delivery)
    {
        var heading = $"Status: {delivery.Status.ToString().ToUpperInvariant()}, attempts: {Tries(delivery)}";
        var body = $"{heading}{Environment.NewLine}{Serialize(publication.Event)}";
        return delivery.LastError is { } error
            ? $"{body}{Environment.NewLine}{Environment.NewLine}Last error after {delivery.Attempts} attempt(s): {error}"
            : body;
    }

    // The outbox counts failed deliveries; the operator reads every try, the successful one included.
    private static int Tries(ConsumerDelivery delivery) =>
        delivery.Attempts + (delivery.Status == PublicationStatus.Completed ? 1 : 0);

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