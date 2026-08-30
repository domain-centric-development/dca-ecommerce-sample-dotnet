using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Implements <see cref="ITransactionBoundary"/> for the in-memory stage — infrastructure, not an adapter, because
/// the boundary is an execution abstraction of the application layer rather than an output port: there is nothing to roll back, but the
/// boundary is real — nested calls join the outer transaction (an inner failure marks it rollback-only, as
/// Spring's REQUIRED propagation does, so the outermost block cannot commit it), after-commit hooks (waking the outbox dispatcher)
/// run once the outermost work completed, after-rollback hooks (discarding in-memory outbox entries) run when it
/// throws. A database adapter replaces this class with one that opens the transaction (EF Core
/// <c>DbContext</c>, <c>TransactionScope</c>) and keeps the same hooks.
/// </summary>
public sealed class InMemoryTransactionBoundary : ITransactionBoundary, ITransactionHooks
{
    private readonly List<Action> _afterCommit = new();
    private readonly List<Action> _afterRollback = new();
    private int _depth;
    private bool _rollbackOnly;

    public bool InTransaction => _depth > 0;

    public void AfterCommit(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!InTransaction)
        {
            action();
            return;
        }

        _afterCommit.Add(action);
    }

    public void AfterRollback(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (InTransaction)
        {
            _afterRollback.Add(action);
        }
    }

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        _depth++;
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            if (_depth == 1)
            {
                if (_rollbackOnly)
                {
                    Rollback();
                    throw new TransactionRolledBackException();
                }

                Commit();
            }

            return result;
        }
        catch (TransactionRolledBackException)
        {
            throw;
        }
        catch
        {
            _rollbackOnly = true;   // an inner failure poisons the shared transaction
            if (_depth == 1)
            {
                Rollback();
            }

            throw;
        }
        finally
        {
            _depth--;
        }
    }

    private void Commit()
    {
        _afterRollback.Clear();
        Run(_afterCommit);
    }

    private void Rollback()
    {
        _rollbackOnly = false;
        _afterCommit.Clear();
        Run(_afterRollback);
    }

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
