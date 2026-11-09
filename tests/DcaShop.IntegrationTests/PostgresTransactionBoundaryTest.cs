using Dapper;

using DcaShop.SharedKernel.Infrastructure.Persistence;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Npgsql;

using OneOf;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The transaction boundary against PostgreSQL: what a block writes commits with the outermost block or not at all,
/// and the hooks follow the outcome.
/// </summary>
public sealed class PostgresTransactionBoundaryTest : IDisposable
{
    private readonly NpgsqlDataSource _source = NpgsqlDataSource.Create(TestDatabase.Create());
    private readonly Database _database;
    private readonly PostgresTransactionBoundary _boundary;

    public PostgresTransactionBoundaryTest()
    {
        _database = new Database(_source);
        _boundary = new PostgresTransactionBoundary(_database);
    }

    public void Dispose() => _source.Dispose();

    /// <summary>Writes a row in the current transaction (or on its own) and answers its key.</summary>
    private Task<Guid> WriteAsync()
    {
        var key = Guid.NewGuid();
        return _database.RunAsync(async (connection, transaction) =>
        {
            await connection.ExecuteAsync(
                "INSERT INTO pricing_processed_events (effect, event_id) VALUES ('probe', @Key)", new { Key = key }, transaction);
            return key;
        });
    }

    /// <summary>Whether a row is there for a reader outside every transaction.</summary>
    private async Task<bool> WrittenAsync(Guid key)
    {
        await using var connection = await _source.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM pricing_processed_events WHERE event_id = @Key)", new { Key = key });
    }
    [Fact]
    public async Task AfterCommitHooksRunOnceTheOutermostWorkCompleted()
    {
        var impl = _boundary;
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
        var uow = _boundary;
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
        var impl = _boundary;
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
        var impl = _boundary;
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
        var uow = _boundary;
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
        var uow = _boundary;
        var ran = false;

        uow.AfterCommit(() => ran = true);

        Assert.True(ran);
    }

    [Fact]
    public void OutsideATransactionBoundaryRollbackHooksAreDiscarded()
    {
        var uow = _boundary;
        var ran = false;

        uow.AfterRollback(() => ran = true);

        Assert.False(ran);
    }

    [Fact]
    public async Task AReturnedRefusalRollsBackTheWriteBeforeItAndIsReturnedUnchanged()
    {
        var impl = _boundary;
        ITransactionBoundary boundary = impl;
        var key = Guid.Empty;
        var rolledBack = false;
        var refusal = new Refused("no");

        var result = await boundary.InTransactionAsync<OneOf<Done, Refused>>(async _ =>
        {
            key = await WriteAsync();
            impl.AfterRollback(() => rolledBack = true);
            return refusal;
        });

        Assert.Same(refusal, result.AsT1);
        Assert.False(await WrittenAsync(key));
        Assert.True(rolledBack);
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task ARefusalReturnedDirectlyRollsBackToo()
    {
        var impl = _boundary;
        var key = Guid.Empty;

        var result = await impl.InTransactionAsync<object>(async _ =>
        {
            key = await WriteAsync();
            return new Refused("no");
        });

        Assert.IsType<Refused>(result);
        Assert.False(await WrittenAsync(key));
    }

    [Fact]
    public async Task AReturnedSuccessCommits()
    {
        var impl = _boundary;
        var key = Guid.Empty;

        var result = await impl.InTransactionAsync<OneOf<Done, Refused>>(async _ =>
        {
            key = await WriteAsync();
            return new Done();
        });

        Assert.True(result.IsT0);
        Assert.True(await WrittenAsync(key));
    }

    [Fact]
    public async Task AThrownFailureStillRollsBack()
    {
        var impl = _boundary;
        var key = Guid.Empty;

        await Assert.ThrowsAsync<InvalidOperationException>(() => impl.InTransactionAsync<OneOf<Done, Refused>>(async _ =>
        {
            key = await WriteAsync();
            throw new InvalidOperationException("boom");
        }));

        Assert.False(await WrittenAsync(key));
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task AnInnerRefusalThenAnOuterSuccessThrowsAndRollsBack()
    {
        var impl = _boundary;
        ITransactionBoundary boundary = impl;
        var key = Guid.Empty;

        await Assert.ThrowsAsync<TransactionRolledBackException>(() => boundary.InTransactionAsync<OneOf<Done, Refused>>(async ct =>
        {
            key = await WriteAsync();
            await boundary.InTransactionAsync<OneOf<Done, Refused>>(_ => Task.FromResult<OneOf<Done, Refused>>(new Refused("inner")), ct);
            return new Done();
        }));

        Assert.False(await WrittenAsync(key));
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task AnOuterRefusalAfterACaughtInnerFailureRollsBackWithoutThrowing()
    {
        var impl = _boundary;
        ITransactionBoundary boundary = impl;
        var key = Guid.Empty;

        var result = await boundary.InTransactionAsync<OneOf<Done, Refused>>(async ct =>
        {
            key = await WriteAsync();
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
        Assert.False(await WrittenAsync(key));
        Assert.False(impl.InTransaction);
    }

    [Fact]
    public async Task ATransactionAfterARefusalStartsClean()
    {
        var impl = _boundary;
        var key = Guid.Empty;

        await impl.InTransactionAsync<OneOf<Done, Refused>>(_ => Task.FromResult<OneOf<Done, Refused>>(new Refused("first")));
        await impl.InTransactionAsync<OneOf<Done, Refused>>(async _ =>
        {
            key = await WriteAsync();
            return new Done();
        });

        Assert.True(await WrittenAsync(key));
    }

    private sealed record Done;

    private sealed record Refused(string Reason) : IRefusal;
}