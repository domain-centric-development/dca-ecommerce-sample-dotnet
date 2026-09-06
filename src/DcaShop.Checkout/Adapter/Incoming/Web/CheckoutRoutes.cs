using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>The routes of the checkout pages — where the web adapter sends a customer for a given <see cref="StepAccess"/>.</summary>
internal static class CheckoutRoutes
{
    public const string Cart = "/cart";

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
}
