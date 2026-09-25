using DcaShop.Backoffice.Adapter.Incoming.Web;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DcaShop.Backoffice.Infrastructure;

/// <summary>
/// Refuses to start the shop with the operator credentials and the cookie policy the sample ships for convenience.
/// </summary>
/// <remarks>
/// The backoffice replays failed event publications, so its login is the most privileged door in the shop, and
/// <c>admin</c>/<c>admin</c> is written down in this repository. The check runs at startup for the same reason as
/// its twin on the token settings: a default that is merely documented as dev-only is a default that ships.
/// </remarks>
public sealed class BackofficeDevelopmentDefaultsValidator : IValidateOptions<BackofficeOptions>
{
    private readonly IHostEnvironment _environment;

    public BackofficeDevelopmentDefaultsValidator(IHostEnvironment environment) => _environment = environment;

    public ValidateOptionsResult Validate(string? name, BackofficeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.Username == BackofficeOptions.DevelopmentUsername
            && options.Password == BackofficeOptions.DevelopmentPassword)
        {
            failures.Add(
                "The backoffice still uses the operator credentials committed to this repository. Set "
                + "Backoffice__Username and Backoffice__Password, or run with "
                + "ASPNETCORE_ENVIRONMENT=Development.");
        }

        if (!options.SecureCookies)
        {
            failures.Add(
                "The operator session cookie is not flagged Secure, so a browser sends it over plain HTTP. Set "
                + "Backoffice__SecureCookies=true, or run with ASPNETCORE_ENVIRONMENT=Development.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}