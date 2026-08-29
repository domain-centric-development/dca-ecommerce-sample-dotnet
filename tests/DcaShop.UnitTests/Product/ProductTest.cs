using DcaShop.Product.Domain.Event;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class ProductTest
{
    private static readonly ProductFactory Factory = new();

    [Fact]
    public void FactoryRaisesProductCreatedWithInitialPriceAndStock()
    {
        var product = Factory.Create(Sku.Of("ABC-1"), ProductName.Of("Thing"), ProductDescription.Empty(), Category.Books(), ImageUrl.None(), Price.Of(Money.Euro(10m)), 5);

        var created = Assert.IsType<ProductCreated>(Assert.Single(product.DomainEvents));
        Assert.Equal(product.Id, created.ProductId);
        Assert.Equal(Money.Euro(10m), created.InitialPrice.Value);
        Assert.Equal(5, created.InitialStock);
    }

    [Fact]
    public void UpdateNameRaisesEventWithOldAndNewName()
    {
        var product = Factory.Create(Sku.Of("ABC-1"), ProductName.Of("Old"), ProductDescription.Empty(), Category.Books(), ImageUrl.None(), Price.Of(Money.Euro(10m)), 5);
        product.ClearDomainEvents();

        product.UpdateName(ProductName.Of("New"));

        var changed = Assert.IsType<ProductNameChanged>(Assert.Single(product.DomainEvents));
        Assert.Equal("Old", changed.OldName.Value);
        Assert.Equal("New", changed.NewName.Value);
        Assert.Equal("New", product.Name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("lower-case")]
    [InlineData("HAS SPACE")]
    public void SkuRejectsInvalidFormats(string value) =>
        Assert.Throws<ArgumentException>(() => Sku.Of(value));

    [Fact]
    public void EnrichedProductKnowsWhetherItCanBePurchased()
    {
        var product = Factory.Create(Sku.Of("ABC-1"), ProductName.Of("Thing"), ProductDescription.Empty(), Category.Books(), ImageUrl.None(), Price.Of(Money.Euro(10m)), 5);

        Assert.True(EnrichedProduct.From(product, new ProductArticle(Money.Euro(10m), 3, true)).CanBePurchased);
        Assert.False(EnrichedProduct.From(product, new ProductArticle(Money.Euro(10m), 0, false)).CanBePurchased);
    }
}
