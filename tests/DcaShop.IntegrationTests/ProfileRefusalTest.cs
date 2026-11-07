using DcaShop.Account.Application.ChangeProfile;
using DcaShop.Account.Application.GetProfile;
using DcaShop.Account.Application.RegisterAccount;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The profile change through its input port in the wired application: a refused change leaves the stored profile
/// as it was, the valid half of it included.
/// </summary>
public sealed class ProfileRefusalTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public ProfileRefusalTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARefusedChangeKeepsTheStoredAddressAndDateOfBirth()
    {
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var userId = await RegisteredAnnaAsync(email);
        using var scope = _factory.Services.CreateScope();
        var changeProfile = scope.ServiceProvider.GetRequiredService<IChangeProfileInputPort>();
        var refused = await changeProfile.ExecuteAsync(
            new ChangeProfileCommand(userId, $"anna.meyer-{Guid.NewGuid():N}@example.com", new DateOnly(2999, 1, 1)));
        Assert.IsType<ChangeProfileResult.DateOfBirthInvalid>(refused.Value);

        var stored = await scope.ServiceProvider.GetRequiredService<IGetProfileInputPort>()
            .ExecuteAsync(new GetProfileQuery(userId));

        var profile = Assert.IsType<GetProfileResult.Found>(stored.Value).Profile;
        Assert.Equal(email, profile.Email);
        Assert.Equal(new DateOnly(1990, 5, 17), profile.DateOfBirth);
    }

    /// <summary>Registers Anna under <paramref name="email"/> and a visitor identity of her own; answers that identity.</summary>
    private async Task<string> RegisteredAnnaAsync(string email)
    {
        var userId = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var registered = await scope.ServiceProvider.GetRequiredService<IRegisterAccountInputPort>().ExecuteAsync(
            new RegisterAccountCommand(email, Password, userId, "Anna", "Schmidt", new DateOnly(1990, 5, 17)));
        Assert.Equal(userId, Assert.IsType<RegisterAccountResult.Registered>(registered.Value).UserId);
        return userId;
    }
}