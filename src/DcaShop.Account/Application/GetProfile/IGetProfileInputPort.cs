using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.GetProfile;

public interface IGetProfileInputPort : IUseCase<GetProfileQuery, GetProfileResult>
{
}
