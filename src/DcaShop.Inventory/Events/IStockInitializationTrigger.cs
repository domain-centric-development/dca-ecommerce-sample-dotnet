
namespace DcaShop.Inventory.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Inventory creates a stock record when an integration event
/// carrying this shape arrives. The catalog's <c>ProductCreatedEvent</c> implements it; Inventory never depends
/// on the Product context.
/// </summary>
public interface IStockInitializationTrigger
{
    Guid ProductId { get; }

    int InitialStock { get; }
}
