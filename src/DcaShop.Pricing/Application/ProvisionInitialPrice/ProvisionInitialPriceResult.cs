namespace DcaShop.Pricing.Application.ProvisionInitialPrice;

/// <summary>The price record after the call; <paramref name="AlreadyApplied"/> when the event had set it before and nothing changed.</summary>
public sealed record ProvisionInitialPriceResult(
    Guid PriceId,
    Guid ProductId,
    decimal PriceAmount,
    string PriceCurrency,
    DateTimeOffset EffectiveFrom,
    bool Created,
    bool AlreadyApplied = false);