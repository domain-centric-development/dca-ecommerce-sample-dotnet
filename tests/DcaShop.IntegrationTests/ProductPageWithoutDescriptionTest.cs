using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Product.Application.GetProductById;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The product page's translation of a product that has no description: the page adapter against a product lookup
/// that answers with such a product, since the seeded catalogue describes every product it holds.
/// </summary>
public sealed class ProductPageWithoutDescriptionTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly ProductId Tote = new(Guid.Parse("6c1f7a52-0d3e-4b8a-9f61-2a7e5c4d9b10"));

    private readonly WebApplicationFactory<Program> _factory;

    public ProductPageWithoutDescriptionTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task APageOfAProductWithoutDescriptionSaysThatThereIsNone()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(s => s.AddSingleton<IGetProductByIdInputPort>(new ProductWithoutDescription())))
            .CreateClient();

        var page = await client.GetStringAsync($"/products/{Tote.Value}");

        var description = Regex.Match(page, @"class=""product-detail__description""><p>([^<]*)</p>");
        Assert.True(description.Success, "the page has a description block");
        Assert.Equal("No description available.", WebUtility.HtmlDecode(description.Groups[1].Value).Trim());
    }

    /// <summary>The lookup answers every id with "Bounded Context Tote", created without a description.</summary>
    private sealed class ProductWithoutDescription : IGetProductByIdInputPort
    {
        public Task<GetProductByIdResult> ExecuteAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GetProductByIdResult(new EnrichedProduct(
                Tote,
                Sku.Of("TOTE-001"),
                ProductName.Of("Bounded Context Tote"),
                ProductDescription.Empty(),
                Category.Of("Desk & Office"),
                ImageUrl.Of(string.Empty),
                new ProductArticle(Money.Euro(19.99m), 10, true))));
    }
}