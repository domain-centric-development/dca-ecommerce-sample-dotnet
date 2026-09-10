using DcaShop.Backoffice.Application.ReplayFailedPublication;
using DcaShop.SharedKernel.Infrastructure.Events;
namespace DcaShop.Backoffice.Adapter.Outgoing.Persistence;

public sealed class OutboxEventPublicationRecoveryAdapter(IIntegrationEventOutbox outbox) : IEventPublicationRecoveryPort
{
    public Task<bool> ReplayFailedAsync(Guid publicationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var publication = outbox.All().SingleOrDefault(p => p.Id == publicationId);
        if (publication is null) return Task.FromResult(false);
        outbox.ReplayFailed(publicationId);
        return Task.FromResult(true);
    }
}
