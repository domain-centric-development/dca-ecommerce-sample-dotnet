using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Event;

/// <summary>A product got its first stock record.</summary>
public sealed record StockLevelCreated(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity InitialQuantity) : IDomainEvent
{
    public static StockLevelCreated Now(StockLevelId stockLevelId, ProductId productId, StockQuantity initialQuantity) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, initialQuantity);
}

/// <summary>Stock was added.</summary>
public sealed record StockIncreased(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity AddedQuantity,
    StockQuantity AvailableQuantity) : IDomainEvent
{
    public static StockIncreased Now(StockLevelId stockLevelId, ProductId productId, StockQuantity added, StockQuantity available) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, added, available);
}

/// <summary>Stock was taken out — the counterpart of a shipped or sold item.</summary>
public sealed record StockDecreased(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity RemovedQuantity,
    StockQuantity AvailableQuantity) : IDomainEvent
{
    public static StockDecreased Now(StockLevelId stockLevelId, ProductId productId, StockQuantity removed, StockQuantity available) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, removed, available);
}

/// <summary>Stock was corrected to an absolute figure (stocktaking).</summary>
public sealed record StockChanged(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity PreviousAvailableQuantity,
    StockQuantity AvailableQuantity,
    StockQuantity PreviousReservedQuantity,
    StockQuantity ReservedQuantity) : IDomainEvent
{
    public static StockChanged Now(
        StockLevelId stockLevelId,
        ProductId productId,
        StockQuantity previousAvailable,
        StockQuantity available,
        StockQuantity previousReserved,
        StockQuantity reserved) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, previousAvailable, available, previousReserved, reserved);
}

/// <summary>Stock was set aside for a specific purpose.</summary>
public sealed record StockReserved(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity ReservedQuantity) : IDomainEvent
{
    public static StockReserved Now(StockLevelId stockLevelId, ProductId productId, StockQuantity reserved) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, reserved);
}

/// <summary>A reservation was given up again.</summary>
public sealed record StockReleased(
    Guid EventId,
    DateTimeOffset OccurredOn,
    StockLevelId StockLevelId,
    ProductId ProductId,
    StockQuantity ReleasedQuantity) : IDomainEvent
{
    public static StockReleased Now(StockLevelId stockLevelId, ProductId productId, StockQuantity released) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, stockLevelId, productId, released);
}
