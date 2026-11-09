using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>
/// Implements the <see cref="IIntegrationEventPublisher"/> port with the outbox pattern: the publication is
/// registered <em>inside</em> the use case's transaction — together with the aggregate, so there is no window in
/// which the aggregate is committed but the event is not recorded. After commit the publication is released to
/// the dispatcher; a rollback takes the publication with it. Delivery
/// happens asynchronously in <c>EventDispatcherService</c>; failures are retried, not dropped.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IEventOutbox _outbox;
    private readonly ITransactionHooks _transaction;

    public OutboxIntegrationEventPublisher(IEventOutbox outbox, ITransactionHooks transaction)
    {
        _outbox = outbox;
        _transaction = transaction;
    }

    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var publication = await _outbox.RegisterAsync(@event, cancellationToken).ConfigureAwait(false);
        _transaction.AfterCommit(() => _outbox.Release(publication.Id));
    }
}