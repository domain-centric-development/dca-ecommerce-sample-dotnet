using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DcaShop.Infrastructure.Persistence;

/// <summary>
/// Migrates the database when the host starts — registered first, so every other hosted service (the outbox
/// dispatcher, the sample data) finds the schema in place.
/// </summary>
public sealed class DatabaseMigration(IConfiguration configuration, ILogger<DatabaseMigration> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        DatabaseMigrations.Apply(ShopDatabase.ConnectionString(configuration), logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}