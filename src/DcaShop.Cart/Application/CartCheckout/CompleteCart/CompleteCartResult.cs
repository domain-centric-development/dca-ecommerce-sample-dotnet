namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

/// <summary>The cart after the completion; <paramref name="AlreadyCompleted"/> when the event had produced it before.</summary>
public sealed record CompleteCartResult(Guid CartId, string Status, bool AlreadyCompleted = false);