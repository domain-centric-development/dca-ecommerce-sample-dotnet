using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect leaves for a cart page. A notice travels in TempData — a cookie sealed with the key ring every
/// instance shares — as its fixed code, never the sentence and never in a server session, so any instance that answers
/// the next request shows it. A notice that names a number carries it beside the code; the page words the sentence. It
/// is addressed to one page: only that page reads it, and reading removes it.
/// </summary>
/// <remarks>
/// The checkout sends the customer back to the cart with some of these codes; it knows them, and the cart page's key,
/// as part of the cart page's address — the sentences stay here.
/// </remarks>
internal enum CartPageNotice
{
    ProductAdded = 1,
    NotEnoughStock = 2,
    ProductUnavailable = 3,
    CartNotFound = 4,
    CartNotModifiable = 5,
    InvalidQuantity = 6,
    ProductNotAdded = 7,
    WelcomeBack = 8,
    CartsMerged = 9,
    AccountCartUsed = 10,
    RecentCartUsed = 11,
    InvalidMergeOption = 12,
    NoActiveCheckout = 13,
    NoConfirmedOrder = 14,
    CartNotCheckable = 15,
    EmptyCart = 16,
    CartProductUnavailable = 17,
    ItemsUnavailable = 18,
    CheckoutNotStarted = 19,
}

/// <summary>
/// Each notice's code, whether it reports a success, the sentence its page shows, and where a page finds the notice
/// left for it.
/// </summary>
internal static class CartPageNotices
{
    /// <summary>The TempData key of the notice addressed to the cart page.</summary>
    public const string CartPage = "notice.cart";

    /// <summary>The TempData key of the notice addressed to the cart merge page.</summary>
    public const string MergePage = "notice.cart.merge";

    /// <summary>Beside a page's key, the key of the number its notice names.</summary>
    private const string CountSuffix = ".count";

    public static string Code(this CartPageNotice notice) => notice switch
    {
        CartPageNotice.ProductAdded => "product-added",
        CartPageNotice.NotEnoughStock => "not-enough-stock",
        CartPageNotice.ProductUnavailable => "product-unavailable",
        CartPageNotice.CartNotFound => "cart-not-found",
        CartPageNotice.CartNotModifiable => "cart-not-modifiable",
        CartPageNotice.InvalidQuantity => "invalid-quantity",
        CartPageNotice.ProductNotAdded => "product-not-added",
        CartPageNotice.WelcomeBack => "welcome-back",
        CartPageNotice.CartsMerged => "carts-merged",
        CartPageNotice.AccountCartUsed => "account-cart-used",
        CartPageNotice.RecentCartUsed => "recent-cart-used",
        CartPageNotice.InvalidMergeOption => "invalid-merge-option",
        CartPageNotice.NoActiveCheckout => "no-active-checkout",
        CartPageNotice.NoConfirmedOrder => "no-confirmed-order",
        CartPageNotice.CartNotCheckable => "cart-not-checkable",
        CartPageNotice.EmptyCart => "empty-cart",
        CartPageNotice.CartProductUnavailable => "cart-product-unavailable",
        CartPageNotice.ItemsUnavailable => "items-unavailable",
        CartPageNotice.CheckoutNotStarted => "checkout-not-started",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>Whether the page shows the notice as a success rather than as an error.</summary>
    public static bool IsSuccess(this CartPageNotice notice) => notice is CartPageNotice.ProductAdded
        or CartPageNotice.WelcomeBack or CartPageNotice.CartsMerged or CartPageNotice.AccountCartUsed
        or CartPageNotice.RecentCartUsed;

    /// <summary>
    /// The sentence the page shows; none for a notice that names a number it was not left with.
    /// </summary>
    public static string? Sentence(this CartPageNotice notice, int? count = null) => notice switch
    {
        CartPageNotice.ProductAdded => "Product added to cart!",
        CartPageNotice.NotEnoughStock => count is { } requested
            ? $"There is not enough stock to add {requested} of this product."
            : null,
        CartPageNotice.ProductUnavailable => "This product is no longer available.",
        CartPageNotice.CartNotFound => "Your cart could not be found.",
        CartPageNotice.CartNotModifiable => "This cart can no longer be changed.",
        CartPageNotice.InvalidQuantity => "Please enter a valid quantity.",
        CartPageNotice.ProductNotAdded => "The product could not be added to the cart. Please try again.",
        CartPageNotice.WelcomeBack => "Welcome back!",
        CartPageNotice.CartsMerged => "Carts merged successfully!",
        CartPageNotice.AccountCartUsed => "Using your account cart.",
        CartPageNotice.RecentCartUsed => "Using your recent cart.",
        CartPageNotice.InvalidMergeOption => "Invalid merge option selected",
        CartPageNotice.NoActiveCheckout => "No active checkout session found",
        CartPageNotice.NoConfirmedOrder => "No confirmed order found",
        CartPageNotice.CartNotCheckable => "This cart can no longer be checked out.",
        CartPageNotice.EmptyCart => "Cannot check out an empty cart.",
        CartPageNotice.CartProductUnavailable => "A product in your cart is no longer available.",
        CartPageNotice.ItemsUnavailable => count is { } unavailable
            ? $"Cannot start checkout, {unavailable} item(s) unavailable or out of stock."
            : null,
        CartPageNotice.CheckoutNotStarted => "The checkout could not be started. Please try again.",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The notice a code names; none for a code the shop does not know.</summary>
    public static CartPageNotice? FromCode(string? code) =>
        Enum.GetValues<CartPageNotice>().Cast<CartPageNotice?>().FirstOrDefault(notice => notice?.Code() == code);

    /// <summary>Leaves the notice's code — and the number it names — for the page the redirect leads to.</summary>
    public static void Leave(this ITempDataDictionary tempData, string page, CartPageNotice notice, int? count = null)
    {
        tempData[page] = notice.Code();
        if (count is { } number)
        {
            tempData[page + CountSuffix] = number;
        }
        else
        {
            tempData.Remove(page + CountSuffix);
        }
    }

    /// <summary>The notice left for this page and the number it names, if any — reading them removes them.</summary>
    public static (CartPageNotice? Notice, int? Count) TakeCartPageNotice(this ITempDataDictionary tempData, string page) =>
        (FromCode(tempData[page] as string), tempData[page + CountSuffix] as int?);
}
