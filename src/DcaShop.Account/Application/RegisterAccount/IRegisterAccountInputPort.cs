using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.RegisterAccount;

public interface IRegisterAccountInputPort : IUseCase<RegisterAccountCommand, RegisterAccountResult>
{
}
