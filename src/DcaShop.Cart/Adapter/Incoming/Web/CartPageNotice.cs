using System.Globalization;

using Microsoft.AspNetCore.WebUtilities;

namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect carries to a cart page, as a fixed code in its address (<c>?notice=&lt;code&gt;</c>) —
/// never the sentence and never in a server session, so any instance that answers the next request shows it. A notice
/// that names a number carries it beside the code (<c>&amp;count=&lt;n&gt;</c>); the page words the sentence.
/// </summary>
/// <remarks>
/// The checkout sends the customer back to the cart with some of these codes; it knows them as the cart page's address,
/// the sentences stay here.
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

/// <summary>Each notice's code in the address, whether it reports a success, and the sentence its page shows.</summary>
internal static class CartPageNotices
{
    /// <summary>The query parameter that carries a notice's code.</summary>
    public const string Parameter = "notice";

    /// <summary>The query parameter that carries the number a notice names.</summary>
    public const string CountParameter = "count";

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
    /// The sentence the page shows; none for a notice that names a number the address did not carry.
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

    /// <summary>The notice an address names by its code; none for a code the shop does not know.</summary>
    public static CartPageNotice? FromCode(string? code) =>
        Enum.GetValues<CartPageNotice>().Cast<CartPageNotice?>().FirstOrDefault(notice => notice?.Code() == code);

    /// <summary>The address carrying the notice — and the number it names — after whatever query it has.</summary>
    public static string PathWith(string path, CartPageNotice notice, int? count = null)
    {
        var withNotice = QueryHelpers.AddQueryString(path, Parameter, notice.Code());
        return count is { } number
            ? QueryHelpers.AddQueryString(withNotice, CountParameter, number.ToString(CultureInfo.InvariantCulture))
            : withNotice;
    }
}