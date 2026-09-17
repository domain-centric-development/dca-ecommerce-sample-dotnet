namespace DcaShop.E2eTests;

/// <summary>
/// E2E tests drive a running shop through a real browser, so they only run when <c>E2E_BASE_URL</c> is set
/// (e.g. <c>E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests</c>); otherwise they are skipped.
/// </summary>
public sealed class E2eFactAttribute : FactAttribute
{
    public E2eFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL")))
        {
            Skip = "set E2E_BASE_URL to the running shop to run E2E tests";
        }
    }
}

/// <summary>
/// An E2E test that holds for one of the two deployments only. The shop under test runs embedded when
/// <c>E2E_EMBEDDED=true</c> (started with <c>Jwt__SameSite=None Jwt__SecureCookies=true</c> behind TLS); a test for
/// the other deployment skips instead of failing against a shop it does not describe.
/// </summary>
public sealed class EmbeddedModeFactAttribute : FactAttribute
{
    public EmbeddedModeFactAttribute(bool embedded)
    {
        var shopRunsEmbedded = string.Equals(
            Environment.GetEnvironmentVariable("E2E_EMBEDDED"), "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("E2E_BASE_URL")))
        {
            Skip = "set E2E_BASE_URL to the running shop to run E2E tests";
        }
        else if (embedded && !shopRunsEmbedded)
        {
            Skip = "needs a shop started with Jwt__SameSite=None Jwt__SecureCookies=true behind TLS (E2E_EMBEDDED=true)";
        }
        else if (embedded && !Environment.GetEnvironmentVariable("E2E_BASE_URL")!.Contains("localhost", StringComparison.Ordinal))
        {
            Skip = "the cross-site case pairs localhost with 127.0.0.1; point E2E_BASE_URL at localhost to run it";
        }
        else if (!embedded && shopRunsEmbedded)
        {
            Skip = "the shop under test runs embedded — framing is allowed there by design";
        }
    }
}
