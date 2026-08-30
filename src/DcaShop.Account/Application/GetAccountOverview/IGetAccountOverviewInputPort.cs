using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.GetAccountOverview;

public interface IGetAccountOverviewInputPort : IUseCase<GetAccountOverviewQuery, GetAccountOverviewResult>
{
}
