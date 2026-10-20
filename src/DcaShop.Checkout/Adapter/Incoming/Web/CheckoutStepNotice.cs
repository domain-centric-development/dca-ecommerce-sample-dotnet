using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect leaves for a checkout step's page. A notice travels in TempData — a cookie sealed with the key
/// ring every instance shares — as its fixed code, never the sentence and never in a server session, so any instance
/// that answers the next request shows it. A notice that names a number carries it beside the code; the page words the
/// sentence. It is addressed to one step: only that step's page reads it, and reading removes it.
/// </summary>
internal enum CheckoutStepNotice
{
    /// <summary>A step was sent before the earlier steps were done.</summary>
    EarlierStepsFirst = 0,

    /// <summary>"Place Order" found no checkout session.</summary>
    SessionNotFound = 1,

    /// <summary>"Place Order" met a checkout that can no longer be changed.</summary>
    CheckoutNotModifiable = 2,

    /// <summary>"Place Order" found items whose prices changed; names how many.</summary>
    PricesChanged = 3,

    /// <summary>"Place Order" was sent before the order was reviewed.</summary>
    ReviewFirst = 4,

    /// <summary>"Place Order" was refused for any other reason.</summary>
    OrderNotPlaced = 5,

    /// <summary>Paying again found the payment already authorized for the total; informs, refuses nothing.</summary>
    PaymentAlreadyAuthorized = 6,

    /// <summary>"Place Order" found a line the stock no longer covers; names its product.</summary>
    StockRanShort = 7,
}

/// <summary>Each notice's code, the sentence its page shows, and where a step's page finds the notice left for it.</summary>
internal static class CheckoutStepNotices
{
    /// <summary>Beside a step's key, the key of the number its notice names.</summary>
    private const string CountSuffix = ".count";

    /// <summary>Beside a step's key, the key of the product its notice names.</summary>
    private const string ProductSuffix = ".product";

    public static string Code(this CheckoutStepNotice notice) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "earlier-steps-first",
        CheckoutStepNotice.SessionNotFound => "session-not-found",
        CheckoutStepNotice.CheckoutNotModifiable => "checkout-not-modifiable",
        CheckoutStepNotice.PricesChanged => "prices-changed",
        CheckoutStepNotice.ReviewFirst => "review-first",
        CheckoutStepNotice.OrderNotPlaced => "order-not-placed",
        CheckoutStepNotice.PaymentAlreadyAuthorized => "payment-already-authorized",
        CheckoutStepNotice.StockRanShort => "stock-ran-short",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The sentence the page shows; none for a notice that names a number or a product it was not left with.</summary>
    public static string? Sentence(this CheckoutStepNotice notice, int? count = null, string? product = null) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "Please complete the previous checkout steps first.",
        CheckoutStepNotice.SessionNotFound => "Checkout session not found",
        CheckoutStepNotice.CheckoutNotModifiable => "This checkout can no longer be changed.",
        CheckoutStepNotice.PricesChanged => count is { } changed
            ? $"Cannot place the order, {changed} item(s) unavailable or out of stock."
            : null,
        CheckoutStepNotice.ReviewFirst => "Please review your order before placing it.",
        CheckoutStepNotice.OrderNotPlaced => "Your order could not be placed. Please try again.",
        CheckoutStepNotice.PaymentAlreadyAuthorized => "Your payment is already authorized.",
        CheckoutStepNotice.StockRanShort => product is { } name
            ? $"Not enough stock left for '{name}'. Please adjust your order."
            : null,
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The notice a code names; none for a code the shop does not know.</summary>
    public static CheckoutStepNotice? FromCode(string? code) =>
        Enum.GetValues<CheckoutStepNotice>().Cast<CheckoutStepNotice?>().FirstOrDefault(notice => notice?.Code() == code);

    /// <summary>
    /// Leaves the notice's code — and the number or the product it names — for the step's page the redirect leads to.
    /// </summary>
    public static void Leave(this ITempDataDictionary tempData, CheckoutStep step, CheckoutStepNotice notice, int? count = null, string? product = null)
    {
        tempData[KeyOf(step)] = notice.Code();
        if (count is { } number)
        {
            tempData[KeyOf(step) + CountSuffix] = number;
        }
        else
        {
            tempData.Remove(KeyOf(step) + CountSuffix);
        }

        if (product is { } name)
        {
            tempData[KeyOf(step) + ProductSuffix] = name;
        }
        else
        {
            tempData.Remove(KeyOf(step) + ProductSuffix);
        }
    }

    /// <summary>
    /// The notice left for the step's page and the number or the product it names, if any — reading them removes them.
    /// </summary>
    public static (CheckoutStepNotice? Notice, int? Count, string? Product) TakeCheckoutStepNotice(this ITempDataDictionary tempData, CheckoutStep step) =>
        (FromCode(tempData[KeyOf(step)] as string), tempData[KeyOf(step) + CountSuffix] as int?, tempData[KeyOf(step) + ProductSuffix] as string);

    /// <summary>
    /// The sentence of a notice left for the step's page that informs rather than refuses, if that is the one left —
    /// reading it removes it; a refusal stays for <see cref="TakeCheckoutStepNotice"/>.
    /// </summary>
    public static string? TakeCheckoutStepInformation(this ITempDataDictionary tempData, CheckoutStep step)
    {
        if (FromCode(tempData.Peek(KeyOf(step)) as string) is not { } notice || !notice.Informs())
        {
            return null;
        }

        tempData.Remove(KeyOf(step));
        return notice.Sentence();
    }

    /// <summary>Whether the notice informs the customer rather than says why a step was refused.</summary>
    private static bool Informs(this CheckoutStepNotice notice) => notice == CheckoutStepNotice.PaymentAlreadyAuthorized;

    /// <summary>The TempData key of the notice addressed to a step's page, written out per step.</summary>
    private static string KeyOf(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => "notice.checkout.buyer",
        CheckoutStep.Delivery => "notice.checkout.delivery",
        CheckoutStep.Payment => "notice.checkout.payment",
        CheckoutStep.Review => "notice.checkout.review",
        CheckoutStep.Confirmation => "notice.checkout.confirmation",
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, null),
    };
}