using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Pricing.Application.SetProductPrice;

/// <summary>Driving port: set (create or change) the price of a product.</summary>
public interface ISetProductPriceInputPort : IUseCase<SetProductPriceCommand, SetProductPriceResult>
{
}
