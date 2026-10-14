namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect carries to a checkout page, as a fixed code in its address (<c>?notice=&lt;code&gt;</c>) —
/// never the sentence and never in a server session, so any instance that answers the next request shows it. A notice
/// that names a number carries it beside the code (<c>&amp;count=&lt;n&gt;</c>); the page words the sentence.
/// </summary>
internal enum CheckoutStepNotice
{
    /// <summary>A step was sent before the earlier steps were done.</summary>
    EarlierStepsFirst = 0,

    /// <summary>"Place Order" found no checkout session.</summary>
    SessionNotFound = 1,

    /// <summary>"Place Order" met a checkout that can no longer be changed.</summary>
    CheckoutNotModifiable = 2,

    /// <summary>"Place Order" found items unavailable or short in stock; names how many.</summary>
    ItemsUnavailable = 3,

    /// <summary>"Place Order" was sent before the order was reviewed.</summary>
    ReviewFirst = 4,

    /// <summary>"Place Order" was refused for any other reason.</summary>
    OrderNotPlaced = 5,
}

/// <summary>Each notice's code in the address and the sentence its page shows.</summary>
internal static class CheckoutStepNotices
{
    /// <summary>The query parameter that carries the number a notice names.</summary>
    public const string CountParameter = "count";

    public static string Code(this CheckoutStepNotice notice) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "earlier-steps-first",
        CheckoutStepNotice.SessionNotFound => "session-not-found",
        CheckoutStepNotice.CheckoutNotModifiable => "checkout-not-modifiable",
        CheckoutStepNotice.ItemsUnavailable => "items-unavailable",
        CheckoutStepNotice.ReviewFirst => "review-first",
        CheckoutStepNotice.OrderNotPlaced => "order-not-placed",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The sentence the page shows; none for a notice that names a number the address did not carry.</summary>
    public static string? Sentence(this CheckoutStepNotice notice, int? count = null) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "Please complete the previous checkout steps first.",
        CheckoutStepNotice.SessionNotFound => "Checkout session not found",
        CheckoutStepNotice.CheckoutNotModifiable => "This checkout can no longer be changed.",
        CheckoutStepNotice.ItemsUnavailable => count is { } unavailable
            ? $"Cannot place the order, {unavailable} item(s) unavailable or out of stock."
            : null,
        CheckoutStepNotice.ReviewFirst => "Please review your order before placing it.",
        CheckoutStepNotice.OrderNotPlaced => "Your order could not be placed. Please try again.",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The notice an address names by its code; none for a code the shop does not know.</summary>
    public static CheckoutStepNotice? FromCode(string? code) =>
        Enum.GetValues<CheckoutStepNotice>().Cast<CheckoutStepNotice?>().FirstOrDefault(notice => notice?.Code() == code);
}