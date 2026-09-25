using DcaShop.Product.Adapter.Outgoing.Persistence;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Product;

/// <summary>
/// A SKU names one product. The store claims it the way a unique column does, so a check made before the save
/// cannot be overtaken between the two.
/// </summary>
public sealed class SkuUniquenessTest
{
    private readonly InMemoryProductRepository _products = new();

    [Fact]
    public async Task ASecondProductUnderTheSameSkuIsRefused()
    {
        await _products.SaveAsync(ProductWith("SKU-TAKEN"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _products.SaveAsync(ProductWith("SKU-TAKEN")));
    }

    [Fact]
    public async Task SavingTheSameProductAgainKeepsItsSku()
    {
        var product = await _products.SaveAsync(ProductWith("SKU-OWN"));

        await _products.SaveAsync(product);

        Assert.Equal(product.Id, (await _products.FindBySkuAsync(Sku.Of("SKU-OWN")))!.Id);
    }

    private static DcaShop.Product.Domain.Model.Product ProductWith(string sku) =>
        DcaShop.Product.Domain.Model.Product.Create(
            Sku.Of(sku),
            ProductName.Of("Thing"),
            ProductDescription.Of("A thing"),
            Category.Of("Books"),
            ImageUrl.Of(string.Empty),
            Price.Of(Money.Euro(10)),
            5);
}