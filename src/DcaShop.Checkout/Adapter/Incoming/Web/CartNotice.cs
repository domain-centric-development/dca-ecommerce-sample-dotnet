using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// The notices the checkout leaves for the cart page when it sends the customer back there. They are part of the cart
/// page's address, like its path: the codes and the key are the ones the cart page knows, and the cart page words the
/// sentences.
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

/// <summary>Each notice's code as the cart page knows it, and where the cart page finds it.</summary>
internal static class CartNotices
{
    /// <summary>The TempData key of the notice addressed to the cart page.</summary>
    private const string CartPage = "notice.cart";

    /// <summary>The TempData key of the number the cart page's notice names.</summary>
    private const string CartPageCount = CartPage + ".count";

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

    /// <summary>Leaves the notice's code — and the number it names — for the cart page the redirect leads to.</summary>
    public static void LeaveForTheCart(this ITempDataDictionary tempData, CartNotice notice, int? count = null)
    {
        tempData[CartPage] = notice.Code();
        if (count is { } number)
        {
            tempData[CartPageCount] = number;
        }
        else
        {
            tempData.Remove(CartPageCount);
        }
    }
}
