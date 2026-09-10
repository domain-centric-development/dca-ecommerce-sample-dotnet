
namespace DcaShop.Pricing.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Pricing creates a price record when an integration event
/// carrying this shape arrives. The catalog's <c>ProductCreatedEvent</c> implements it; Pricing never depends
/// on the Product context.
/// </summary>
public interface IPriceInitializationTrigger
{
    Guid ProductId { get; }

    string Amount { get; }

    string Currency { get; }
}
