using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Synchronous in-process dispatch inside the publishing transaction: the synchronous listeners of the current DI
/// scope are awaited one after another — the Java twin's <c>@EventListener</c>. Transactional listeners
/// (<see cref="ITransactionalEventListener"/>) are skipped here: the outbox delivers to them after the commit.
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
            if (listener is not ITransactionalEventListener && listener.Listens(@event))
            {
                await listener.OnAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}