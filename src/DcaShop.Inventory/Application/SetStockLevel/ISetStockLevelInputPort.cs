using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.SetStockLevel;

/// <summary>Driving port: set the stock of a product to an absolute figure.</summary>
public interface ISetStockLevelInputPort : IUseCase<SetStockLevelCommand, SetStockLevelResult>
{
}
