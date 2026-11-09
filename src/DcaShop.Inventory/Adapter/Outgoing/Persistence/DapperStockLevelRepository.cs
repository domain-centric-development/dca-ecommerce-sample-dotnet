using Dapper;

using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

namespace DcaShop.Inventory.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="IStockLevelRepository"/>: one row per product, read back through
/// <see cref="StockLevel.Reconstitute"/>.
/// </summary>
/// <remarks>
/// A stock level read by product inside a transaction is read <c>FOR UPDATE</c>: a reservation, a release or a
/// reduction checks the units and then changes them, and the row lock holds every other one back until this
/// transaction ends — across processes, where the one-at-a-time of
/// <see cref="IStockLevelRepository.InStockLevelsAsync{T}"/> reaches only this one. Several products are locked in
/// product order, so two transactions over overlapping products never deadlock.
/// </remarks>
public sealed class DapperStockLevelRepository : IStockLevelRepository
{
    private const string SelectStock =
        """
        SELECT id AS Id, product_id AS ProductId, available_quantity AS AvailableQuantity, reserved_quantity AS ReservedQuantity
        FROM stock_levels
        """;

    private readonly Database _database;

    public DapperStockLevelRepository(Database database) => _database = database;

    private string Lock => _database.InTransaction ? " FOR UPDATE" : "";

    public async Task<StockLevel?> FindByIdAsync(StockLevelId id, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectStock} WHERE id = @Id", new { Id = id.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public async Task<StockLevel?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectStock} WHERE product_id = @ProductId{Lock}", new { ProductId = productId.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public Task<IReadOnlyList<StockLevel>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectStock} WHERE product_id = ANY(@ProductIds) ORDER BY product_id{Lock}",
            new { ProductIds = productIds.Select(id => id.Value.ToString()).ToArray() }, cancellationToken);

    public Task<IReadOnlyList<StockLevel>> FindAllAsync(CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectStock} ORDER BY product_id", null, cancellationToken);

    public async Task<StockLevel> SaveAsync(StockLevel aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        await _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO stock_levels (id, product_id, available_quantity, reserved_quantity)
            VALUES (@Id, @ProductId, @Available, @Reserved)
            ON CONFLICT (id) DO UPDATE SET
              product_id = EXCLUDED.product_id, available_quantity = EXCLUDED.available_quantity,
              reserved_quantity = EXCLUDED.reserved_quantity
            """,
            new
            {
                Id = aggregate.Id.Value.ToString(),
                ProductId = aggregate.ProductId.Value.ToString(),
                Available = aggregate.AvailableQuantity.Value,
                Reserved = aggregate.ReservedQuantity.Value,
            },
            transaction, cancellationToken: cancellationToken)), cancellationToken).ConfigureAwait(false);
        return aggregate;
    }

    public Task DeleteByIdAsync(StockLevelId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM stock_levels WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<IReadOnlyList<StockLevel>> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken) =>
        _database.RunAsync<IReadOnlyList<StockLevel>>(async (connection, transaction) =>
            (await connection.QueryAsync<StockRow>(new CommandDefinition(sql, parameters, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(row => StockLevel.Reconstitute(
                StockLevelId.Of(row.Id), ProductId.Of(row.ProductId), row.AvailableQuantity, row.ReservedQuantity))
            .ToList(), cancellationToken);

    private sealed class StockRow
    {
        public string Id { get; init; } = "";
        public string ProductId { get; init; } = "";
        public int AvailableQuantity { get; init; }
        public int ReservedQuantity { get; init; }
    }
}