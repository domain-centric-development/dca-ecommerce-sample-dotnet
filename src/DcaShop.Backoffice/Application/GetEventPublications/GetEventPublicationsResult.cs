using DcaShop.Backoffice.Application.Shared;

namespace DcaShop.Backoffice.Application.GetEventPublications;

/// <summary>The log, with the three numbers the operator looks at first.</summary>
public sealed record GetEventPublicationsResult(
    IReadOnlyList<EventPublicationSummary> Entries,
    int TotalCount,
    int CompletedCount,
    int IncompleteCount)
{
    public static GetEventPublicationsResult From(IReadOnlyList<EventPublicationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var summaries = entries.Select(EventPublicationSummary.From).ToList();
        var completed = summaries.Count(s => s.IsCompleted);
        return new GetEventPublicationsResult(summaries, summaries.Count, completed, summaries.Count - completed);
    }
}

/// <summary>One entry of the log, as the page needs it.</summary>
public sealed record EventPublicationSummary(
    Guid Id,
    string EventType,
    string SerializedEvent,
    string ListenerId,
    DateTimeOffset PublicationDate,
    DateTimeOffset? CompletionDate)
{
    public static EventPublicationSummary From(EventPublicationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new EventPublicationSummary(
            entry.Id,
            entry.EventType,
            entry.SerializedEvent,
            entry.ListenerId,
            entry.PublicationDate,
            entry.CompletionDate);
    }

    public bool IsCompleted => CompletionDate is not null;

    public string ShortEventType => EventType[(EventType.LastIndexOf('.') + 1)..];
}
