using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DcaShop.Infrastructure.Events;

/// <summary>
/// Drains the <see cref="IntegrationEventChannel"/>: every integration event is delivered in its own DI scope
/// to the listeners of other contexts — asynchronous and after the publishing use case finished.
/// </summary>
public sealed class IntegrationEventDispatcherService : BackgroundService
{
    private readonly IntegrationEventChannel _channel;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<IntegrationEventDispatcherService> _logger;

    public IntegrationEventDispatcherService(IntegrationEventChannel channel, IServiceScopeFactory scopes, ILogger<IntegrationEventDispatcherService> logger)
    {
        _channel = channel;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IEventDispatcher>().DispatchAsync(@event, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "Failed to deliver integration event {EventType}", @event.GetType().Name);
            }
        }
    }
}
