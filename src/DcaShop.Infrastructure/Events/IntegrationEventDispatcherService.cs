using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DcaShop.Infrastructure.Events;

/// <summary>Per-consumer delivery with retained retry due times, bounded attempts and intentional failed-work replay.</summary>
public sealed class IntegrationEventDispatcherService : BackgroundService
{
    private readonly IIntegrationEventOutbox _outbox;
    private readonly IServiceScopeFactory _scopes;
    private readonly IntegrationEventRetryPolicy _retry;
    private readonly ILogger<IntegrationEventDispatcherService> _logger;

    public IntegrationEventDispatcherService(IIntegrationEventOutbox outbox, IServiceScopeFactory scopes,
        IntegrationEventRetryPolicy retry, ILogger<IntegrationEventDispatcherService> logger)
    { _outbox = outbox; _scopes = scopes; _retry = retry; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var publication in _outbox.ReadDueAsync(stoppingToken).ConfigureAwait(false))
            await DeliverAsync(publication, stoppingToken).ConfigureAwait(false);
    }

    private async Task DeliverAsync(IntegrationEventPublication publication, CancellationToken token)
    {
        using var discovery = _scopes.CreateScope();
        var consumers = discovery.ServiceProvider.GetServices<IEventListener>().Where(l => l.Listens(publication.Event))
            .Select(l => l.GetType().FullName ?? l.GetType().Name).ToArray();
        publication = _outbox.InitializeConsumers(publication.Id, consumers);
        foreach (var progress in publication.Consumers.Values.Where(c => c.Status == PublicationStatus.Pending))
        {
            if (progress.NextAttemptOn > DateTimeOffset.UtcNow) continue;
            try
            {
                using var scope = _scopes.CreateScope();
                var listener = scope.ServiceProvider.GetServices<IEventListener>()
                    .SingleOrDefault(l => (l.GetType().FullName ?? l.GetType().Name) == progress.ConsumerId)
                    ?? throw new InvalidOperationException($"Registered consumer is unavailable: {progress.ConsumerId}");
                await listener.OnAsync(publication.Event, token).ConfigureAwait(false);
                _outbox.MarkConsumerCompleted(publication.Id, progress.ConsumerId);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                var updated = _outbox.RecordConsumerFailure(publication.Id, progress.ConsumerId, error.Message,
                    _retry.MaxAttempts, _retry.DelayBefore(progress.Attempts + 2));
                _logger.LogWarning(error, "Delivery {DeliveryKey} is {Status} after {Attempts} failed attempts",
                    DeliveryIdentity.For(publication.Id, progress.ConsumerId), updated.Consumers[progress.ConsumerId].Status,
                    updated.Consumers[progress.ConsumerId].Attempts);
            }
        }
        _outbox.MarkCompleted(publication.Id);
        // ReadDue applies retained per-consumer due times. A restart does not reset the backoff.
        if (_outbox.All().Single(p => p.Id == publication.Id).Status == PublicationStatus.Pending) _outbox.Requeue(publication.Id);
    }
}
