namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

/// <summary>
/// Reconciles the cart with a confirmed checkout session. <paramref name="EventId"/> is the id of the integration
/// event that asked for it; a completion already produced for that id is not produced again. Without an event id
/// the completion is not deduplicated.
/// </summary>
public sealed record CompleteCartCommand(Guid CartId, string SessionId, IReadOnlyList<string> PurchasedPositions, Guid? EventId = null);