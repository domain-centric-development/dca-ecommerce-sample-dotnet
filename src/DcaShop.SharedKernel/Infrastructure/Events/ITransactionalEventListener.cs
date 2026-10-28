namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// The second listener kind: delivered after the publishing transaction committed, in a scope and transaction of its
/// own, from a publication the outbox captured in that transaction. A listener that drives an input port is one of
/// these; a listener that only translates or measures stays a plain <see cref="IEventListener"/> and runs inside the
/// publishing transaction. The in-process dispatcher skips this kind; the outbox dispatcher delivers only to it.
/// </summary>
public interface ITransactionalEventListener : IEventListener
{
}

/// <summary>Typed base for <see cref="ITransactionalEventListener"/>: receives every event assignable to <typeparamref name="TEvent"/>.</summary>
public abstract class TransactionalEventListener<TEvent> : EventListener<TEvent>, ITransactionalEventListener
    where TEvent : class
{
}