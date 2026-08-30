using DcaShop.Backoffice.Application.GetEventPublications;
using DcaShop.Backoffice.Application.Shared;

namespace DcaShop.UnitTests.Backoffice;

/// <summary>
/// The three numbers the operator reads off the event log. They are the whole point of the page, so they get
/// asserted rather than eyeballed: total, completed, and the remainder that still owes a delivery.
/// </summary>
public sealed class GetEventPublicationsTest
{
    [Fact]
    public async Task TheSummaryCountsCompletedAndIncompleteSeparately()
    {
        var log = new StubLog(
            Entry("CheckoutConfirmedEvent", completed: true),
            Entry("CartCheckedOutEvent", completed: true),
            Entry("ProductCreatedEvent", completed: false));

        var result = await new GetEventPublicationsUseCase(log).ExecuteAsync(new GetEventPublicationsQuery());

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.CompletedCount);
        Assert.Equal(1, result.IncompleteCount);
        Assert.Equal(3, result.Entries.Count);
    }

    [Fact]
    public async Task AnEmptyLogCountsToZeroRatherThanFailing()
    {
        var result = await new GetEventPublicationsUseCase(new StubLog()).ExecuteAsync(new GetEventPublicationsQuery());

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.CompletedCount);
        Assert.Equal(0, result.IncompleteCount);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void TheListShowsTheEventTypeWithoutItsNamespace()
    {
        var summary = EventPublicationSummary.From(
            Entry("DcaShop.Checkout.Events.CheckoutConfirmedEvent", completed: false));

        Assert.Equal("CheckoutConfirmedEvent", summary.ShortEventType);
        Assert.False(summary.IsCompleted);
    }

    private static EventPublicationEntry Entry(string eventType, bool completed) =>
        new(
            Guid.NewGuid(),
            eventType,
            "{}",
            "integration-event-dispatcher",
            DateTimeOffset.UnixEpoch,
            completed ? DateTimeOffset.UnixEpoch.AddSeconds(1) : null);

    private sealed class StubLog : IEventPublicationLogStore
    {
        private readonly IReadOnlyList<EventPublicationEntry> _entries;

        public StubLog(params EventPublicationEntry[] entries) => _entries = entries;

        public Task<IReadOnlyList<EventPublicationEntry>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries);
    }
}
