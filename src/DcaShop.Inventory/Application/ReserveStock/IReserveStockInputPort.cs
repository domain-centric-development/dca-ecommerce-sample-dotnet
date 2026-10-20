using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Inventory.Application.ReserveStock;

/// <summary>Driving port: reserve the stock of one order, all lines or none.</summary>
public interface IReserveStockInputPort : IUseCase<ReserveStockCommand, ReserveStockResult>
{
}