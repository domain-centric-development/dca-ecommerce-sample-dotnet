namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// A listener an event can be handed to. A plain <see cref="IEventListener"/> is the synchronous kind: the in-process
/// dispatcher calls it inside the publishing transaction, and it calls output ports only. A listener that drives an
/// input port is an <see cref="ITransactionalEventListener"/>. Listeners are matched by assignability,
/// so a listener for an interface receives every event implementing it (interface inversion: the
/// consumer owns the contract, the publisher's event implements it).
/// </summary>
public interface IEventListener
{
    bool Listens(object @event);

    Task OnAsync(object @event, CancellationToken cancellationToken = default);
}