namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>The price a product has after the change, and the moment it took effect.</summary>
public sealed record ChangePriceResult(Guid ProductId, decimal PriceAmount, string PriceCurrency, DateTimeOffset EffectiveFrom);