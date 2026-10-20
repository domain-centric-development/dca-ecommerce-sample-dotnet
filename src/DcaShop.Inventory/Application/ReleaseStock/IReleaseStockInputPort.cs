using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.ReleaseStock;

/// <summary>Driving port: give back the stock an order reserved.</summary>
public interface IReleaseStockInputPort : IUseCase<ReleaseStockCommand, ReleaseStockResult>
{
}