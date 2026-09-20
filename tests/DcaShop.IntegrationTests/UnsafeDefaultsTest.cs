using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The guards of <c>DevelopmentDefaultsTest</c> seen from outside: a shop that is not told it is a development run
/// does not start on the values this repository ships.
/// <para>
/// The unit test pins what the rule says; this one pins that the rule is wired — a validator nobody registers
/// refuses nothing, and that difference is invisible in the class itself. Same scenarios as the Java sample's
/// <c>UnsafeDefaultsIntegrationTest</c>.
/// </para>
/// </summary>
public sealed class UnsafeDefaultsTest
{
    [Fact]
    public void TheCommittedDefaultsDoNotStartAShopThatIsNotInDevelopment()
    {
        var failure = Assert.ThrowsAny<Exception>(() => Start());
        var message = Messages(failure);

        Assert.Contains("Jwt__Secret", message, StringComparison.Ordinal);
        Assert.Contains("Jwt__SecureCookies", message, StringComparison.Ordinal);
    }

    [Fact]
    public void NorDoTheCommittedOperatorCredentials()
    {
        var failure = Assert.ThrowsAny<Exception>(() => Start(
            ("Jwt:Secret", "a-secret-nobody-else-has-and-long-enough"),
            ("Jwt:SecureCookies", "true")));
        var message = Messages(failure);

        Assert.Contains("Backoffice__Password", message, StringComparison.Ordinal);
        Assert.Contains("Backoffice__SecureCookies", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConfiguredShopStarts()
    {
        using var client = Start(
            ("Jwt:Secret", "a-secret-nobody-else-has-and-long-enough"),
            ("Jwt:SecureCookies", "true"),
            ("Backoffice:Username", "operator"),
            ("Backoffice:Password", "not-the-committed-one"),
            ("Backoffice:SecureCookies", "true"));

        Assert.NotNull(client);
    }

    [Fact]
    public async Task ADevelopmentRunStartsOnTheShippedValues()
    {
        using var factory = new WebApplicationFactory<Program>();

        var response = await factory.CreateClient().GetAsync("/products");

        Assert.True(response.IsSuccessStatusCode);
    }

    private static HttpClient Start(params (string Key, string Value)[] settings)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

        // The host is built lazily: creating a client is what starts it, and what makes a refused configuration
        // surface here rather than on the first request.
        return factory.CreateClient();
    }

    private static string Messages(Exception failure)
    {
        var text = new System.Text.StringBuilder();
        for (Exception? cause = failure; cause is not null; cause = cause.InnerException)
        {
            text.AppendLine(cause.Message);
        }

        return text.ToString();
    }
}
