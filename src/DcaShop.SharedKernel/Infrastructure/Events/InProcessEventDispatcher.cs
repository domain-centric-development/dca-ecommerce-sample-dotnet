using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Synchronous in-process dispatch: listeners are resolved from the current DI scope and awaited one
/// after another. Domain events travel this way (the Java twin's <c>@EventListener</c>); integration
/// events reach it through the <see cref="IntegrationEventChannel"/> in a background service.
/// </summary>
public sealed class InProcessEventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _services;

    public InProcessEventDispatcher(IServiceProvider services)
    {
        _services = services;
    }

    public async Task DispatchAsync(object @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        foreach (var listener in _services.GetServices<IEventListener>())
        {
            if (listener.Listens(@event))
            {
                await listener.OnAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
