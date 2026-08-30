using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>Driving port: reduce the stock of a product.</summary>
public interface IReduceStockInputPort : IUseCase<ReduceStockCommand, ReduceStockResult>
{
}
