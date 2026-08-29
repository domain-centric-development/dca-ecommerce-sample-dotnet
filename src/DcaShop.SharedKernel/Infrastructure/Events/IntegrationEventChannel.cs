using System.Threading.Channels;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Unbounded channel that decouples publishing an integration event from consuming it (the Java twin's
/// <c>@ApplicationModuleListener</c>: asynchronous, after the publishing unit of work). A background
/// service drains it.
/// </summary>
public sealed class IntegrationEventChannel
{
    private readonly Channel<IIntegrationEvent> _channel = Channel.CreateUnbounded<IIntegrationEvent>();

    public ChannelWriter<IIntegrationEvent> Writer => _channel.Writer;

    public ChannelReader<IIntegrationEvent> Reader => _channel.Reader;
}
