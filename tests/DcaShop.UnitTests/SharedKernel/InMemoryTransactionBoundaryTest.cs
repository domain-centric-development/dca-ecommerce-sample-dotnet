using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using OneOf;

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

    [Fact]
    public async Task AReturnedRefusalRollsBackTheWriteBeforeItAndIsReturnedUnchanged()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary boundary = impl;
        var written = false;
        var rolledBack = false;
        var refusal = new Refused("no");

        var result = await boundary.InTransactionAsync<OneOf<Done, Refused>>(_ =>
        {
            impl.EnlistCommit(() => written = true);
            impl.AfterRollback(() => rolledBack = true);
            return Task.FromResult<OneOf<Done, Refused>>(refusal);
        });

        Assert.Same(refusal, result.AsT1);
        Assert.False(written);
        Assert.True(rolledBack);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task ARefusalReturnedDirectlyRollsBackToo()
    {
        var impl = new InMemoryTransactionBoundary();
        var written = false;

        var result = await impl.InTransactionAsync<object>(_ =>
        {
            impl.EnlistCommit(() => written = true);
            return Task.FromResult<object>(new Refused("no"));
        });

        Assert.IsType<Refused>(result);
        Assert.False(written);
    }

    [Fact]
    public async Task AReturnedSuccessCommits()
    {
        var impl = new InMemoryTransactionBoundary();
        var written = false;

        var result = await impl.InTransactionAsync<OneOf<Done, Refused>>(_ =>
        {
            impl.EnlistCommit(() => written = true);
            return Task.FromResult<OneOf<Done, Refused>>(new Done());
        });

        Assert.True(result.IsT0);
        Assert.True(written);
    }

    [Fact]
    public async Task AThrownFailureStillRollsBack()
    {
        var impl = new InMemoryTransactionBoundary();
        var written = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => impl.InTransactionAsync<OneOf<Done, Refused>>(_ =>
        {
            impl.EnlistCommit(() => written = true);
            throw new InvalidOperationException("boom");
        }));

        Assert.False(written);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task AnInnerRefusalThenAnOuterSuccessThrowsAndRollsBack()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary boundary = impl;
        var written = false;

        await Assert.ThrowsAsync<TransactionRolledBackException>(() => boundary.InTransactionAsync<OneOf<Done, Refused>>(async ct =>
        {
            impl.EnlistCommit(() => written = true);
            await boundary.InTransactionAsync<OneOf<Done, Refused>>(_ => Task.FromResult<OneOf<Done, Refused>>(new Refused("inner")), ct);
            return new Done();
        }));

        Assert.False(written);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task AnOuterRefusalAfterACaughtInnerFailureRollsBackWithoutThrowing()
    {
        var impl = new InMemoryTransactionBoundary();
        ITransactionBoundary boundary = impl;
        var written = false;

        var result = await boundary.InTransactionAsync<OneOf<Done, Refused>>(async ct =>
        {
            impl.EnlistCommit(() => written = true);
            try
            {
                await boundary.InTransactionAsync(_ => throw new InvalidOperationException("inner"), ct);
            }
            catch (InvalidOperationException)
            {
                // answered by the outer block with a refusal
            }

            return new Refused("outer");
        });

        Assert.True(result.IsT1);
        Assert.False(written);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task ATransactionAfterARefusalStartsClean()
    {
        var impl = new InMemoryTransactionBoundary();
        var written = false;

        await impl.InTransactionAsync<OneOf<Done, Refused>>(_ => Task.FromResult<OneOf<Done, Refused>>(new Refused("first")));
        await impl.InTransactionAsync<OneOf<Done, Refused>>(_ =>
        {
            impl.EnlistCommit(() => written = true);
            return Task.FromResult<OneOf<Done, Refused>>(new Done());
        });

        Assert.True(written);
    }

    private sealed record Done;

    private sealed record Refused(string Reason) : IRefusal;
}