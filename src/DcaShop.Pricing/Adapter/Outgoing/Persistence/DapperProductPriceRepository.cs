using Dapper;

using DcaShop.Pricing.Application.Shared;
using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

namespace DcaShop.Pricing.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="IProductPriceRepository"/>: one row per product
/// (<c>uq_product_prices_product</c>), mapped back through <see cref="ProductPrice.Reconstitute"/>.
/// </summary>
public sealed class DapperProductPriceRepository : IProductPriceRepository
{
    private const string SelectPrice =
        """
        SELECT id AS Id, product_id AS ProductId, amount AS Amount, currency AS Currency, effective_from AS EffectiveFrom
        FROM product_prices
        """;

    private readonly Database _database;

    public DapperProductPriceRepository(Database database) => _database = database;

    public async Task<ProductPrice?> FindByIdAsync(PriceId id, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectPrice} WHERE id = @Id", new { Id = id.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public async Task<ProductPrice?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectPrice} WHERE product_id = @ProductId", new { ProductId = productId.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public Task<IReadOnlyList<ProductPrice>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectPrice} WHERE product_id = ANY(@ProductIds)",
            new { ProductIds = productIds.Select(id => id.Value.ToString()).ToArray() }, cancellationToken);

    public async Task<ProductPrice> SaveAsync(ProductPrice aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        await _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO product_prices (id, product_id, amount, currency, effective_from)
            VALUES (@Id, @ProductId, @Amount, @Currency, @EffectiveFrom)
            ON CONFLICT (id) DO UPDATE SET
              product_id = EXCLUDED.product_id, amount = EXCLUDED.amount, currency = EXCLUDED.currency,
              effective_from = EXCLUDED.effective_from
            """,
            new
            {
                Id = aggregate.Id.Value.ToString(),
                ProductId = aggregate.ProductId.Value.ToString(),
                aggregate.CurrentPrice.Value.Amount,
                Currency = aggregate.CurrentPrice.Value.Currency.Code,
                EffectiveFrom = StoredTime.Write(aggregate.EffectiveFrom),
            },
            transaction, cancellationToken: cancellationToken)), cancellationToken).ConfigureAwait(false);
        return aggregate;
    }

    public Task DeleteByIdAsync(PriceId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM product_prices WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<IReadOnlyList<ProductPrice>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken) =>
        _database.RunAsync<IReadOnlyList<ProductPrice>>(async (connection, transaction) =>
            (await connection.QueryAsync<PriceRow>(new CommandDefinition(sql, parameters, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(row => ProductPrice.Reconstitute(
                PriceId.Of(row.Id), ProductId.Of(row.ProductId),
                Price.Of(Money.Of(row.Amount, Currency.Of(row.Currency))), StoredTime.Read(row.EffectiveFrom)))
            .ToList(), cancellationToken);

    private sealed class PriceRow
    {
        public string Id { get; init; } = "";
        public string ProductId { get; init; } = "";
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "";
        public DateTime EffectiveFrom { get; init; }
    }
}