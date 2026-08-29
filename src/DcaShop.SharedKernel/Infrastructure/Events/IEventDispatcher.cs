namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Delivers one event to every registered listener that listens to it.</summary>
public interface IEventDispatcher
{
    Task DispatchAsync(object @event, CancellationToken cancellationToken = default);
}
