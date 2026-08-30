using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// Implements <see cref="ITransactionBoundary"/> for the in-memory stage — infrastructure, not an adapter, because
/// the boundary is an execution abstraction of the application layer rather than an output port: there is nothing to roll back, but the
/// boundary is real — nested calls join the outer unit of work, after-commit hooks (outbox registrations) run
/// once the outermost work completed and are dropped when it throws. A database adapter replaces this class with
/// one that opens the transaction (EF Core <c>DbContext</c>, <c>TransactionScope</c>) and keeps the same hooks.
/// </summary>
public sealed class InMemoryTransactionBoundary : ITransactionBoundary, ITransactionHooks
{
    private readonly List<Action> _afterCommit = new();
    private int _depth;

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

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        _depth++;
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            if (_depth == 1)
            {
                Commit();
            }

            return result;
        }
        catch
        {
            if (_depth == 1)
            {
                _afterCommit.Clear();   // rollback: nothing enlisted becomes visible
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
        var hooks = _afterCommit.ToArray();
        _afterCommit.Clear();
        foreach (var hook in hooks)
        {
            hook();
        }
    }
}
