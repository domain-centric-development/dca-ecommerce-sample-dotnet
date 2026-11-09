using DcaShop.Backoffice.Application.ReplayFailedPublication;
using DcaShop.SharedKernel.Infrastructure.Events;
namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

public sealed class OutboxEventPublicationRecoveryAdapter(IEventOutbox outbox) : IEventPublicationRecoveryPort
{
    public async Task<bool> ReplayFailedAsync(Guid publicationId, string listenerId, CancellationToken cancellationToken = default)
    {
        var publication = await outbox.FindAsync(publicationId, cancellationToken).ConfigureAwait(false);
        if (publication is null || !publication.Consumers.ContainsKey(listenerId)) return false;
        await outbox.ReplayFailedAsync(publicationId, listenerId, cancellationToken).ConfigureAwait(false);
        return true;
    }
}