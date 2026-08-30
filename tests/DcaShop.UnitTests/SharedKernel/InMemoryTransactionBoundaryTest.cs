using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.UnitTests.SharedKernel;

public sealed class InMemoryTransactionBoundaryTest
{
    [Fact]
    public async Task AfterCommitHooksRunOnceTheOutermostWorkCompleted()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary uow = impl;
        var log = new List<string>();

        var result = await uow.InTransactionAsync(async ct =>
        {
            impl.AfterCommit(() => log.Add("outer hook"));
            await uow.InTransactionAsync(_ =>
            {
                impl.AfterCommit(() => log.Add("inner hook"));
                log.Add("inner work");
                return Task.CompletedTask;
            }, ct);
            log.Add("outer work");
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal(["inner work", "outer work", "outer hook", "inner hook"], log);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task RollbackDropsCommitHooksAndRunsRollbackHooks()
    {
        var uow = new InMemoryTransactionBoundary();
        var committed = false;
        var rolledBack = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.InTransactionAsync<int>(_ =>
        {
            uow.AfterCommit(() => committed = true);
            uow.AfterRollback(() => rolledBack = true);
            throw new InvalidOperationException("boom");
        }));

        Assert.False(committed);
        Assert.True(rolledBack);
        Assert.False(uow.InTransaction);
    }

    [Fact]
    public async Task SwallowedInnerFailureMarksTheTransactionRollbackOnly()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary boundary = impl;
        var committed = false;
        var rolledBack = false;

        await Assert.ThrowsAsync<TransactionRolledBackException>(() => boundary.InTransactionAsync(async ct =>
        {
            impl.AfterCommit(() => committed = true);
            impl.AfterRollback(() => rolledBack = true);
            try
            {
                await boundary.InTransactionAsync(_ => throw new InvalidOperationException("inner"), ct);
            }
            catch (InvalidOperationException)
            {
                // handled by the outer block — the transaction is still poisoned
            }
        }));

        Assert.False(committed);
        Assert.True(rolledBack);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task RollbackOnlyDoesNotLeakIntoTheNextTransaction()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary boundary = impl;

        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(_ => throw new InvalidOperationException("boom")));
        var committed = false;
        await boundary.InTransactionAsync(_ =>
        {
            impl.AfterCommit(() => committed = true);
            return Task.CompletedTask;
        });

        Assert.True(committed);
    }

    [Fact]
    public async Task CommitDropsRollbackHooks()
    {
        var uow = new InMemoryTransactionBoundary();
        var rolledBack = false;

        await ((ITransactionBoundary)uow).InTransactionAsync(_ =>
        {
            uow.AfterRollback(() => rolledBack = true);
            return Task.CompletedTask;
        });

        Assert.False(rolledBack);
    }

    [Fact]
    public void OutsideATransactionBoundaryHooksRunImmediately()
    {
        var uow = new InMemoryTransactionBoundary();
        var ran = false;

        uow.AfterCommit(() => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public void OutsideATransactionBoundaryRollbackHooksAreDiscarded()
    {
        var uow = new InMemoryTransactionBoundary();
        var ran = false;

        uow.AfterRollback(() => ran = true);

        Assert.False(ran);
    }
}
