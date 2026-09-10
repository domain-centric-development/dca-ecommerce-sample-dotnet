using System.Text.Json;
using DcaShop.Backoffice.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

/// <summary>
/// Reads the publication log out of the integration-event outbox.
/// </summary>
/// <remarks>
/// The outbox retains one immutable integration-event snapshot with per-consumer completion.
/// Staged records are visible to operators but not eligible for dispatch until commit.
/// Java's registry uses one publication row per listener; either mechanism can carry integration contracts.
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
            .Select(ToEntry)
            .ToList();

        return Task.FromResult<IReadOnlyList<EventPublicationEntry>>(entries);
    }

    private static EventPublicationEntry ToEntry(IntegrationEventPublication publication) =>
        new(
            publication.Id,
            publication.Event.GetType().FullName ?? publication.Event.GetType().Name,
            Payload(publication),
            publication.Consumers.Count == 0 ? OutboxListener : string.Join(", ", publication.Consumers.Keys),
            publication.RegisteredOn,
            publication.CompletedOn, publication.Status.ToString());

    private static string Payload(IntegrationEventPublication publication)
    {
        var progress = string.Join(Environment.NewLine, publication.Consumers.Values.Select(c =>
            $"{c.ConsumerId}: {c.Status}, failed attempts {c.Attempts}, next {c.NextAttemptOn}, error {c.LastError}"));
        var body = $"Status: {publication.Status}{Environment.NewLine}{Serialize(publication.Event)}{Environment.NewLine}{progress}";
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
