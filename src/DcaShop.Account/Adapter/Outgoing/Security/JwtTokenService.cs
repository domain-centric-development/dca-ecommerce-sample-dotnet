using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// Mints and reads the HS256 tokens the two cookies carry. It reports an <i>expired</i> token separately from an
/// <i>unreadable</i> one (ADR-029): the first is the routine end of a session, the second is an attack or a bug,
/// and collapsing both into "no identity" erases that distinction at the boundary.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private const string ClaimType = "type";
    private const string ClaimEmail = "email";
    private const string ClaimRoles = "roles";
    private const string TypeAnonymous = "anonymous";
    private const string TypeRegistered = "registered";

    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _options.Validate();
        _logger = logger;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = key,
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    /// <summary>Mints the token that carries the visitor identity — no claims beyond who the browser is.</summary>
    public string GenerateAnonymousToken(UserId userId) =>
        Write([new Claim(ClaimType, TypeAnonymous)], userId, _options.IdentityLifetime);

    public string GenerateRegisteredToken(UserId userId, string email, IReadOnlySet<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var claims = new List<Claim> { new(ClaimType, TypeRegistered), new(ClaimEmail, email) };
        claims.AddRange(roles.Select(role => new Claim(ClaimRoles, role)));
        return Write(claims, userId, _options.SessionLifetime);
    }

    /// <summary>What reading a token produced.</summary>
    public abstract record TokenValidation
    {
        private TokenValidation()
        {
        }

        /// <summary>The token verified and named an identity.</summary>
        public sealed record Valid(IIdentityProvider.IIdentity Identity) : TokenValidation;

        /// <summary>The token verified but has aged out. Routine.</summary>
        public sealed record Expired : TokenValidation;

        /// <summary>The token did not verify, or its claims make no sense. Worth a warning.</summary>
        public sealed record Unreadable(string Reason) : TokenValidation;
    }

    /// <summary>Verifies a token and reads the identity out of it.</summary>
    public TokenValidation Validate(string token)
    {
        try
        {
            _handler.ValidateToken(token, _validationParameters, out var validated);
            return new TokenValidation.Valid(IdentityFrom((JwtSecurityToken)validated));
        }
        catch (SecurityTokenExpiredException e)
        {
            _logger.LogDebug("JWT token expired: {Reason}", e.Message);
            return new TokenValidation.Expired();
        }
        catch (Exception e) when (e is SecurityTokenException or ArgumentException)
        {
            _logger.LogWarning("Unreadable JWT token: {Reason}", e.Message);
            return new TokenValidation.Unreadable(e.Message);
        }
    }

    /// <summary>The identity a token names, or <see langword="null"/> when it cannot be read.</summary>
    public IIdentityProvider.IIdentity? ValidateAndParse(string token) =>
        Validate(token) is TokenValidation.Valid valid ? valid.Identity : null;

    private string Write(IEnumerable<Claim> claims, UserId userId, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims.Append(new Claim(JwtRegisteredClaimNames.Sub, userId.Value)),
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: _signingCredentials);
        return _handler.WriteToken(token);
    }

    private static IIdentityProvider.IIdentity IdentityFrom(JwtSecurityToken token)
    {
        var subject = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("JWT subject claim is missing", nameof(token));
        }

        var userId = UserId.Of(subject);
        if (token.Claims.FirstOrDefault(c => c.Type == ClaimType)?.Value != TypeRegistered)
        {
            // Anything that is not explicitly registered is anonymous — including a legacy identity cookie whose
            // token still carries registered claims. Honouring those would let the identity cookie grant
            // authentication, which is the conflation ADR-030 removes.
            return JwtIdentity.Anonymous(userId);
        }

        var email = token.Claims.FirstOrDefault(c => c.Type == ClaimEmail)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Registered JWT missing email claim", nameof(token));
        }

        var roles = token.Claims.Where(c => c.Type == ClaimRoles).Select(c => c.Value).ToHashSet();
        return JwtIdentity.Registered(userId, email, roles);
    }
}
