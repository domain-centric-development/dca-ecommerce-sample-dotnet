using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The backoffice through the browser: login, the event publication log, its payload, and the redirect that keeps the
/// log away from anyone who has not signed in. Port of the Java sample's <c>BackofficeE2ETest</c>.
/// </summary>
/// <remarks>
/// Requires the shop running with the default operator credentials (<c>admin</c>/<c>admin</c>) and at least one
/// published event — the sample data seeder publishes several at startup.
/// </remarks>
public sealed class BackofficeE2eTest : BaseE2eTest
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    /// <summary>An address below the back office that no page answers; the prefix guards it all the same.</summary>
    private const string UnknownBackofficeAddress = "/backoffice/settings";

    public BackofficeE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Login opens the event log")]
    public async Task LoginAndViewEventLog()
    {
        var events = await SignInAsync();

        Assert.True(await events.IsOnPageAsync(), "Should be on the event log page after login");
        Assert.Equal("Event Publication Log", await events.HeadingAsync());
        Assert.True(await events.HasSummaryAsync(), "Event log should display the summary section");
        Assert.Equal(["Total Events", "Completed", "Incomplete"], await events.SummaryLabelsAsync());
        Assert.True(await events.TotalEventsAsync() >= 0, "Total events count should be a non-negative number");
    }

    [E2eFact(DisplayName = "Log lists the events published at start up")]
    public async Task EventLogShowsPublishedEvents()
    {
        var events = await SignInAsync();

        var total = await events.TotalEventsAsync();
        Assert.True(total > 0, "Event log should contain events published during application startup");
        Assert.True(await events.EventCountAsync() > 0, "Event list should render at least one event item");
        Assert.False(string.IsNullOrWhiteSpace(await events.FirstEventTypeAsync()), "First event type label should not be empty");
        Assert.All(await events.EventTypesAsync(), type => Assert.EndsWith("Event", type.Trim(), StringComparison.Ordinal));

        var completed = await events.CompletedCountAsync();
        var incomplete = await events.IncompleteCountAsync();
        Assert.Equal(total, completed + incomplete);
    }

    [E2eFact(DisplayName = "Event log sends a visitor to the login")]
    public async Task UnauthenticatedAccessRedirectsToLogin()
    {
        await ClearCookiesAsync();
        await NavigateToAsync("/backoffice/events");

        Assert.Contains("/backoffice/login", CurrentPath, StringComparison.Ordinal);
    }

    [E2eFact(DisplayName = "Login after a redirect opens the event log")]
    public async Task LoginAfterARedirectOpensTheEventLog()
    {
        await ClearCookiesAsync();
        await NavigateToAsync(UnknownBackofficeAddress);
        Assert.StartsWith("/backoffice/login", CurrentPath, StringComparison.Ordinal);

        var login = await BackofficeLoginPage.OpenAsync(Page);
        await login.FillCredentialsAsync(AdminUsername, AdminPassword);
        var events = await login.SubmitAsync();

        Assert.StartsWith("/backoffice/events", CurrentPath, StringComparison.Ordinal);
        Assert.True(await events.IsOnPageAsync(), "The event log should open after the login");
    }

    [E2eFact(DisplayName = "Payload can be expanded")]
    public async Task EventPayloadCanBeExpanded()
    {
        var events = await SignInAsync();
        Assert.True(await events.EventCountAsync() > 0, "At least one event must exist before expanding a payload");

        await events.ExpandFirstEventPayloadAsync();

        Assert.False(string.IsNullOrWhiteSpace(await events.FirstEventPayloadTextAsync()), "Expanded payload should have content");
    }

    [E2eFact(DisplayName = "Payload starts with status and attempts")]
    public async Task PayloadOpensWithItsStatusAndAttemptsBeforeTheEventsContent()
    {
        var events = await SignInAsync();

        var lines = await events.CompletedPayloadLinesOfAsync("ProductCreatedEvent");

        Assert.Equal("Status: COMPLETED, attempts: 1", lines[0]);
        var content = string.Join('\n', lines.Skip(1));
        Assert.All(["\"eventId\"", "\"occurredOn\"", "\"productId\"", "\"amount\"", "\"currency\"", "\"initialStock\""],
            field => Assert.Contains(field, content, StringComparison.Ordinal));
    }

    [E2eFact(DisplayName = "The back office header offers the operators logout")]
    public async Task TheHeaderOfTheEventLogOffersTheOperatorsLogoutAndNothingOfAShopper()
    {
        await ClearCookiesAsync();
        await SignInAsync();

        var events = await BackofficeEventsPage.NavigateToAsync(Page);

        Assert.Equal("Back office", await events.HeaderLabelAsync());
        Assert.Equal("domaincentric.commerce", await events.LogoTextAsync());
        Assert.Equal("Logout", await events.HeaderOperatorLogoutAsync());
        Assert.False(await events.HeaderOffersLoginAsync(), "The back-office header should offer no shop login");
        Assert.False(await events.HeaderOffersRegisterAsync(), "The back-office header should offer no registration");
        Assert.False(await events.HeaderOffersCartAsync(), "The back-office header should offer no cart");
    }

    private async Task<BackofficeEventsPage> SignInAsync()
    {
        var login = await BackofficeLoginPage.NavigateToAsync(Page);
        await login.FillCredentialsAsync(AdminUsername, AdminPassword);
        return await login.SubmitAsync();
    }
}