namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Lets infrastructure enlist work that must become visible only when the current unit of work commits —
/// the in-process stand-in for "written in the same transaction as the aggregate".
/// </summary>
public interface ITransactionHooks
{
    /// <summary>True while a unit of work is running on this scope.</summary>
    bool InTransaction { get; }

    /// <summary>Runs <paramref name="action"/> after the outermost unit of work committed; dropped on rollback.</summary>
    void AfterCommit(Action action);
}
