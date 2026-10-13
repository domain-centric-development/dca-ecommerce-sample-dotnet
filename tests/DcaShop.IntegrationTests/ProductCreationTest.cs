using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using DcaShop.Product.Application.CreateProduct;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Creating a product in the wired shop, and refusing a stranger who tries. Each test starts a shop of its own, so
/// the seeded catalogue it counts is not touched by the products other tests create.
/// </summary>
public sealed class ProductCreationTest
{
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);

    [Fact]
    public async Task ACreatedProductJoinsTheCatalogueWithItsPriceAndStock()
    {
        using var shop = new WebApplicationFactory<Program>();
        using (var scope = shop.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ICreateProductInputPort>().ExecuteAsync(new CreateProductCommand(
                "MUG-002", "Aggregate Mug", "A mug with a consistency boundary.", string.Empty, 14.99m, "EUR", "Desk & Office", 12));
        }

        var client = shop.CreateClient();
        var catalogue = await Eventually(
            () => client.GetStringAsync("/products"),
            page => CardOf(page, "Aggregate Mug") is { } card && card.Contains("product-card__badge--available", StringComparison.Ordinal));

        Assert.Equal(22, Cards(catalogue).Count);
        var mug = CardOf(catalogue, "Aggregate Mug")!;
        Assert.Equal("14.99 EUR", Text(mug, @"class=""product-card__price"">([^<]*)</div>"));
        Assert.Equal("In Stock", Text(mug, @"class=""product-card__badge[^""]*"">([^<]*)</span>"));
    }

    [Fact]
    public async Task AStrangerCreatingAProductIsChallengedAndTheCatalogueKeepsItsProducts()
    {
        using var shop = new WebApplicationFactory<Program>();

        var response = await shop.CreateClient().PostAsync("/api/products", new StringContent(
            """{"sku":"MUG-004","name":"Stranger Mug","description":"","imageUrl":"","price":9.99,"category":"Desk & Office","stock":1}""",
            Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unauthorized", body.GetProperty("title").GetString());
        using var scope = shop.Services.CreateScope();
        var catalogue = await scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery());
        Assert.Equal(21, catalogue.Products.Count);
    }

    /// <summary>The markup of each catalogue card, in page order.</summary>
    private static List<string> Cards(string catalogue) =>
        catalogue.Split("data-test=\"product-card\"").Skip(1).ToList();

    private static string? CardOf(string catalogue, string name) =>
        Cards(catalogue).SingleOrDefault(card => CardTitle.Match(card) is { Success: true } title
            && WebUtility.HtmlDecode(title.Groups[1].Value).Trim() == name);

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    private static async Task<T> Eventually<T>(Func<Task<T>> read, Func<T, bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            var value = await read();
            if (condition(value))
            {
                return value;
            }

            await Task.Delay(100);
        }

        Assert.Fail("condition not met within 5 seconds");
        throw new InvalidOperationException("unreachable");
    }
}