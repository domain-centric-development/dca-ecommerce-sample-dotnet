using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>
/// Implements the <see cref="IIntegrationEventPublisher"/> port by registering the event in the outbox.
/// Delivery happens asynchronously in <c>IntegrationEventDispatcherService</c>, after the publishing use case
/// has returned; failures are retried, not dropped.
/// </summary>
public sealed class OutboxIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IIntegrationEventOutbox _outbox;

    public OutboxIntegrationEventPublisher(IIntegrationEventOutbox outbox)
    {
        _outbox = outbox;
    }

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _outbox.Register(@event);
        return Task.CompletedTask;
    }
}
