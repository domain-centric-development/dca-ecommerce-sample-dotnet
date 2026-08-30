using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Backoffice.Application.GetEventPublications;

public interface IGetEventPublicationsInputPort : IUseCase<GetEventPublicationsQuery, GetEventPublicationsResult>
{
}
