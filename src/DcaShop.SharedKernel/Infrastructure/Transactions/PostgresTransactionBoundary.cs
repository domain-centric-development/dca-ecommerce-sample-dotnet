using DcaShop.SharedKernel.Infrastructure.Persistence;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Application.Transactions;

using OneOf;

namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Implements <see cref="ITransactionBoundary"/> with a PostgreSQL transaction — infrastructure, not an adapter,
/// because the boundary is an execution abstraction of the application layer rather than an output port. The
/// outermost block opens the transaction and makes it current for its flow, so every repository, store and the
/// outbox write in it; nested blocks join it (an inner failure marks it rollback-only, as Spring's REQUIRED
/// propagation does, so the outermost block cannot commit it). After-commit hooks (waking the outbox dispatcher)
/// run once it committed, after-rollback hooks once it rolled back.
/// </summary>
/// <remarks>
/// A returned refusal rolls back like a thrown failure (WP-171): when the work completes with an
/// <see cref="IRefusal"/> — directly, or as the current case of a OneOf result — the outermost block rolls back and
/// returns the result unchanged, without throwing; an inner block that returns one marks the shared transaction
/// rollback-only, so an outermost block that returns a success afterwards throws
/// <see cref="TransactionRolledBackException"/>. "Decide, then write" stays the rule; this is the net.
/// </remarks>
public sealed class PostgresTransactionBoundary : ITransactionBoundary, ITransactionHooks
{
    private readonly Database _database;

    public PostgresTransactionBoundary(Database database) => _database = database;

    public bool InTransaction => _database.Current is not null;

    public void AfterCommit(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_database.Current is { } transaction) transaction.AfterCommit.Add(action);
        else action();
    }

    public void AfterRollback(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _database.Current?.AfterRollback.Add(action);
    }

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (_database.Current is { } joined)
        {
            return await JoinAsync(joined, work, cancellationToken).ConfigureAwait(false);
        }

        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var opened = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var transaction = new DatabaseTransaction(connection, opened);
        T result;
        bool committed;
        _database.Enter(transaction);
        try
        {
            try
            {
                result = await work(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollbackAsync(transaction).ConfigureAwait(false);
                _database.Enter(null);
                Run(transaction.AfterRollback);
                throw;
            }

            committed = !IsRefusal(result) && !transaction.RollbackOnly;
            if (committed)
            {
                try
                {
                    await transaction.Transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    _database.Enter(null);
                    Run(transaction.AfterRollback);
                    throw;
                }
            }
            else
            {
                await RollbackAsync(transaction).ConfigureAwait(false);
            }
        }
        finally
        {
            // The hooks run outside the transaction: whatever they start must not inherit a finished one
            _database.Enter(null);
        }

        if (!committed)
        {
            Run(transaction.AfterRollback);
            return IsRefusal(result) ? result : throw new TransactionRolledBackException();
        }

        Run(transaction.AfterCommit);
        return result;
    }

    private static async Task<T> JoinAsync<T>(
        DatabaseTransaction joined, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            if (IsRefusal(result))
            {
                joined.RollbackOnly = true;   // an inner refusal poisons the shared transaction like an inner failure
            }

            return result;
        }
        catch
        {
            joined.RollbackOnly = true;   // an inner failure poisons the shared transaction
            throw;
        }
    }

    private static Task RollbackAsync(DatabaseTransaction transaction) =>
        transaction.Transaction.RollbackAsync(CancellationToken.None);

    /// <summary>A refusal: the value itself, or the case a OneOf result currently holds.</summary>
    private static bool IsRefusal(object? result) =>
        result is IRefusal || (result is IOneOf union && union.Value is IRefusal);

    private static void Run(List<Action> hooks)
    {
        var pending = hooks.ToArray();
        hooks.Clear();
        foreach (var hook in pending)
        {
            hook();
        }
    }
}