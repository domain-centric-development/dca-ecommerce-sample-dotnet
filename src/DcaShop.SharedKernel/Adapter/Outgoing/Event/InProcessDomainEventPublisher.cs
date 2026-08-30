using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>Publishes domain events to in-process listeners and clears them from the aggregate.</summary>
public sealed class InProcessDomainEventPublisher : IDomainEventPublisher
{
    private readonly IEventDispatcher _dispatcher;

    public InProcessDomainEventPublisher(IEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default) =>
        _dispatcher.DispatchAsync(@event, cancellationToken);

    public async Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        // Dispatch first, clear last: clearing is the acknowledgement that every listener has seen the event.
        // If a listener throws, the events stay on the aggregate and the failure surfaces in the use case.
        foreach (var @event in aggregate.DomainEvents.ToArray())
        {
            await _dispatcher.DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
        }

        aggregate.ClearDomainEvents();
    }
}
