using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>
/// Implements the <see cref="IIntegrationEventPublisher"/> port by registering the event in the outbox — after the
/// current unit of work committed, so a rolled-back use case publishes nothing (the in-process equivalent of an
/// outbox row written in the aggregate's transaction). Delivery happens asynchronously in
/// <c>IntegrationEventDispatcherService</c>; failures are retried, not dropped.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IIntegrationEventOutbox _outbox;
    private readonly ITransactionHooks _transaction;

    public OutboxIntegrationEventPublisher(IIntegrationEventOutbox outbox, ITransactionHooks transaction)
    {
        _outbox = outbox;
        _transaction = transaction;
    }

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _transaction.AfterCommit(() => _outbox.Register(@event));
        return Task.CompletedTask;
    }
}
