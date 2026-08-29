using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Adapter.Outgoing.Event;

/// <summary>Hands integration events to the channel; consumers in other contexts receive them asynchronously.</summary>
public sealed class ChannelIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IntegrationEventChannel _channel;

    public ChannelIntegrationEventPublisher(IntegrationEventChannel channel)
    {
        _channel = channel;
    }

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(@event, cancellationToken).AsTask();
}
