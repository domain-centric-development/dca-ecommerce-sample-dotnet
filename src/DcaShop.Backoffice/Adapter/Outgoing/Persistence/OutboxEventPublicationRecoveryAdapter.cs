using DcaShop.Backoffice.Application.ReplayFailedPublication;
using DcaShop.SharedKernel.Infrastructure.Events;
namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

public sealed class OutboxEventPublicationRecoveryAdapter(IEventOutbox outbox) : IEventPublicationRecoveryPort
{
    public Task<bool> ReplayFailedAsync(Guid publicationId, string listenerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var publication = outbox.All().SingleOrDefault(p => p.Id == publicationId);
        if (publication is null || !publication.Consumers.ContainsKey(listenerId)) return Task.FromResult(false);
        outbox.ReplayFailed(publicationId, listenerId);
        return Task.FromResult(true);
    }
}