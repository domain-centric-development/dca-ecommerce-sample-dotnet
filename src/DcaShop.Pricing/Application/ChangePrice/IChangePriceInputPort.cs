using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>Driving port: replace the current price of a product with a new one, effective now.</summary>
public interface IChangePriceInputPort : IUseCase<ChangePriceCommand, ChangePriceResult>
{
}