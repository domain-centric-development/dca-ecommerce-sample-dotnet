using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The backoffice through the browser: login, the event publication log and its counts, a failed login, logout,
/// and the redirect that keeps the log away from anyone who has not signed in. Port of the Java sample's
/// <c>BackofficeE2ETest</c>.
/// </summary>
/// <remarks>
/// Requires the shop running with the default operator credentials (<c>admin</c>/<c>admin</c>) and at least one
/// published event — the sample data seeder publishes several at startup.
/// </remarks>
public sealed class BackofficeE2eTest : BaseE2eTest
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    public BackofficeE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Login as admin and view the event log")]
    public async Task LoginAndViewEventLog()
    {
        var events = await SignInAsync();

        Assert.True(await events.IsOnPageAsync(), "Should be on the event log page after login");
        Assert.True(await events.HasSummaryAsync(), "Event log should display the summary section");
        Assert.True(await events.TotalEventsAsync() >= 0, "Total events count should be a non-negative number");
    }

    [E2eFact(DisplayName = "Event log shows published events with correct counts")]
    public async Task EventLogShowsPublishedEvents()
    {
        var events = await SignInAsync();

        var total = await events.TotalEventsAsync();
        Assert.True(total > 0, "Event log should contain events published during application startup");
        Assert.True(await events.EventCountAsync() > 0, "Event list should render at least one event item");
        Assert.False(string.IsNullOrWhiteSpace(await events.FirstEventTypeAsync()), "First event type label should not be empty");

        var completed = await events.CompletedCountAsync();
        var incomplete = await events.IncompleteCountAsync();
        Assert.Equal(total, completed + incomplete);
    }

    [E2eFact(DisplayName = "Invalid credentials show a login error message")]
    public async Task InvalidLoginShowsError()
    {
        var login = await BackofficeLoginPage.NavigateToAsync(Page);
        await login.FillCredentialsAsync(AdminUsername, "wrongpassword");
        await login.SubmitExpectingErrorAsync();

        Assert.True(await login.ShowsLoginErrorAsync(), "Login page should display an error after failed authentication");
    }

    [E2eFact(DisplayName = "Logout redirects to login page and shows logout confirmation")]
    public async Task LogoutRedirectsToLoginPage()
    {
        var events = await SignInAsync();

        var login = await events.LogoutAsync();

        Assert.True(await login.ShowsLogoutMessageAsync(), "Login page should confirm the logout");
    }

    [E2eFact(DisplayName = "Unauthenticated access to events page redirects to login")]
    public async Task UnauthenticatedAccessRedirectsToLogin()
    {
        await ClearCookiesAsync();
        await NavigateToAsync("/backoffice/events");

        Assert.Contains("/backoffice/login", CurrentPath, StringComparison.Ordinal);
    }

    [E2eFact(DisplayName = "Event payload can be expanded to reveal details")]
    public async Task EventPayloadCanBeExpanded()
    {
        var events = await SignInAsync();
        Assert.True(await events.EventCountAsync() > 0, "At least one event must exist before expanding a payload");

        await events.ExpandFirstEventPayloadAsync();

        Assert.False(string.IsNullOrWhiteSpace(await events.FirstEventPayloadTextAsync()), "Expanded payload should have content");
    }

    private async Task<BackofficeEventsPage> SignInAsync()
    {
        var login = await BackofficeLoginPage.NavigateToAsync(Page);
        await login.FillCredentialsAsync(AdminUsername, AdminPassword);
        return await login.SubmitAsync();
    }
}
