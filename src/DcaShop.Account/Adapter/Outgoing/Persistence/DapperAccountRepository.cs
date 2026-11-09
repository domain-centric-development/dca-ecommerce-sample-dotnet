using Dapper;

using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

namespace DcaShop.Account.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="IAccountRepository"/>. Loading maps a row back through
/// <see cref="Domain.Model.Account.Reconstitute"/>, so every load hands out a fresh instance: a change nobody saved is
/// gone. The roles live in a table of their own, by their code, replaced wholesale on save in the same transaction.
/// Email and linked identity are unique columns — the backstop against a race the use cases' own check cannot see.
/// </summary>
public sealed class DapperAccountRepository : IAccountRepository
{
    private const string SelectAccount =
        """
        SELECT id AS Id, email AS Email, first_name AS FirstName, last_name AS LastName, date_of_birth AS DateOfBirth,
               linked_user_id AS LinkedUserId, password_hash AS PasswordHash, status AS Status,
               created_at AS CreatedAt, last_login_at AS LastLoginAt
        FROM accounts
        """;

    private readonly Database _database;

    public DapperAccountRepository(Database database) => _database = database;

    public Task<Domain.Model.Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        FindOneAsync($"{SelectAccount} WHERE id = @Value", new { Value = id.Value.ToString() }, cancellationToken);

    public Task<Domain.Model.Account?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return FindOneAsync($"{SelectAccount} WHERE email = @Value", new { email.Value }, cancellationToken);
    }

    public Task<Domain.Model.Account?> FindByLinkedUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        FindOneAsync($"{SelectAccount} WHERE linked_user_id = @Value", new { userId.Value }, cancellationToken);

    public async Task<Domain.Model.Account> SaveAsync(Domain.Model.Account aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var id = aggregate.Id.Value.ToString();
        await _database.RunAsync(async (connection, transaction) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO accounts (id, email, first_name, last_name, date_of_birth, linked_user_id,
                                      password_hash, status, created_at, last_login_at)
                VALUES (@Id, @Email, @FirstName, @LastName, @DateOfBirth, @LinkedUserId,
                        @PasswordHash, @Status, @CreatedAt, @LastLoginAt)
                ON CONFLICT (id) DO UPDATE SET
                  email = EXCLUDED.email, first_name = EXCLUDED.first_name, last_name = EXCLUDED.last_name,
                  date_of_birth = EXCLUDED.date_of_birth, linked_user_id = EXCLUDED.linked_user_id,
                  password_hash = EXCLUDED.password_hash, status = EXCLUDED.status,
                  created_at = EXCLUDED.created_at, last_login_at = EXCLUDED.last_login_at
                """,
                new
                {
                    Id = id,
                    Email = aggregate.Email.Value,
                    aggregate.Owner.FirstName,
                    aggregate.Owner.LastName,
                    DateOfBirth = aggregate.Owner.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                    LinkedUserId = aggregate.LinkedUserId.Value,
                    PasswordHash = aggregate.Password.Hash,
                    Status = ColumnOf(aggregate.Status),
                    CreatedAt = StoredTime.Write(aggregate.CreatedAt),
                    LastLoginAt = StoredTime.Write(aggregate.LastLoginAt),
                },
                transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);

            // Replaced wholesale rather than diffed: roles are a set, and a set has no update semantics beyond
            // "these are the members now".
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM account_roles WHERE account_id = @Id", new { Id = id }, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO account_roles (account_id, role) VALUES (@Id, @Role)",
                aggregate.Roles.Select(role => new { Id = id, Role = role.Code }), transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return aggregate;
    }

    public Task DeleteByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM accounts WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<Domain.Model.Account?> FindOneAsync(string sql, object parameters, CancellationToken cancellationToken) =>
        _database.RunAsync(async (connection, transaction) =>
        {
            var row = await connection.QuerySingleOrDefaultAsync<AccountRow>(new CommandDefinition(
                sql, parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (row is null) return null;

            var roles = await connection.QueryAsync<string>(new CommandDefinition(
                "SELECT role FROM account_roles WHERE account_id = @Id", new { row.Id }, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            return Domain.Model.Account.Reconstitute(
                AccountId.Of(row.Id),
                Email.Of(row.Email),
                Owner.Of(row.FirstName, row.LastName, DateOnly.FromDateTime(row.DateOfBirth)),
                UserId.Of(row.LinkedUserId),
                HashedPassword.FromHash(row.PasswordHash),
                StatusOf(row.Status),
                roles.Select(code => Role.FromCode(code) ?? throw new InvalidOperationException($"Unknown stored role: {code}")),
                StoredTime.Read(row.CreatedAt),
                StoredTime.Read(row.LastLoginAt));
        }, cancellationToken);

    /// <summary>The stored spelling of a status, written out per value as every stored account carries it.</summary>
    private static string ColumnOf(AccountStatus status) => status switch
    {
        AccountStatus.Active => "ACTIVE",
        AccountStatus.Suspended => "SUSPENDED",
        AccountStatus.Closed => "CLOSED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static AccountStatus StatusOf(string column) => column switch
    {
        "ACTIVE" => AccountStatus.Active,
        "SUSPENDED" => AccountStatus.Suspended,
        "CLOSED" => AccountStatus.Closed,
        _ => throw new InvalidOperationException($"Unknown stored account status: {column}"),
    };

    private sealed class AccountRow
    {
        public string Id { get; init; } = "";
        public string Email { get; init; } = "";
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public DateTime DateOfBirth { get; init; }
        public string LinkedUserId { get; init; } = "";
        public string PasswordHash { get; init; } = "";
        public string Status { get; init; } = "";
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
    }
}