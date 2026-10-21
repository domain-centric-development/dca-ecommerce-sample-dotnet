using System.Text.Json;

using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// A customer's browser that sends the cart take-over for the guest cart of another browser takes nothing: the
/// customer's cart stays as it was, and so does the other guest's.
/// </summary>
public sealed class AnotherGuestsCartE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string CleanArchitecture = "Clean Architecture";

    public AnotherGuestsCartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Another guests cart is not taken over")]
    public async Task SendingTheTakeOverForAnotherBrowsersGuestCartLeavesBothCartsAsTheyWere()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, Password);
        await CartPage.OpenAsync(Page);
        await using var othersBrowser = await NewBrowserAsync();
        var other = await othersBrowser.NewPageAsync();
        var catalog = await ProductCatalogPage.NavigateToAsync(other);
        await (await catalog.ViewProductAsync(CleanArchitecture)).AddToCartAsync();

        await SendTheTakeOverForAsync(await GuestOfAsync(othersBrowser));

        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart", cart.ShownPath);
        Assert.Equal(0, await cart.ItemCountAsync());
        var othersCart = await CartPage.NavigateToAsync(other);
        Assert.Equal([$"{CleanArchitecture} x1"], await othersCart.LineItemsAsync());
    }

    /// <summary>
    /// Sends from this browser the form the page after a login sends, naming <paramref name="guest"/>'s cart — with the
    /// form token this browser's own page carries — and waits for the page the shop answers with.
    /// </summary>
    private Task SendTheTakeOverForAsync(string guest) =>
        Page.RunAndWaitForNextPageAsync(() => Page.EvaluateAsync(
            """
            guest => {
                const form = document.createElement('form');
                form.method = 'POST';
                form.action = '/cart/merge/take-over';
                const field = (name, value) => {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = name;
                    input.value = value;
                    form.appendChild(input);
                };
                field('anonymousUserId', guest);
                field('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
                document.body.appendChild(form);
                form.submit();
            }
            """,
            guest));

    /// <summary>The guest a browser is, as its identity cookie names it: the subject of the token it carries.</summary>
    private static async Task<string> GuestOfAsync(IBrowserContext browser)
    {
        var identity = (await browser.CookiesAsync()).Single(cookie => cookie.Name == "shop-identity").Value;
        var payload = identity.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var claims = JsonDocument.Parse(Convert.FromBase64String(payload));
        return claims.RootElement.GetProperty("sub").GetString()!;
    }
}