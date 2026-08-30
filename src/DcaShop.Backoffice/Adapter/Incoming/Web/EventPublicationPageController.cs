using System.Security.Claims;
using DcaShop.Backoffice.Application.GetEventPublications;
using DcaShop.Backoffice.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DcaShop.Backoffice.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the backoffice: its login form and the event-publication log.
/// </summary>
/// <remarks>
/// Authentication here is the module's own (<see cref="BackofficeOptions.AuthenticationScheme"/>) and has nothing
/// to do with the shop's session: an operator signs in with an operator credential, into an operator cookie.
/// Every page but the login form requires it.
/// </remarks>
[Route("backoffice")]
[Authorize(AuthenticationSchemes = BackofficeOptions.AuthenticationScheme)]
public sealed class EventPublicationPageController : Controller
{
    private readonly IGetEventPublicationsInputPort _getEventPublications;
    private readonly BackofficeOptions _options;

    public EventPublicationPageController(
        IGetEventPublicationsInputPort getEventPublications, IOptions<BackofficeOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _getEventPublications = getEventPublications;
        _options = options.Value;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult ShowLogin([FromQuery] string? error, [FromQuery] string? logout) =>
        View("~/Views/Backoffice/Login.cshtml", new BackofficeLoginViewModel(error is not null, logout is not null));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
    {
        if (!CredentialsMatch(username, password))
        {
            return Redirect("/backoffice/login?error=true");
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, _options.Username)], BackofficeOptions.AuthenticationScheme);

        await HttpContext.SignInAsync(
            BackofficeOptions.AuthenticationScheme, new ClaimsPrincipal(identity)).ConfigureAwait(false);

        return Redirect("/backoffice/events");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(BackofficeOptions.AuthenticationScheme).ConfigureAwait(false);
        return Redirect("/backoffice/login?logout=true");
    }

    [HttpGet("events")]
    public async Task<IActionResult> ShowEventLog(CancellationToken cancellationToken)
    {
        var result = await _getEventPublications.ExecuteAsync(new GetEventPublicationsQuery(), cancellationToken);
        return View("~/Views/Backoffice/Events.cshtml", EventPublicationPageViewModel.From(result));
    }

    /// <summary>
    /// Compares both halves in fixed time, so a wrong username and a wrong password cost the same — the
    /// credential pair is the whole authentication here, and timing is the only side channel it has.
    /// </summary>
    private bool CredentialsMatch(string? username, string? password) =>
        FixedTimeEquals(username, _options.Username) & FixedTimeEquals(password, _options.Password);

    private static bool FixedTimeEquals(string? candidate, string expected) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(candidate ?? string.Empty),
            System.Text.Encoding.UTF8.GetBytes(expected));
}
