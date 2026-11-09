using Npgsql;

namespace DcaShop.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The shop's PostgreSQL database as the persistence adapters use it: a statement runs on the connection of the
/// transaction the current flow is in, or on a connection of its own when there is none.
/// </summary>
/// <remarks>
/// The transaction travels with the asynchronous flow (an <see cref="AsyncLocal{T}"/>), the way a thread-bound
/// transaction travels in Spring: a repository is a singleton and still writes inside the use case's transaction,
/// and the outbox records a publication in the same transaction as the aggregate it reports on. Only
/// <see cref="Transactions.PostgresTransactionBoundary"/> opens and closes one. Work started inside a transaction and
/// left running — a <c>Task.Run</c>, a timer — inherits it like any other continuation, so it is started after the
/// commit, from an after-commit hook, which runs outside the finished transaction.
/// </remarks>
public sealed class Database
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly AsyncLocal<DatabaseTransaction?> _current = new();

    public Database(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    /// <summary>The transaction the current flow runs in, or <see langword="null"/>.</summary>
    internal DatabaseTransaction? Current => _current.Value;

    /// <summary>True while the current flow runs in a transaction — where a read that precedes a write takes a row lock.</summary>
    public bool InTransaction => _current.Value is not null;

    /// <summary>Runs <paramref name="work"/> in the current transaction, or on a connection of its own.</summary>
    public async Task<T> RunAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction?, Task<T>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (_current.Value is { } transaction)
        {
            return await work(transaction.Connection, transaction.Transaction).ConfigureAwait(false);
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await work(connection, null).ConfigureAwait(false);
    }

    /// <summary>Runs <paramref name="work"/> in the current transaction, or on a connection of its own.</summary>
    public Task RunAsync(Func<NpgsqlConnection, NpgsqlTransaction?, Task> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        return RunAsync(async (connection, transaction) =>
        {
            await work(connection, transaction).ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    /// <summary>Opens a connection of its own — for the transaction boundary, which begins its transactions on it.</summary>
    internal ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken) =>
        _dataSource.OpenConnectionAsync(cancellationToken);

    /// <summary>
    /// Makes <paramref name="transaction"/> current for the calling flow. A synchronous method on purpose: the value
    /// it sets stays with the caller, where an asynchronous one would restore it on return.
    /// </summary>
    internal void Enter(DatabaseTransaction? transaction) => _current.Value = transaction;
}

/// <summary>
/// One open transaction with its connection, and what is to happen when it ends. Only the transaction boundary
/// begins, commits and closes it.
/// </summary>
internal sealed class DatabaseTransaction
{
    public DatabaseTransaction(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public NpgsqlConnection Connection { get; }

    public NpgsqlTransaction Transaction { get; }

    /// <summary>An inner block failed or refused: the transaction may only roll back.</summary>
    public bool RollbackOnly { get; set; }

    public List<Action> AfterCommit { get; } = [];

    public List<Action> AfterRollback { get; } = [];
}