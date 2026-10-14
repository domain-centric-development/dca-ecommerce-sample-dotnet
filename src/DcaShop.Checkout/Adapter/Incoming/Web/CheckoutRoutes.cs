using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>The routes of the checkout pages — where the web adapter sends a customer for a given <see cref="StepAccess"/>.</summary>
internal static class CheckoutRoutes
{
    public const string Cart = "/cart";

    /// <summary>The query parameter that carries a <see cref="CheckoutStepNotice"/>'s code to the page a redirect leads to.</summary>
    public const string Notice = "notice";

    public static string PathOf(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => "/checkout/buyer",
        CheckoutStep.Delivery => "/checkout/delivery",
        CheckoutStep.Payment => "/checkout/payment",
        CheckoutStep.Review => "/checkout/review",
        CheckoutStep.Confirmation => "/checkout/confirmation",
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    /// <summary>Where a denied <see cref="StepAccess"/> sends the customer.</summary>
    public static string PathTo(StepAccess access) =>
        access.RedirectStep is { } step ? PathOf(step) : Cart;

    /// <summary>
    /// Where a denied <see cref="StepAccess"/> sends the customer, a step carrying the notice it shows; the cart reads
    /// no notice, so its path stays plain.
    /// </summary>
    public static string PathTo(StepAccess access, CheckoutStepNotice notice) =>
        access.RedirectStep is { } step ? PathWith(step, notice) : Cart;

    /// <summary>A step's page carrying a notice — and the number it names.</summary>
    public static string PathWith(CheckoutStep step, CheckoutStepNotice notice, int? count = null) =>
        WithCount($"{PathOf(step)}?{Notice}={notice.Code()}", count);

    /// <summary>The cart page carrying a notice the cart page knows — and the number it names.</summary>
    public static string CartWith(CartNotice notice, int? count = null) =>
        WithCount($"{Cart}?{Notice}={notice.Code()}", count);

    private static string WithCount(string path, int? count) =>
        count is { } number
            ? $"{path}&{CheckoutStepNotices.CountParameter}={number.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : path;
}