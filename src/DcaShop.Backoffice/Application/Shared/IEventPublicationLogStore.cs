using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Backoffice.Application.Shared;

/// <summary>
/// Reads the log of what the shop has published. It is a <see cref="IStore"/> and not a repository: the entries
/// are records of an event's delivery, not an aggregate anybody may load and change.
/// </summary>
public interface IEventPublicationLogStore : IStore
{
    Task<IReadOnlyList<EventPublicationEntry>> FindAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>One published event, as the operator sees it.</summary>
public sealed record EventPublicationEntry(
    Guid Id,
    string EventType,
    string SerializedEvent,
    string ListenerId,
    DateTimeOffset PublicationDate,
    DateTimeOffset? CompletionDate)
{
    public bool IsCompleted => CompletionDate is not null;

    /// <summary>The type without its namespace — what the log lists.</summary>
    public string ShortEventType => EventType[(EventType.LastIndexOf('.') + 1)..];
}
