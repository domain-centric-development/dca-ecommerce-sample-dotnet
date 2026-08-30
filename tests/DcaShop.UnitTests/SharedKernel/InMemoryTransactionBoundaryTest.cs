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
    public async Task RollbackDropsEnlistedHooks()
    {
        var uow = new InMemoryTransactionBoundary();
        var ran = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.InTransactionAsync<int>(_ =>
        {
            uow.AfterCommit(() => ran = true);
            throw new InvalidOperationException("boom");
        }));

        Assert.False(ran);
        Assert.False(uow.InTransaction);
    }

    [Fact]
    public void OutsideATransactionBoundaryHooksRunImmediately()
    {
        var uow = new InMemoryTransactionBoundary();
        var ran = false;

        uow.AfterCommit(() => ran = true);

        Assert.True(ran);
    }
}
