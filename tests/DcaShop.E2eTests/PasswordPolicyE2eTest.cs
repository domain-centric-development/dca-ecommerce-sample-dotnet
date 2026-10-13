using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A visitor registers with a password the policy refuses, and the shop names the rule it breaks.</summary>
public sealed class PasswordPolicyE2eTest : BaseE2eTest
{
    public PasswordPolicyE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Password without uppercase is refused")]
    public async Task APasswordWithoutAnUppercaseLetterIsRefusedAndTheVisitorStaysLoggedOut()
    {
        // The shop is shared by the whole run, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);

        var error = await register.RegisterRefusedAsync("Anna", "Schmidt", "1990-05-17", email, "secret123", "secret123");

        Assert.Equal("Password must contain an uppercase letter.", error);
        Assert.Equal(string.Empty, await register.HeaderGreetingAsync());
        Assert.True(await register.HeaderOffersRegisterAsync(), "the header still offers to register");
    }
}