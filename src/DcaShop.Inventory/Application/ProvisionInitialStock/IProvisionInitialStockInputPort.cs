using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.ProvisionInitialStock;

/// <summary>Driving port: provision the initial stock of a product, as an absolute figure.</summary>
public interface IProvisionInitialStockInputPort : IUseCase<ProvisionInitialStockCommand, ProvisionInitialStockResult>
{
}