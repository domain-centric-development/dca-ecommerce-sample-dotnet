using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A customer with an account logs in from a browser that holds nothing of it yet.</summary>
public sealed class LoginE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public LoginE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Login opens the cart")]
    public async Task ALoginWithAnEmptyCartOpensTheCartAndGreetsTheCustomer()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();

        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, Password);

        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart?notice=welcome-back", cart.ShownPath);
        Assert.Equal("Welcome back!", await cart.SuccessMessageAsync());
        Assert.Equal($"Welcome, {email}", await cart.HeaderGreetingAsync());
        Assert.Equal("Logout", await cart.HeaderLogoutButtonAsync());
        Assert.False(await cart.HeaderOffersLoginAsync(), "the header no longer offers to log in");
        Assert.False(await cart.HeaderOffersRegisterAsync(), "the header no longer offers to register");
    }

    [E2eFact(DisplayName = "Wrong password is refused")]
    public async Task AWrongPasswordKeepsTheTypedEmailAndSaysOnlyThatTheLoginFailed()
    {
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        var login = await LoginPage.NavigateToAsync(Page);

        await login.SubmitRefusedAsync(email, "Secret999");

        Assert.Equal("Invalid email or password", await login.ErrorMessageTextAsync());
        Assert.Equal(email, await login.EmailValueAsync());
        Assert.Equal(string.Empty, await login.PasswordValueAsync());
        Assert.True(await login.HeaderOffersLoginAsync(), "the header still offers to log in");
        Assert.True(await login.HeaderOffersRegisterAsync(), "the header still offers to register");
    }
}