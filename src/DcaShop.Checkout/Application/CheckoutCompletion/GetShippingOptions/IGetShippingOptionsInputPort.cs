using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.GetShippingOptions;

public interface IGetShippingOptionsInputPort : IUseCase<GetShippingOptionsQuery, GetShippingOptionsResult>
{
}
