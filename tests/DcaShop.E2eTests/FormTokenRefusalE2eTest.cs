using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>A form post that did not come from the shop's own page is refused, whatever it carries besides.</summary>
public sealed class FormTokenRefusalE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public FormTokenRefusalE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A login post without the form token is refused")]
    public async Task ALoginPostedWithoutTheTokenOfTheLoginPageIsForbiddenAndLogsNobodyIn()
    {
        // The browser suite shares one shop, so Ada's address carries a suffix of its own.
        var email = $"ada-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Ada", "Lovelace", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        await LoginPage.NavigateToAsync(Page);

        // Posted from this browser, with its cookies, but with the fields alone: the login page's token is not sent.
        var answer = await Page.APIRequest.PostAsync(
            BaseUrl + "/login",
            new APIRequestContextOptions
            {
                Form = Page.APIRequest.CreateFormData().Set("email", email).Set("password", Password),
                MaxRedirects = 0,
            });

        Assert.Equal(403, answer.Status);
        var login = await LoginPage.NavigateToAsync(Page);
        Assert.True(await login.HeaderOffersLoginAsync(), "the header still offers to log in");
        Assert.True(await login.HeaderOffersRegisterAsync(), "the header still offers to register");
    }
}