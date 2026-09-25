using DcaShop.Account.Adapter.Outgoing.Security;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DcaShop.Account.Infrastructure;

/// <summary>
/// Refuses to start the shop with the token and cookie settings the sample ships for convenience.
/// </summary>
/// <remarks>
/// A committed secret is a published secret: anyone reading this repository can mint a token this shop accepts.
/// Cookies without the <c>Secure</c> flag travel over plain HTTP, where the token is readable in transit. Both are
/// fine while the shop runs on a laptop and unacceptable anywhere else, and neither announces itself — the
/// application starts and behaves normally.
/// <para>
/// So the check is made at startup, against the hosting environment rather than against a hostname: a deployment
/// that does not say <c>Development</c> says it is not the laptop, and from that moment the development values are
/// refused rather than merely discouraged. The message names the environment variable that supplies a real value,
/// because a fail-fast the operator cannot act on is only an outage. The Java twin reads the active Spring profile
/// for the same decision.
/// </para>
/// </remarks>
public sealed class JwtDevelopmentDefaultsValidator : IValidateOptions<JwtOptions>
{
    private readonly IHostEnvironment _environment;

    public JwtDevelopmentDefaultsValidator(IHostEnvironment environment) => _environment = environment;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.Secret == JwtOptions.DevelopmentSecret)
        {
            failures.Add(
                "The JWT signing secret is the one committed to this repository, so it is public and anyone can "
                + "mint a token this shop accepts. Set Jwt__Secret to a secret of at least 32 characters, or run "
                + "with ASPNETCORE_ENVIRONMENT=Development.");
        }

        if (!options.SecureCookies)
        {
            failures.Add(
                "Identity and session cookies are not flagged Secure, so a browser sends them over plain HTTP. "
                + "Set Jwt__SecureCookies=true, or run with ASPNETCORE_ENVIRONMENT=Development.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}