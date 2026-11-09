using DcaShop.Infrastructure.Persistence;

using Npgsql;

using Testcontainers.PostgreSql;

namespace DcaShop.TestSupport;

/// <summary>
/// The PostgreSQL the tests run against: one container per test run, and a database of its own for every shop a
/// test starts — copied from a template that is migrated once, so a fresh database costs a file copy, not a
/// migration. xUnit runs test classes in parallel; no two of them share data.
/// </summary>
public static class TestDatabase
{
    public const string Image = "postgres:17-alpine";

    private const string Template = "shop_template";

    private static readonly Lazy<PostgreSqlContainer> Container = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    // CREATE DATABASE refuses to copy a template another session is copying at the same moment
    private static readonly SemaphoreSlim Copying = new(1, 1);

    private static int _created;

    /// <summary>Creates an empty, migrated database and answers its connection string.</summary>
    public static string Create()
    {
        var container = Container.Value;
        var name = $"shop_{Interlocked.Increment(ref _created)}";
        Copying.Wait();
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString(container, "postgres", pooled: false));
            connection.Open();
            using var command = new NpgsqlCommand($"CREATE DATABASE {name} TEMPLATE {Template}", connection);
            command.ExecuteNonQuery();
        }
        finally
        {
            Copying.Release();
        }

        return ConnectionString(container, name, pooled: true);
    }

    private static PostgreSqlContainer Start()
    {
        var container = new PostgreSqlBuilder(Image)
            .WithCommand("-c", "max_connections=500", "-c", "fsync=off", "-c", "full_page_writes=off")
            .Build();
        container.StartAsync().GetAwaiter().GetResult();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => container.DisposeAsync().AsTask().GetAwaiter().GetResult();
        DatabaseMigrations.Apply(ConnectionString(container, Template, pooled: false));
        return container;
    }

    private static string ConnectionString(PostgreSqlContainer container, string database, bool pooled) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = database,
            Pooling = pooled,
            MaxPoolSize = 10,
        }.ConnectionString;
}