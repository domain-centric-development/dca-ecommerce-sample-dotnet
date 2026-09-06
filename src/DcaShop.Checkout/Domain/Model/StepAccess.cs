using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// The domain's answer to "may this checkout step be opened?": granted, redirected to the step the session is
/// actually on, or — without a usable session — back to the cart. Carries no routes; the web adapter maps it to paths.
/// </summary>
public sealed record StepAccess(bool Granted, CheckoutStep? RedirectStep) : IValue
{
    public static StepAccess Grant() => new(true, null);

    public static StepAccess RedirectTo(CheckoutStep step) => new(false, step);

    public static StepAccess BackToCart() => new(false, null);

    public bool IsBackToCart => !Granted && RedirectStep is null;
}
