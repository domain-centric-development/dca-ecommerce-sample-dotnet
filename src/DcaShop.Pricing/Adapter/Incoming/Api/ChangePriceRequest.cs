namespace DcaShop.Pricing.Adapter.Incoming.Api;

/// <summary>The body of a price change: the new price, in euros.</summary>
public sealed record ChangePriceRequest(decimal Price);