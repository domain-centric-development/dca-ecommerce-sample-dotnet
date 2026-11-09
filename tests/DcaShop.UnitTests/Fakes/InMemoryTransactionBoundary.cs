using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using OneOf;

namespace DcaShop.SharedKernel.Infrastructure.Transactions;

/// <summary>
/// The transaction boundary of the unit tests, beside the in-memory fakes: there is nothing to roll back, but the
/// boundary keeps the production one's rules — nested calls join the outer transaction (an inner failure marks it
/// rollback-only, so the outermost block cannot commit it), after-commit hooks run once the outermost work completed,
/// after-rollback hooks when it failed or refused. The fakes store copies, so a rolled-back change that was never
/// saved is not seen; one that was saved stays, which a database would have undone.
/// </summary>
/// <remarks>
/// A returned refusal rolls back like a thrown failure (WP-171): when the work completes with an
/// <see cref="IRefusal"/> — directly, or as the current case of a OneOf result — the outermost block rolls back and
/// returns the result unchanged, without throwing; an inner block that returns one marks the shared transaction
/// rollback-only, so an outermost block that returns a success afterwards throws
/// <see cref="TransactionRolledBackException"/>. "Decide, then write" stays the rule; this is the net.
/// </remarks>
public sealed class InMemoryTransactionBoundary : ITransactionBoundary, ITransactionHooks
{
    private bool _committed;
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
        if (_depth == 0) _committed = false;
        _depth++;
        try
        {
            var result = await work(cancellationToken).ConfigureAwait(false);
            var refused = IsRefusal(result);
            if (_depth > 1 && refused)
            {
                _rollbackOnly = true;   // an inner refusal poisons the shared transaction like an inner failure
            }

            if (_depth == 1)
            {
                if (refused)
                {
                    Rollback();
                    return result;
                }

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
            if (_committed) throw;
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

    /// <summary>A refusal: the value itself, or the case a OneOf result currently holds.</summary>
    private static bool IsRefusal(object? result) =>
        result is IRefusal || (result is IOneOf union && union.Value is IRefusal);

    private void Commit()
    {
        _committed = true;
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