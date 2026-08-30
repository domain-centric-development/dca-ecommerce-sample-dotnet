using DcaShop.Inventory.Domain.Event;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>
/// Available and reserved stock of one product. Aggregate root of the Inventory context: quantities never go
/// negative, a reservation never exceeds the unreserved stock, and every movement is recorded as a domain event.
/// </summary>
public sealed class StockLevel : AggregateRootBase<StockLevel, StockLevelId>
{
    private StockLevel(StockLevelId id, ProductId productId, StockQuantity availableQuantity, StockQuantity reservedQuantity)
    {
        Id = id;
        ProductId = productId;
        AvailableQuantity = availableQuantity;
        ReservedQuantity = reservedQuantity;
    }

    public override StockLevelId Id { get; }

    public ProductId ProductId { get; }

    public StockQuantity AvailableQuantity { get; private set; }

    public StockQuantity ReservedQuantity { get; private set; }

    /// <summary>True while there is stock that nobody has reserved yet.</summary>
    public bool IsAvailable => AvailableQuantity.Value > ReservedQuantity.Value;

    public static StockLevel Create(ProductId productId, int initialQuantity)
    {
        var stockLevel = new StockLevel(StockLevelId.Generate(), productId, StockQuantity.Of(initialQuantity), StockQuantity.Of(0));
        stockLevel.RegisterEvent(StockLevelCreated.Now(stockLevel.Id, productId, stockLevel.AvailableQuantity));
        return stockLevel;
    }

    public void IncreaseStock(int amount)
    {
        RequireNotNegative(amount);
        AvailableQuantity = StockQuantity.Of(AvailableQuantity.Value + amount);
        RegisterEvent(StockIncreased.Now(Id, ProductId, StockQuantity.Of(amount), AvailableQuantity));
    }

    public void DecreaseStock(int amount)
    {
        RequireNotNegative(amount);
        if (amount > AvailableQuantity.Value)
        {
            throw new InvalidOperationException($"Cannot decrease stock by {amount}, only {AvailableQuantity.Value} available");
        }

        AvailableQuantity = StockQuantity.Of(AvailableQuantity.Value - amount);
        if (ReservedQuantity.Value > AvailableQuantity.Value)
        {
            ReservedQuantity = AvailableQuantity;
        }

        RegisterEvent(StockDecreased.Now(Id, ProductId, StockQuantity.Of(amount), AvailableQuantity));
    }

    public void Reserve(int amount)
    {
        RequireNotNegative(amount);
        var unreserved = AvailableQuantity.Value - ReservedQuantity.Value;
        if (amount > unreserved)
        {
            throw new InvalidOperationException($"Cannot reserve {amount}, only {unreserved} unreserved stock available");
        }

        ReservedQuantity = StockQuantity.Of(ReservedQuantity.Value + amount);
        RegisterEvent(StockReserved.Now(Id, ProductId, StockQuantity.Of(amount)));
    }

    public void Release(int amount)
    {
        RequireNotNegative(amount);
        if (amount > ReservedQuantity.Value)
        {
            throw new InvalidOperationException($"Cannot release {amount}, only {ReservedQuantity.Value} reserved");
        }

        ReservedQuantity = StockQuantity.Of(ReservedQuantity.Value - amount);
        RegisterEvent(StockReleased.Now(Id, ProductId, StockQuantity.Of(amount)));
    }

    /// <summary>Corrects the stock to an absolute figure (stocktaking), capping reservations at the new amount.</summary>
    public void AdjustStockTo(int quantity)
    {
        RequireNotNegative(quantity);
        var previousAvailable = AvailableQuantity;
        var previousReserved = ReservedQuantity;
        AvailableQuantity = StockQuantity.Of(quantity);
        if (ReservedQuantity.Value > AvailableQuantity.Value)
        {
            ReservedQuantity = AvailableQuantity;
        }

        RegisterEvent(StockChanged.Now(Id, ProductId, previousAvailable, AvailableQuantity, previousReserved, ReservedQuantity));
    }

    private static void RequireNotNegative(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }
    }
}
