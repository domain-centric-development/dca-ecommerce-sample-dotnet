using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.TestSupport;

/// <summary>The outbox as an assertion reads it: what it holds right now.</summary>
public static class OutboxReading
{
    /// <summary>Every publication, newest first.</summary>
    public static IReadOnlyList<EventPublication> All(this IEventOutbox outbox) => outbox.AllAsync().GetAwaiter().GetResult();
}