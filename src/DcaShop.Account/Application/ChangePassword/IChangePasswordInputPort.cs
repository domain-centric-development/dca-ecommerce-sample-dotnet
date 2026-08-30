using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.ChangePassword;

public interface IChangePasswordInputPort : IUseCase<ChangePasswordCommand, ChangePasswordResult>
{
}
