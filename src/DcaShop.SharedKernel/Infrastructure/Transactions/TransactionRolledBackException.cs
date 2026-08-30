namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Thrown by the outermost <c>InTransactionAsync</c> when an inner block failed and the caller swallowed that
/// failure: the shared transaction is rollback-only and cannot commit (Spring's <c>UnexpectedRollbackException</c>).
/// </summary>
public sealed class TransactionRolledBackException : InvalidOperationException
{
    public TransactionRolledBackException()
        : base("Transaction was marked rollback-only by a nested block and cannot commit.")
    {
    }
}
