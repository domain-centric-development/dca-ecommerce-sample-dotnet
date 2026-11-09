namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Lets infrastructure react to the outcome of the current transaction. Work that belongs to the transaction
/// (saving an aggregate, recording an outbox publication) runs inside it; these hooks only carry what must
/// happen <em>because</em> it committed (wake the dispatcher) or rolled back.
/// </summary>
public interface ITransactionHooks
{
    /// <summary>True while a transaction is running in the current flow.</summary>
    bool InTransaction { get; }

    /// <summary>Runs <paramref name="action"/> after the outermost transaction committed; dropped on rollback.
    /// Outside a transaction the action runs immediately.</summary>
    void AfterCommit(Action action);

    /// <summary>Runs <paramref name="action"/> after the outermost transaction rolled back; dropped on commit.
    /// Outside a transaction the action is discarded.</summary>
    void AfterRollback(Action action);
}