using Dapper;

using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

using Npgsql;

namespace DcaShop.Cart.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="IShoppingCartRepository"/>: a cart row and its lines, the lines
/// replaced wholesale on save in the same transaction and handed back to <see cref="ShoppingCart.Reconstitute"/> with
/// their unit allocation (<c>position_units</c>).
/// </summary>
/// <remarks>
/// "At most one active cart per customer" is uniqueness over a subset of the rows: the partial unique index
/// <c>uq_carts_active_customer</c> holds it. When it refuses a row, this adapter answers
/// <see cref="ActiveCartAlreadyExistsException"/> — the failure the port declares — and only for that index: any
/// other constraint keeps travelling as what it is.
/// </remarks>
public sealed class DapperShoppingCartRepository : IShoppingCartRepository
{
    private const string ActiveCartIndex = "uq_carts_active_customer";

    private const string SelectCart = "SELECT id AS Id, customer_id AS CustomerId, status AS Status FROM carts";

    private readonly Database _database;

    public DapperShoppingCartRepository(Database database) => _database = database;

    public async Task<ShoppingCart?> FindByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectCart} WHERE id = @Id", new { Id = id.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public async Task<ShoppingCart?> FindByIdForCustomerAsync(CartId id, CustomerId customerId, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectCart} WHERE id = @Id AND customer_id = @CustomerId",
            new { Id = id.Value.ToString(), CustomerId = customerId.Value }, cancellationToken).ConfigureAwait(false)).SingleOrDefault();

    public async Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectCart} WHERE customer_id = @CustomerId AND status = 'ACTIVE'",
            new { CustomerId = customerId.Value }, cancellationToken).ConfigureAwait(false)).SingleOrDefault();

    public Task<IReadOnlyList<ShoppingCart>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectCart} WHERE customer_id = @CustomerId ORDER BY updated_at", new { CustomerId = customerId.Value }, cancellationToken);

    public Task<IReadOnlyList<ShoppingCart>> FindAllAsync(CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectCart} ORDER BY updated_at", null, cancellationToken);

    /// <exception cref="ActiveCartAlreadyExistsException">
    /// The cart is active and the customer already has a different active cart.
    /// </exception>
    public async Task<ShoppingCart> SaveAsync(ShoppingCart aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var id = aggregate.Id.Value.ToString();
        await _database.RunAsync(async (connection, transaction) =>
        {
            try
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO carts (id, customer_id, status, updated_at) VALUES (@Id, @CustomerId, @Status, CURRENT_TIMESTAMP)
                    ON CONFLICT (id) DO UPDATE SET
                      customer_id = EXCLUDED.customer_id, status = EXCLUDED.status, updated_at = EXCLUDED.updated_at
                    """,
                    new { Id = id, CustomerId = aggregate.CustomerId.Value, Status = ColumnOf(aggregate.Status) },
                    transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }
            catch (PostgresException violation) when (violation is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: ActiveCartIndex })
            {
                throw new ActiveCartAlreadyExistsException(aggregate.CustomerId);
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM cart_items WHERE cart_id = @Id", new { Id = id }, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO cart_items (id, cart_id, product_id, quantity, price_amount, price_currency, position_units)
                VALUES (@Id, @CartId, @ProductId, @Quantity, @Amount, @Currency, @Units)
                """,
                aggregate.Items.Select(item => new
                {
                    Id = item.Id.Value.ToString(),
                    CartId = id,
                    ProductId = item.ProductId.Value.ToString(),
                    Quantity = item.Quantity.Value,
                    item.PriceAtAddition.Value.Amount,
                    Currency = item.PriceAtAddition.Value.Currency.Code,
                    Units = item.StoredUnits,
                }),
                transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return aggregate;
    }

    public Task DeleteByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM carts WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<IReadOnlyList<ShoppingCart>> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken) =>
        _database.RunAsync<IReadOnlyList<ShoppingCart>>(async (connection, transaction) =>
        {
            var carts = (await connection.QueryAsync<CartRow>(new CommandDefinition(sql, parameters, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false)).ToList();
            if (carts.Count == 0) return [];

            var items = (await connection.QueryAsync<ItemRow>(new CommandDefinition(
                    """
                    SELECT id AS Id, cart_id AS CartId, product_id AS ProductId, quantity AS Quantity,
                           price_amount AS Amount, price_currency AS Currency, position_units AS Units
                    FROM cart_items WHERE cart_id = ANY(@CartIds)
                    """,
                    new { CartIds = carts.Select(c => c.Id).ToArray() }, transaction, cancellationToken: cancellationToken))
                .ConfigureAwait(false)).ToLookup(item => item.CartId);

            return carts.Select(cart => ShoppingCart.Reconstitute(
                    CartId.Of(cart.Id), CustomerId.Of(cart.CustomerId), StatusOf(cart.Status),
                    items[cart.Id].Select(item => new ShoppingCart.StoredItem(
                        CartItemId.Of(item.Id), ProductId.Of(item.ProductId), Quantity.Of(item.Quantity),
                        Price.Of(Money.Of(item.Amount, Currency.Of(item.Currency))), item.Units))))
                .ToList();
        }, cancellationToken);

    /// <summary>The stored spelling of a status, written out per value as every stored cart carries it.</summary>
    private static string ColumnOf(CartStatus status) => status switch
    {
        CartStatus.Active => "ACTIVE",
        CartStatus.Completed => "COMPLETED",
        CartStatus.Abandoned => "ABANDONED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static CartStatus StatusOf(string column) => column switch
    {
        "ACTIVE" => CartStatus.Active,
        "COMPLETED" => CartStatus.Completed,
        "ABANDONED" => CartStatus.Abandoned,
        _ => throw new InvalidOperationException($"Unknown stored cart status: {column}"),
    };

    private sealed class CartRow
    {
        public string Id { get; init; } = "";
        public string CustomerId { get; init; } = "";
        public string Status { get; init; } = "";
    }

    private sealed class ItemRow
    {
        public string Id { get; init; } = "";
        public string CartId { get; init; } = "";
        public string ProductId { get; init; } = "";
        public int Quantity { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "";
        public string Units { get; init; } = "";
    }
}