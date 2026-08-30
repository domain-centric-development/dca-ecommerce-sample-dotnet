using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DcaShop.Infrastructure.Events;

/// <summary>
/// Delivers outbox publications to their listeners, each in its own DI scope. A failing delivery is recorded and
/// retried with exponential backoff; after <see cref="IntegrationEventRetryPolicy.MaxAttempts"/> the publication
/// is marked failed and stays inspectable. Consumers must therefore be idempotent (key: <c>EventId</c>).
/// </summary>
public sealed class IntegrationEventDispatcherService : BackgroundService
{
    private readonly IIntegrationEventOutbox _outbox;
    private readonly IServiceScopeFactory _scopes;
    private readonly IntegrationEventRetryPolicy _retry;
    private readonly ILogger<IntegrationEventDispatcherService> _logger;

    public IntegrationEventDispatcherService(
        IIntegrationEventOutbox outbox,
        IServiceScopeFactory scopes,
        IntegrationEventRetryPolicy retry,
        ILogger<IntegrationEventDispatcherService> logger)
    {
        _outbox = outbox;
        _scopes = scopes;
        _retry = retry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var publication in _outbox.ReadDueAsync(stoppingToken).ConfigureAwait(false))
        {
            await DeliverAsync(publication, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task DeliverAsync(IntegrationEventPublication publication, CancellationToken stoppingToken)
    {
        var eventType = publication.Event.GetType().Name;
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IEventDispatcher>().DispatchAsync(publication.Event, stoppingToken).ConfigureAwait(false);
            _outbox.MarkCompleted(publication.Id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var failed = _outbox.RecordFailure(publication.Id, e.Message);
            if (failed.Attempts >= _retry.MaxAttempts)
            {
                _outbox.MarkFailed(publication.Id);
                _logger.LogError(e, "Giving up on integration event {EventType} {EventId} after {Attempts} attempts", eventType, publication.Id, failed.Attempts);
                return;
            }

            var delay = _retry.DelayBefore(failed.Attempts + 1);
            _logger.LogWarning(e, "Delivery of integration event {EventType} {EventId} failed (attempt {Attempt}/{Max}); retrying in {Delay}", eventType, publication.Id, failed.Attempts, _retry.MaxAttempts, delay);
            _ = RequeueAfterAsync(publication.Id, delay, stoppingToken);
        }
    }

    private async Task RequeueAfterAsync(Guid publicationId, TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            _outbox.Requeue(publicationId);
        }
        catch (OperationCanceledException)
        {
            // shutting down; the publication stays pending and is replayed on the next start
        }
    }
}
