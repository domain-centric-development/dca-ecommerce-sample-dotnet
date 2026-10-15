namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>Sets the price of the product <paramref name="ProductId"/> to <paramref name="PriceAmount"/> in <paramref name="PriceCurrency"/>.</summary>
public sealed record ChangePriceCommand(Guid ProductId, decimal PriceAmount, string PriceCurrency);