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
