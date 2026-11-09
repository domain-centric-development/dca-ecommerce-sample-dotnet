using Dapper;

using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Persistence;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using Npgsql;

namespace DcaShop.IntegrationTests;

/// <summary>A database of its own with the transaction boundary and the outbox on it, without a shop around them — for the
/// adapter tests.</summary>
internal sealed class IsolatedDatabase : IDisposable
{
    private readonly NpgsqlDataSource _source = NpgsqlDataSource.Create(TestDatabase.Create());

    public IsolatedDatabase()
    {
        Database = new Database(_source);
        Boundary = new PostgresTransactionBoundary(Database);
        Outbox = new PostgresEventOutbox(Database, Boundary, TimeProvider.System);
    }

    public Database Database { get; }

    public PostgresTransactionBoundary Boundary { get; }

    public PostgresEventOutbox Outbox { get; }

    /// <summary>How many publications a reader outside every transaction sees.</summary>
    public int Committed()
    {
        using var connection = _source.OpenConnection();
        return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM event_publications");
    }

    /// <summary>A connection of its own: a reader or a writer outside every transaction of the code under test.</summary>
    public NpgsqlConnection Connect() => _source.OpenConnection();

    public void Dispose() => _source.Dispose();
}