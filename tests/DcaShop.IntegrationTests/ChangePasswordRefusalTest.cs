using DcaShop.Account.Application.ChangePassword;
using DcaShop.Account.Application.RegisterAccount;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The password change through its input port in the wired application: which refusal the use case reports when
/// more than one rule is broken.
/// </summary>
public sealed class ChangePasswordRefusalTest : IClassFixture<ShopFactory>
{
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public ChangePasswordRefusalTest(ShopFactory factory) => _factory = factory;

    [Fact]
    public async Task AWrongCurrentPasswordIsReportedBeforeAWeakNewOne()
    {
        var userId = await RegisteredAnnaAsync();
        using var scope = _factory.Services.CreateScope();
        var changePassword = scope.ServiceProvider.GetRequiredService<IChangePasswordInputPort>();

        var result = await changePassword.ExecuteAsync(new ChangePasswordCommand(userId, "Wrong999", "weakpass"));

        Assert.IsType<ChangePasswordResult.CurrentPasswordInvalid>(result.Value);
    }

    /// <summary>Registers Anna under an address and a visitor identity of her own; answers that identity.</summary>
    private async Task<string> RegisteredAnnaAsync()
    {
        var userId = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var registered = await scope.ServiceProvider.GetRequiredService<IRegisterAccountInputPort>().ExecuteAsync(
            new RegisterAccountCommand(
                $"anna.schmidt-{Guid.NewGuid():N}@example.com", Password, userId, "Anna", "Schmidt", new DateOnly(1990, 5, 17)));
        Assert.Equal(userId, Assert.IsType<RegisterAccountResult.Registered>(registered.Value).UserId);
        return userId;
    }
}