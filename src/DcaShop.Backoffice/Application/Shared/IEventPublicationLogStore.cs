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

/// <summary>
/// One delivery of a published event to one listener, as the operator sees it; <see cref="Id"/> is the publication's,
/// shared by its listeners' entries.
/// </summary>
public sealed record EventPublicationEntry(
    Guid Id,
    string EventType,
    string SerializedEvent,
    string ListenerId,
    DateTimeOffset PublicationDate,
    DateTimeOffset? CompletionDate,
    PublicationStatus Status = PublicationStatus.Pending)
{
    public bool IsCompleted => CompletionDate is not null;

    /// <summary>The type without its namespace — what the log lists.</summary>
    public string ShortEventType => EventType[(EventType.LastIndexOf('.') + 1)..];
}

/// <summary>The status of a delivery to one listener, as the outbox keeps it.</summary>
public enum PublicationStatus
{
    /// <summary>Registered inside the publishing use case's transaction, not yet released.</summary>
    Staged,

    /// <summary>Released and waiting for its delivery, a first one or a retry.</summary>
    Pending,

    /// <summary>Delivered.</summary>
    Completed,

    /// <summary>Given up after its retries; the one an operator can retry from the log.</summary>
    Failed,
}