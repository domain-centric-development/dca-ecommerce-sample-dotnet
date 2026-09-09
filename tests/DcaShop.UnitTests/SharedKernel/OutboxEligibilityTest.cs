using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.UnitTests.SharedKernel;

public sealed class OutboxEligibilityTest
{
    [Fact]
    public async Task RegisteredUncommittedPublicationIsNotEligibleForStartupReplay()
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        outbox.Register(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await using var due = outbox.ReadDueAsync(timeout.Token).GetAsyncEnumerator();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await due.MoveNextAsync());
    }

    [Fact]
    public async Task AFailedCommitParticipantRollsBackEligibilityBeforeReadersCanObserveIt()
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var boundary = new DcaShop.SharedKernel.Infrastructure.Transactions.InMemoryTransactionBoundary();
        var publisher = new DcaShop.SharedKernel.Adapter.Outgoing.Event.OutboxIntegrationEventPublisher(outbox, boundary);
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow), ct);
            boundary.EnlistCommit(() => throw new InvalidOperationException("participant failed"));
            return 0;
        }));
        Assert.Empty(outbox.All());
    }

    [Fact]
    public async Task FailedWakeupCannotUndoACommittedPublication()
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var boundary = new DcaShop.SharedKernel.Infrastructure.Transactions.InMemoryTransactionBoundary();
        var publisher = new DcaShop.SharedKernel.Adapter.Outgoing.Event.OutboxIntegrationEventPublisher(outbox, boundary);
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow), ct);
            boundary.AfterCommit(() => throw new InvalidOperationException("wakeup failed"));
            return 0;
        }));
        Assert.Equal(PublicationStatus.Pending, outbox.All().Single().Status);
    }

    [Fact]
    public async Task ModeledAggregateParticipantAndInsidePublicationRollBackButOutsidePublicationSurvives()
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var boundary = new DcaShop.SharedKernel.Infrastructure.Transactions.InMemoryTransactionBoundary();
        var publisher = new DcaShop.SharedKernel.Adapter.Outgoing.Event.OutboxIntegrationEventPublisher(outbox, boundary);
        DcaShop.Cart.Domain.Model.ShoppingCart? stored = null;
        var cart = new DcaShop.Cart.Domain.Model.ShoppingCart(DcaShop.Cart.Domain.Model.CartId.Generate(), DcaShop.Cart.Domain.Model.CustomerId.Of("participant"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            boundary.EnlistCommit(() => stored = cart);
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow), ct);
            throw new InvalidOperationException("rollback");
        }));
        Assert.Null(stored); Assert.Empty(outbox.All());
        await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(ct =>
        {
            boundary.EnlistCommit(() => stored = cart);
            throw new InvalidOperationException("later rollback");
        }));
        Assert.Null(stored); Assert.Equal(PublicationStatus.Pending, outbox.All().Single().Status);
    }

    private sealed record ProbeEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;
}
