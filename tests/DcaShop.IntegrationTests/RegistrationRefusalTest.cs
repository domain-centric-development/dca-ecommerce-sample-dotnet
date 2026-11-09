using DcaShop.Account.Application.IsAccountRegistered;
using DcaShop.Account.Application.RegisterAccount;
using DcaShop.Account.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The registration's refusals through its input port, in the wired shop: each is a case of the result, and a refused
/// registration stores no account.
/// </summary>
public sealed class RegistrationRefusalTest : IClassFixture<ShopFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegistrationRefusalTest(ShopFactory factory) => _factory = factory;

    [Fact]
    public async Task APasswordThePolicyRefusesNamesTheRuleAndStoresNoAccount()
    {
        var userId = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<IRegisterAccountInputPort>().ExecuteAsync(
            new RegisterAccountCommand(
                $"anna.schmidt-{Guid.NewGuid():N}@example.com", "secret123", userId, "Anna", "Schmidt", new DateOnly(1990, 5, 17)));

        Assert.Equal(PasswordRule.NoUppercaseLetter, Assert.IsType<RegisterAccountResult.PasswordTooWeak>(result.Value).BrokenRule);
        var registered = await scope.ServiceProvider.GetRequiredService<IIsAccountRegisteredInputPort>()
            .ExecuteAsync(new IsAccountRegisteredQuery(userId));
        Assert.False(registered.Registered);
    }

    [Fact]
    public async Task AnAddressAnotherAccountHoldsIsRefused()
    {
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        using var scope = _factory.Services.CreateScope();
        var registerAccount = scope.ServiceProvider.GetRequiredService<IRegisterAccountInputPort>();
        var first = await registerAccount.ExecuteAsync(
            new RegisterAccountCommand(email, "Secret123", Guid.NewGuid().ToString(), "Anna", "Schmidt", new DateOnly(1990, 5, 17)));
        Assert.IsType<RegisterAccountResult.Registered>(first.Value);

        var second = await registerAccount.ExecuteAsync(
            new RegisterAccountCommand(email, "Secret123", Guid.NewGuid().ToString(), "Anne", "Schmidt", new DateOnly(1991, 6, 18)));

        Assert.Equal(email, Assert.IsType<RegisterAccountResult.EmailAlreadyRegistered>(second.Value).Email.Value);
    }
}