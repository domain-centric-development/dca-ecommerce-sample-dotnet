using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.ChangeProfile;

public interface IChangeProfileInputPort : IUseCase<ChangeProfileCommand, ChangeProfileResult>
{
}
