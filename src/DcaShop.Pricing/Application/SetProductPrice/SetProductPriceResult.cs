namespace DcaShop.Pricing.Application.SetProductPrice;

public sealed record SetProductPriceResult(
    Guid PriceId,
    Guid ProductId,
    decimal PriceAmount,
    string PriceCurrency,
    DateTimeOffset EffectiveFrom,
    bool Created);
