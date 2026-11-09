using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>
/// Publishes domain events to both listener kinds and clears them from the aggregate. The synchronous listeners run
/// now, inside the use case's transaction — the translators that capture integration events. When a transactional
/// listener listens to the event, the event is also captured in the outbox inside the same transaction, so it commits
/// and rolls back with the use case's work; the dispatcher delivers it after the commit.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IEventOutbox _outbox;
    private readonly ITransactionHooks _transaction;
    private readonly IServiceProvider _services;

    public DomainEventPublisher(IEventDispatcher dispatcher, IEventOutbox outbox, ITransactionHooks transaction, IServiceProvider services)
    {
        _dispatcher = dispatcher;
        _outbox = outbox;
        _transaction = transaction;
        _services = services;
    }

    public async Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        await _dispatcher.DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
        await CaptureAsync(@event, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        // Dispatch first, clear last: clearing is the acknowledgement that every synchronous listener has seen the
        // event and that every event a transactional listener waits for is captured.
        foreach (var @event in aggregate.DomainEvents.ToArray())
        {
            await _dispatcher.DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
            await CaptureAsync(@event, cancellationToken).ConfigureAwait(false);
        }

        aggregate.ClearDomainEvents();
    }

    private async Task CaptureAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        if (!_services.GetServices<IEventListener>().OfType<ITransactionalEventListener>().Any(l => l.Listens(@event)))
        {
            return;
        }

        var publication = await _outbox.RegisterAsync(@event, cancellationToken).ConfigureAwait(false);
        _transaction.AfterCommit(() => _outbox.Release(publication.Id));
    }
}