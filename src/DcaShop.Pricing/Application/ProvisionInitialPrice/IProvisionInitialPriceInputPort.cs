using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Pricing.Application.ProvisionInitialPrice;

/// <summary>Driving port: provision the initial price of a product (creating the record, or changing one already there).</summary>
public interface IProvisionInitialPriceInputPort : IUseCase<ProvisionInitialPriceCommand, ProvisionInitialPriceResult>
{
}