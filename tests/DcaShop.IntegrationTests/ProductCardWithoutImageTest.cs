using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The catalogue page's translation of a product that has no image: the page adapter against a catalogue that
/// answers with such a product, since the seeded catalogue pictures every product it holds.
/// </summary>
public sealed class ProductCardWithoutImageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductCardWithoutImageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ACardOfAProductWithoutImageShowsTheFirstLetterOfItsName()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(s => s.AddSingleton<IGetAllProductsInputPort>(new CatalogueWithAMugWithoutImage())))
            .CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var card = catalogue.Split("data-test=\"product-card\"").Skip(1).Single();
        var placeholder = Regex.Match(card, @"class=""product-card__image"".*?<div class=""product-card__image-placeholder"">([^<]*)</div>", RegexOptions.Singleline);
        Assert.True(placeholder.Success, "the card shows a placeholder where the image would be");
        Assert.Equal("A", WebUtility.HtmlDecode(placeholder.Groups[1].Value).Trim());
        Assert.DoesNotContain("<img", card, StringComparison.Ordinal);
    }

    /// <summary>The catalogue holds "Aggregate Mug", created with an empty image address.</summary>
    private sealed class CatalogueWithAMugWithoutImage : IGetAllProductsInputPort
    {
        public Task<GetAllProductsResult> ExecuteAsync(GetAllProductsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GetAllProductsResult([new EnrichedProduct(
                new ProductId(Guid.Parse("2b9e4c71-5a0d-4f3e-8c62-7d1a9e0b4f25")),
                Sku.Of("MUG-002"),
                ProductName.Of("Aggregate Mug"),
                ProductDescription.Of("A mug with a consistency boundary."),
                Category.Of("Desk & Office"),
                ImageUrl.Of(string.Empty),
                new ProductArticle(Money.Euro(14.99m), 12, true))]));
    }
}