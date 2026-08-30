using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.AuthenticateAccount;

public interface IAuthenticateAccountInputPort : IUseCase<AuthenticateAccountCommand, AuthenticateAccountResult>
{
}
