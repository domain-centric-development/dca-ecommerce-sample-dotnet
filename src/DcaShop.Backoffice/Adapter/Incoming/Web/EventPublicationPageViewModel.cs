using System.Globalization;
using System.Text.Json;
using DcaShop.Backoffice.Application.GetEventPublications;

namespace DcaShop.Backoffice.Adapter.Incoming.Web;

/// <summary>What the event log page shows.</summary>
public sealed record EventPublicationPageViewModel(
    int TotalEvents,
    int CompletedCount,
    int IncompleteCount,
    IReadOnlyList<EventPublicationPageViewModel.Item> Events)
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public static EventPublicationPageViewModel From(GetEventPublicationsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new EventPublicationPageViewModel(
            result.TotalCount,
            result.CompletedCount,
            result.IncompleteCount,
            result.Entries.Select(Item.From).ToList());
    }

    /// <summary>One row of the log.</summary>
    public sealed record Item(
        string Id,
        string ShortEventType,
        string FullEventType,
        string SerializedEvent,
        string ListenerId,
        string PublicationDate,
        string? CompletionDate,
        bool Completed,
        string StatusLabel)
    {
        internal static Item From(EventPublicationSummary summary) =>
            new(
                summary.Id.ToString(),
                summary.ShortEventType,
                summary.EventType,
                PrettyPrint(summary.SerializedEvent),
                summary.ListenerId,
                Format(summary.PublicationDate)!,
                Format(summary.CompletionDate),
                summary.IsCompleted,
                summary.IsCompleted ? "Completed" : "Incomplete");

        private static string? Format(DateTimeOffset? value) =>
            value?.LocalDateTime.ToString(DateFormat, CultureInfo.InvariantCulture);

        private static string PrettyPrint(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return payload;
            }

            try
            {
                using var parsed = JsonDocument.Parse(payload);
                return JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException)
            {
                // The payload may carry a trailing error note, which is no longer valid JSON. Showing it as it
                // is beats hiding it behind a parse failure.
                return payload;
            }
        }
    }
}
