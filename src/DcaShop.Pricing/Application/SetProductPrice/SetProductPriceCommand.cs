namespace DcaShop.Pricing.Application.SetProductPrice;

/// <summary>Sets the price of a product — creating the record on first use, updating it afterwards.</summary>
public sealed record SetProductPriceCommand(Guid ProductId, decimal PriceAmount, string PriceCurrency);
