namespace DcaShop.Cart.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): the cart completes when an event carrying this shape arrives.
/// Checkout's <c>CheckoutConfirmedEvent</c> implements it; the cart never depends on Checkout.
/// </summary>
public interface ICartCompletionTrigger
{
    string CartId { get; }
}
