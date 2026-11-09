using Dapper;

using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

using Npgsql;

namespace DcaShop.Product.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="IProductRepository"/>. A SKU names one product: the unique
/// constraint <c>uq_products_sku</c> holds the claim against a race between the use case's check and its save, and
/// this adapter translates that one refusal; listings sort by product name, ordinally, as the contract requires.
/// </summary>
public sealed class DapperProductRepository : IProductRepository
{
    private const string SkuConstraint = "uq_products_sku";

    private const string SelectProduct =
        """
        SELECT id AS Id, sku AS Sku, name AS Name, description AS Description, category AS Category, image_url AS ImageUrl
        FROM products
        """;

    private readonly Database _database;

    public DapperProductRepository(Database database) => _database = database;

    public async Task<Domain.Model.Product?> FindByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectProduct} WHERE id = @Id", new { Id = id.Value.ToString() }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public async Task<Domain.Model.Product?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        (await QueryAsync($"{SelectProduct} WHERE sku = @Value", new { sku.Value }, cancellationToken)
            .ConfigureAwait(false)).SingleOrDefault();

    public Task<IReadOnlyList<Domain.Model.Product>> FindAllAsync(CancellationToken cancellationToken = default) =>
        QueryAsync($"{SelectProduct} ORDER BY name COLLATE \"C\"", null, cancellationToken);

    /// <exception cref="InvalidOperationException">Another product already carries this SKU.</exception>
    public async Task<Domain.Model.Product> SaveAsync(Domain.Model.Product aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        try
        {
            await _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO products (id, sku, name, description, category, image_url)
                VALUES (@Id, @Sku, @Name, @Description, @Category, @ImageUrl)
                ON CONFLICT (id) DO UPDATE SET
                  sku = EXCLUDED.sku, name = EXCLUDED.name, description = EXCLUDED.description,
                  category = EXCLUDED.category, image_url = EXCLUDED.image_url
                """,
                new
                {
                    Id = aggregate.Id.Value.ToString(),
                    Sku = aggregate.Sku.Value,
                    Name = aggregate.Name.Value,
                    Description = aggregate.Description.Value,
                    Category = aggregate.Category.Name,
                    ImageUrl = aggregate.ImageUrl.Value,
                },
                transaction, cancellationToken: cancellationToken)), cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException violation) when (violation is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: SkuConstraint })
        {
            throw new InvalidOperationException($"SKU {aggregate.Sku} is already taken", violation);
        }

        return aggregate;
    }

    public Task DeleteByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM products WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<IReadOnlyList<Domain.Model.Product>> QueryAsync(string sql, object? parameters, CancellationToken cancellationToken) =>
        _database.RunAsync<IReadOnlyList<Domain.Model.Product>>(async (connection, transaction) =>
            (await connection.QueryAsync<ProductRow>(new CommandDefinition(sql, parameters, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(row => Domain.Model.Product.Reconstitute(
                ProductId.Of(row.Id), Sku.Of(row.Sku), ProductName.Of(row.Name), ProductDescription.Of(row.Description),
                Category.Of(row.Category), ImageUrl.Of(row.ImageUrl)))
            .ToList(), cancellationToken);

    private sealed class ProductRow
    {
        public string Id { get; init; } = "";
        public string Sku { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Category { get; init; } = "";
        public string ImageUrl { get; init; } = "";
    }
}