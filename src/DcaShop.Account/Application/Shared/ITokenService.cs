using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Account.Application.Shared;

/// <summary>Mints the token an authenticated session is carried in.</summary>
public interface ITokenService : IOutputPort
{
    string GenerateRegisteredToken(UserId userId, string email, IReadOnlySet<string> roles);
}
