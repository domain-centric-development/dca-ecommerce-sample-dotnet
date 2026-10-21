using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Application.RegisterAccount;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The auth resource's translation of a registration: which bodies it hands on to the use case and how it refuses the
/// others. The use case behind it is a stand-in that records what it was asked.
/// </summary>
public sealed class RegistrationResourceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RecordingRegisterAccount _registerAccount = new();

    public RegistrationResourceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IRegisterAccountInputPort>(_registerAccount)));
    }

    [Fact]
    public async Task TodayAsDateOfBirthIsAnUnacceptableValueNamingTheField()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("grace@example.com", Today()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        var reasons = problem.GetProperty("errors").GetProperty("dateOfBirth");
        Assert.Equal("The date of birth must lie in the past.", Assert.Single(reasons.EnumerateArray()).GetString());
        Assert.DoesNotContain(_registerAccount.Commands, command => command.Email == "grace@example.com");
    }

    [Fact]
    public async Task YesterdayAsDateOfBirthIsHandedOnToTheRegistration()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("linus@example.com", yesterday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(_registerAccount.Commands, command => command.Email == "linus@example.com" && command.DateOfBirth == yesterday);
    }

    private static string Today() => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static object Registration(string email, string dateOfBirth) => new
    {
        email,
        password = "Secret123",
        firstName = "Grace",
        lastName = "Hopper",
        dateOfBirth,
    };

    /// <summary>Answers every registration with a customer account for the address it was asked for.</summary>
    private sealed class RecordingRegisterAccount : IRegisterAccountInputPort
    {
        public List<RegisterAccountCommand> Commands { get; } = [];

        public Task<RegisterAccountResult> ExecuteAsync(RegisterAccountCommand command, CancellationToken cancellationToken = default)
        {
            lock (Commands)
            {
                Commands.Add(command);
            }

            return Task.FromResult(new RegisterAccountResult(
                Guid.NewGuid().ToString(), command.CurrentUserId, command.Email, new HashSet<Role> { Role.Customer }));
        }
    }
}