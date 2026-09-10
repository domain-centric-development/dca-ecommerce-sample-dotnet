namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

public sealed record CompleteCartCommand(Guid CartId, string SessionId, IReadOnlyList<string> PurchasedPositions);
