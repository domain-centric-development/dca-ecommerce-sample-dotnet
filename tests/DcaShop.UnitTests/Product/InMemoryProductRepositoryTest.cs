using DcaShop.Product.Adapter.Outgoing.Persistence;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class InMemoryProductRepositoryTest
{
    private static readonly ProductFactory Factory = new();

    [Fact]
    public async Task FindAllIsOrderedByProductNameWhateverTheOrderTheyWereSavedIn()
    {
        var repository = new InMemoryProductRepository();
        await repository.SaveAsync(Product("SKU-3", "Team Topologies"));
        await repository.SaveAsync(Product("SKU-1", "Hexagon Sticker Sheet"));
        await repository.SaveAsync(Product("SKU-2", "Clean Architecture"));
        await repository.SaveAsync(Product("SKU-4", "\"Ports & Adapters\" T-Shirt"));

        var names = (await repository.FindAllAsync()).Select(p => p.Name.Value).ToList();

        Assert.Equal(
            new[] { "\"Ports & Adapters\" T-Shirt", "Clean Architecture", "Hexagon Sticker Sheet", "Team Topologies" },
            names);
    }

    private static DcaShop.Product.Domain.Model.Product Product(string sku, string name) =>
        Factory.Create(Sku.Of(sku), ProductName.Of(name), ProductDescription.Empty(), Category.Books(), ImageUrl.None(), Price.Of(Money.Euro(1m)), 1);
}
