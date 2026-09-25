using DcaShop.Product.Adapter.Outgoing.Persistence;
using DcaShop.Product.Application.GetProductSelection;
using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class GetProductSelectionUseCaseTest
{
    private static readonly ProductFactory Factory = new();

    private readonly InMemoryProductRepository _repository = new();
    private readonly FixedPricing _pricing = new();
    private readonly FixedStock _stock = new();

    [Fact]
    public async Task OffersOnlyProductsThatHaveAPrice()
    {
        var products = await SaveProductsAsync(6);
        _pricing.Price(products[1], Money.Euro(12.50m));
        _pricing.Price(products[4], Money.Euro(7m));

        var result = await UseCase(new Random(3)).ExecuteAsync(new GetProductSelectionQuery());

        Assert.Equal(new[] { products[1].Id, products[4].Id }.ToHashSet(), result.Products.Select(p => p.ProductId).ToHashSet());
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(Money.Euro(12.50m), result.Products.Single(p => p.ProductId == products[1].Id).CurrentPrice);
    }

    [Fact]
    public async Task OffersAtMostFourPricedProducts()
    {
        var products = await SaveProductsAsync(10);
        foreach (var product in products)
        {
            _pricing.Price(product, Money.Euro(5m));
        }

        var result = await UseCase(new Random(3)).ExecuteAsync(new GetProductSelectionQuery());

        Assert.Equal(4, result.Products.Count);
        Assert.Equal(4, result.Products.Select(p => p.ProductId).Distinct().Count());
    }

    [Fact]
    public async Task OffersAPricedProductThatIsOutOfStock()
    {
        var products = await SaveProductsAsync(1);
        _pricing.Price(products[0], Money.Euro(9m));
        _stock.Stock(products[0], 0);

        var result = await UseCase(new Random(3)).ExecuteAsync(new GetProductSelectionQuery());

        var offered = Assert.Single(result.Products);
        Assert.Equal(products[0].Id, offered.ProductId);
        Assert.False(offered.CanBePurchased);
    }

    [Fact]
    public async Task KeepsTheOrderTheProductsWereDrawnIn()
    {
        var products = await SaveProductsAsync(8);
        foreach (var product in products)
        {
            _pricing.Price(product, Money.Euro(5m));
        }

        var catalogOrder = (await _repository.FindAllAsync()).Select(p => p.Id).ToList();
        var drawn = ProductSelection.Draw(catalogOrder, new Random(11)).ProductIds;

        var result = await UseCase(new Random(11)).ExecuteAsync(new GetProductSelectionQuery());

        Assert.Equal(drawn, result.Products.Select(p => p.ProductId).ToList());
    }

    [Fact]
    public async Task OffersNothingWhenNoProductHasAPrice()
    {
        await SaveProductsAsync(5);

        var result = await UseCase(new Random(3)).ExecuteAsync(new GetProductSelectionQuery());

        Assert.Empty(result.Products);
    }

    private GetProductSelectionUseCase UseCase(Random random) =>
        new(_repository, _pricing, new ProductArticleAssembler(_pricing, _stock), random);

    private async Task<IReadOnlyList<DcaShop.Product.Domain.Model.Product>> SaveProductsAsync(int count)
    {
        var products = new List<DcaShop.Product.Domain.Model.Product>();
        for (var i = 0; i < count; i++)
        {
            var product = Factory.Create(Sku.Of($"SKU-{i}"), ProductName.Of($"Product {i:D2}"), ProductDescription.Empty(), Category.Books(), ImageUrl.None(), Price.Of(Money.Euro(1m)), 1);
            await _repository.SaveAsync(product);
            products.Add(product);
        }
        return products;
    }

    /// <summary>Pricing's answer: only the products priced here are in it, as with a product nobody has priced yet.</summary>
    private sealed class FixedPricing : IPricingDataPort
    {
        private readonly Dictionary<ProductId, PriceData> _prices = new();

        public void Price(DcaShop.Product.Domain.Model.Product product, Money price) => _prices[product.Id] = new PriceData(product.Id, price);

        public Task<IReadOnlyDictionary<ProductId, PriceData>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, PriceData>>(
                productIds.Where(_prices.ContainsKey).ToDictionary(id => id, id => _prices[id]));
    }

    private sealed class FixedStock : IProductStockDataPort
    {
        private readonly Dictionary<ProductId, StockData> _stock = new();

        public void Stock(DcaShop.Product.Domain.Model.Product product, int available) =>
            _stock[product.Id] = new StockData(product.Id, available, available > 0);

        public Task<IReadOnlyDictionary<ProductId, StockData>> GetStockDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, StockData>>(
                productIds.Where(_stock.ContainsKey).ToDictionary(id => id, id => _stock[id]));
    }
}