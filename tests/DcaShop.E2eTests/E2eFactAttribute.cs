namespace DcaShop.E2eTests;

/// <summary>
/// An E2E test. It runs against the shop the suite starts itself (<see cref="ShopUnderTest"/>), or against the one
/// <c>E2E_BASE_URL</c> names.
/// </summary>
public sealed class E2eFactAttribute : FactAttribute
{
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

        if (embedded && !shopRunsEmbedded)
        {
            Skip = "needs a shop started with Jwt__SameSite=None Jwt__SecureCookies=true behind TLS (E2E_EMBEDDED=true)";
        }
        else if (embedded && !ShopUnderTest.BaseUrl.Contains("localhost", StringComparison.Ordinal))
        {
            Skip = "the cross-site case pairs localhost with 127.0.0.1; point E2E_BASE_URL at localhost to run it";
        }
        else if (!embedded && shopRunsEmbedded)
        {
            Skip = "the shop under test runs embedded — framing is allowed there by design";
        }
    }
}