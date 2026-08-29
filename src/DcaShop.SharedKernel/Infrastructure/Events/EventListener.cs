namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Typed base for <see cref="IEventListener"/>: receives every event assignable to <typeparamref name="TEvent"/>.</summary>
public abstract class EventListener<TEvent> : IEventListener
    where TEvent : class
{
    public bool Listens(object @event) => @event is TEvent;

    public Task OnAsync(object @event, CancellationToken cancellationToken = default) =>
        OnAsync((TEvent)@event, cancellationToken);

    protected abstract Task OnAsync(TEvent @event, CancellationToken cancellationToken);
}
