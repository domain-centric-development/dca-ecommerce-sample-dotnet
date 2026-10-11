namespace DcaShop.Pricing.Application.SetProductPrice;

/// <summary>
/// Sets the price of a product — creating the record on first use, updating it afterwards. <paramref name="EventId"/>
/// is the id of the integration event that asked for it; a price already set for that id is not set again. Without
/// an event id the change is not deduplicated.
/// </summary>
public sealed record SetProductPriceCommand(Guid ProductId, decimal PriceAmount, string PriceCurrency, Guid? EventId = null);