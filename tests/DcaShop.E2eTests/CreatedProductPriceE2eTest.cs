using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.E2eTests.Pages;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DcaShop.E2eTests;

/// <summary>
/// The price a member of staff gives a new product over the REST API, as the answer carries it and as a visitor then
/// sees it on the catalogue card. The product goes into a shop of the test's own, so the shared shop keeps the seeded
/// catalogue the other suites count.
/// </summary>
public sealed class CreatedProductPriceE2eTest : BaseE2eTest, IDisposable
{
    private readonly SideShop _shop = SideShop.Start();

    public CreatedProductPriceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A price is rounded half up to cents")]
    public async Task APriceWithAHalfCentIsRoundedUpOnTheAnswerAndTheCard()
    {
        var staff = Bearer(await StaffTokenAsync("rounding-mug@example.com"));

        var response = await staff.PostAsync("/api/products", new StringContent(
            """{"sku":"ROUND-001","name":"Rounding Mug","description":"","imageUrl":"","price":14.995,"category":"Desk & Office","stock":5}""",
            System.Text.Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("15.00", body.GetProperty("price").GetRawText());
        Assert.Equal("15.00 EUR", await CardPriceOncePricedAsync("Rounding Mug"));
    }

    public void Dispose() => _shop.Dispose();

    /// <summary>
    /// The card's price once Pricing has taken it over from the creation: the catalogue is opened again until the card
    /// shows more than the "0.00 EUR" of a product nobody has priced yet.
    /// </summary>
    private async Task<string> CardPriceOncePricedAsync(string name)
    {
        for (var i = 0; i < 50; i++)
        {
            var price = await (await ProductCatalogPage.NavigateToAsync(Page, _shop.BaseUrl)).CardPriceAsync(name);
            if (price != "0.00 EUR")
            {
                return price;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"the card of {name} shows no price within 5 seconds");
        throw new InvalidOperationException("unreachable");
    }

    private HttpClient Bearer(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(_shop.BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// A staff token for a freshly registered account. No registration path hands the role out, so the test mints
    /// the token the way an operator provisioning tool would, with the secret the shop under test is started with.
    /// </summary>
    private async Task<string> StaffTokenAsync(string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_shop.BaseUrl) };
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var token = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        var tokens = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = JwtOptions.DevelopmentSecret }),
            NullLogger<JwtTokenService>.Instance);
        return tokens.GenerateRegisteredToken(
            UserId.Of(SubjectOf(token)),
            email,
            new HashSet<string> { IIdentityProvider.IIdentity.RoleCustomer, IIdentityProvider.IIdentity.RoleStaff });
    }

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}