namespace DcaShop.Checkout.Domain.Model;

/// <summary>Lifecycle status of a checkout session.</summary>
public enum CheckoutSessionStatus
{
    Active,
    Confirmed,
    Completed,
    Abandoned,
    Expired,
}

public static class CheckoutSessionStatusExtensions
{
    public static bool IsModifiable(this CheckoutSessionStatus status) => status == CheckoutSessionStatus.Active;

    public static bool IsTerminal(this CheckoutSessionStatus status) =>
        status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Abandoned or CheckoutSessionStatus.Expired;

    public static bool CanConfirm(this CheckoutSessionStatus status) => status == CheckoutSessionStatus.Active;

    public static bool CanComplete(this CheckoutSessionStatus status) => status == CheckoutSessionStatus.Confirmed;
}
