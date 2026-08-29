namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// A listener the in-process dispatcher can hand an event to. Listeners are matched by assignability,
/// so a listener for an interface receives every event implementing it (interface inversion: the
/// consumer owns the contract, the publisher's event implements it).
/// </summary>
public interface IEventListener
{
    bool Listens(object @event);

    Task OnAsync(object @event, CancellationToken cancellationToken = default);
}
