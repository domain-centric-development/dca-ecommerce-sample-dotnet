namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// The notices the checkout hands the cart page when it sends the customer back there. They are part of the cart
/// page's address, like its path: the codes are the ones the cart page knows, and the cart page words the sentences.
/// </summary>
internal enum CartNotice
{
    NoActiveCheckout = 1,
    NoConfirmedOrder = 2,
    CartNotFound = 3,
    CartNotCheckable = 4,
    EmptyCart = 5,
    CartProductUnavailable = 6,
    ItemsUnavailable = 7,
    CheckoutNotStarted = 8,
}

/// <summary>Each notice's code in the cart page's address.</summary>
internal static class CartNotices
{
    public static string Code(this CartNotice notice) => notice switch
    {
        CartNotice.NoActiveCheckout => "no-active-checkout",
        CartNotice.NoConfirmedOrder => "no-confirmed-order",
        CartNotice.CartNotFound => "cart-not-found",
        CartNotice.CartNotCheckable => "cart-not-checkable",
        CartNotice.EmptyCart => "empty-cart",
        CartNotice.CartProductUnavailable => "cart-product-unavailable",
        CartNotice.ItemsUnavailable => "items-unavailable",
        CartNotice.CheckoutNotStarted => "checkout-not-started",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };
}