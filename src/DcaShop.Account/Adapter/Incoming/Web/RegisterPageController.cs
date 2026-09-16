using DcaShop.Account.Application.RegisterAccount;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the registration page. Unlike login it does not go through the cart merge page: the
/// visitor identity is preserved by registration, so no second cart can exist to merge with.
/// </summary>
[Route("register")]
public sealed class RegisterPageController : Controller
{
    private readonly IRegisterAccountInputPort _registerAccount;
    private readonly ITokenService _tokenService;
    private readonly IIdentityProvider _identityProvider;
    private readonly IIdentitySession _identitySession;

    public RegisterPageController(
        IRegisterAccountInputPort registerAccount,
        ITokenService tokenService,
        IIdentityProvider identityProvider,
        IIdentitySession identitySession)
    {
        _registerAccount = registerAccount;
        _tokenService = tokenService;
        _identityProvider = identityProvider;
        _identitySession = identitySession;
    }

    [HttpGet("")]
    public IActionResult Show([FromQuery] string? returnUrl) =>
        View("~/Views/Account/Register.cshtml", RegisterPageViewModel.Blank(returnUrl));

    [HttpPost("")]
    public async Task<IActionResult> Register(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string confirmPassword,
        [FromForm] string firstName,
        [FromForm] string lastName,
        [FromForm] string dateOfBirth,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var submission = new RegisterPageViewModel(email, firstName, lastName, dateOfBirth, returnUrl, null);

        if (password != confirmPassword)
        {
            return Rejected(submission, "Passwords do not match");
        }

        if (password.Length < 8)
        {
            return Rejected(submission, "Password must be at least 8 characters");
        }

        if (SubmittedDate.Parse(dateOfBirth) is not { } parsedDateOfBirth)
        {
            return Rejected(submission, SubmittedDate.NotADate);
        }

        try
        {
            var result = await _registerAccount.ExecuteAsync(
                new RegisterAccountCommand(
                    email,
                    password,
                    _identityProvider.GetCurrentIdentity().UserId.Value,
                    firstName,
                    lastName,
                    parsedDateOfBirth),
                cancellationToken);

            _identitySession.SetRegisteredIdentity(
                _tokenService.GenerateRegisteredToken(UserId.Of(result.UserId), result.Email, result.Roles));

            TempData["Message"] = "Account created successfully! Welcome!";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return Rejected(submission, e.Message);
        }
    }

    private IActionResult Rejected(RegisterPageViewModel submission, string error) =>
        View("~/Views/Account/Register.cshtml", submission with { ErrorMessage = error });
}
