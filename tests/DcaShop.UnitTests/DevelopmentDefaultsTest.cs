using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.Account.Infrastructure;
using DcaShop.Backoffice.Adapter.Incoming.Web;
using DcaShop.Backoffice.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DcaShop.UnitTests;

/// <summary>
/// The values this repository ships so the shop starts without configuration, and the guard that keeps them where
/// they belong. Same scenarios as the Java sample's <c>DevelopmentDefaultsTest</c>.
/// </summary>
public sealed class DevelopmentDefaultsTest
{
    private const string OwnSecret = "a-secret-nobody-else-has-and-long-enough-for-hs256";

    [Fact]
    public void TheCommittedDefaultsInAppsettingsAreTheOnesTheGuardKnows()
    {
        var settings = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "DcaShop.Web", "appsettings.json"));

        Assert.Contains($"\"Secret\": \"{JwtOptions.DevelopmentSecret}\"", settings, StringComparison.Ordinal);
        Assert.Contains($"\"Username\": \"{BackofficeOptions.DevelopmentUsername}\"", settings, StringComparison.Ordinal);
        Assert.Contains($"\"Password\": \"{BackofficeOptions.DevelopmentPassword}\"", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void OutsideDevelopmentTheCommittedSigningSecretIsRefused()
    {
        var result = JwtValidator("Production").Validate(null, Jwt(JwtOptions.DevelopmentSecret, secureCookies: true));

        Assert.True(result.Failed);
        Assert.Contains("Jwt__Secret", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void OutsideDevelopmentCookiesThatAreNotSecureAreRefused()
    {
        var result = JwtValidator("Production").Validate(null, Jwt(OwnSecret, secureCookies: false));

        Assert.True(result.Failed);
        Assert.Contains("Jwt__SecureCookies", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void OutsideDevelopmentTheCommittedOperatorCredentialsAreRefused()
    {
        var result = BackofficeValidator("Production").Validate(null, new BackofficeOptions { SecureCookies = true });

        Assert.True(result.Failed);
        Assert.Contains("Backoffice__Password", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void OutsideDevelopmentAnOperatorSessionCookieThatIsNotSecureIsRefused()
    {
        var options = new BackofficeOptions { Username = "operator", Password = "not-the-committed-one" };

        var result = BackofficeValidator("Production").Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Backoffice__SecureCookies", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguredValuesStartTheShop()
    {
        var operatorOptions = new BackofficeOptions
        {
            Username = "operator", Password = "not-the-committed-one", SecureCookies = true,
        };

        Assert.True(JwtValidator("Production").Validate(null, Jwt(OwnSecret, secureCookies: true)).Succeeded);
        Assert.True(BackofficeValidator("Production").Validate(null, operatorOptions).Succeeded);
    }

    [Fact]
    public void InDevelopmentTheShippedValuesAreWhatTheSampleIsFor()
    {
        var shipped = Jwt(JwtOptions.DevelopmentSecret, secureCookies: false);

        Assert.True(JwtValidator("Development").Validate(null, shipped).Succeeded);
        Assert.True(BackofficeValidator("Development").Validate(null, new BackofficeOptions()).Succeeded);
    }

    private static JwtDevelopmentDefaultsValidator JwtValidator(string environment) =>
        new(new Environment(environment));

    private static BackofficeDevelopmentDefaultsValidator BackofficeValidator(string environment) =>
        new(new Environment(environment));

    private static JwtOptions Jwt(string secret, bool secureCookies) =>
        new() { Secret = secret, SecureCookies = secureCookies };

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DcaShop.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("DcaShop.sln not found above the test binaries");
    }

    private sealed class Environment : IHostEnvironment
    {
        public Environment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "DcaShop.Web";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
