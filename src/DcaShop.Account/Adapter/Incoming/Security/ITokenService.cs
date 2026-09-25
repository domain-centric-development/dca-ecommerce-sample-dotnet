using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Incoming.Security;

/// <summary>
/// Mints the token an authenticated session is carried in.
/// </summary>
/// <remarks>
/// Adapter-internal mechanics, not a port: a token is a property of the protocol the incoming adapters speak (a
/// cookie for the browser, a Bearer header for API clients), and no use case knows or needs one. The interface
/// therefore lives with its callers in the adapter layer and carries no <c>IOutputPort</c> marker. The JWT
/// implementation is <c>JwtTokenService</c> in the security adapter. Reading the identity is different: that is
/// <see cref="SharedKernel.Application.Shared.IIdentityProvider"/>, an output port, because the caller's identity is
/// something the application needs from outside.
/// </remarks>
public interface ITokenService
{
    string GenerateRegisteredToken(UserId userId, string email, IReadOnlySet<string> roles);
}