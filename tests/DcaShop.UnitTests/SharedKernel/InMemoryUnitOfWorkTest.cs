using DcaShop.SharedKernel.Adapter.Outgoing.Transaction;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.UnitTests.SharedKernel;

public sealed class InMemoryUnitOfWorkTest
{
    [Fact]
    public async Task AfterCommitHooksRunOnceTheOutermostWorkCompleted()
    {
        var impl = new InMemoryUnitOfWork();
        IUnitOfWork uow = impl;
        var log = new List<string>();

        var result = await uow.RunAsync(async ct =>
        {
            impl.AfterCommit(() => log.Add("outer hook"));
            await uow.RunAsync(_ =>
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
        var uow = new InMemoryUnitOfWork();
        var ran = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RunAsync<int>(_ =>
        {
            uow.AfterCommit(() => ran = true);
            throw new InvalidOperationException("boom");
        }));

        Assert.False(ran);
        Assert.False(uow.InTransaction);
    }

    [Fact]
    public void OutsideAUnitOfWorkHooksRunImmediately()
    {
        var uow = new InMemoryUnitOfWork();
        var ran = false;

        uow.AfterCommit(() => ran = true);

        Assert.True(ran);
    }
}
