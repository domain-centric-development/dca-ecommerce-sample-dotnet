using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Service;

/// <summary>
/// Decides whether a checkout step may be opened: no session sends the customer back to the cart, terminal and
/// confirmed sessions only reach the confirmation, and a step whose prerequisites are unfulfilled sends them to
/// the step they are actually on. Going back to a completed step is allowed. The decision is a <see cref="StepAccess"/>
/// value; which URL a step lives at is the web adapter's business.
/// </summary>
public sealed class CheckoutStepValidator : IDomainService
{
    public StepAccess AccessTo(CheckoutCartSnapshot? session, CheckoutStep targetStep)
    {
        if (session is null)
        {
            return StepAccess.BackToCart();
        }

        if (session.Status.IsTerminal())
        {
            return TerminalStateAccess(session, targetStep);
        }

        if (session.Status.CanComplete())
        {
            return targetStep == CheckoutStep.Confirmation ? StepAccess.Grant() : StepAccess.RedirectTo(CheckoutStep.Confirmation);
        }

        if (targetStep == CheckoutStep.Confirmation)
        {
            return session.IsCompleted ? StepAccess.Grant() : StepAccess.RedirectTo(session.Step);
        }

        return IsSkippingAhead(session, targetStep) ? StepAccess.RedirectTo(session.Step) : StepAccess.Grant();
    }

    private static StepAccess TerminalStateAccess(CheckoutCartSnapshot session, CheckoutStep targetStep) => session.Status switch
    {
        CheckoutSessionStatus.Completed => targetStep == CheckoutStep.Confirmation ? StepAccess.Grant() : StepAccess.RedirectTo(CheckoutStep.Confirmation),
        CheckoutSessionStatus.Superseded or CheckoutSessionStatus.Abandoned or CheckoutSessionStatus.Expired => StepAccess.BackToCart(),
        _ => StepAccess.Grant(),
    };

    private static bool IsSkippingAhead(CheckoutCartSnapshot session, CheckoutStep targetStep) =>
        targetStep.IsAfter(session.Step) || !ArePrerequisitesMet(session, targetStep);

    private static bool ArePrerequisitesMet(CheckoutCartSnapshot session, CheckoutStep targetStep) => targetStep switch
    {
        CheckoutStep.BuyerInfo => true,
        CheckoutStep.Delivery => session.IsStepCompleted(CheckoutStep.BuyerInfo),
        CheckoutStep.Payment => session.IsStepCompleted(CheckoutStep.BuyerInfo) && session.IsStepCompleted(CheckoutStep.Delivery),
        CheckoutStep.Review => session.IsStepCompleted(CheckoutStep.BuyerInfo) && session.IsStepCompleted(CheckoutStep.Delivery) && session.IsStepCompleted(CheckoutStep.Payment),
        CheckoutStep.Confirmation => session.IsCompleted,
        _ => throw new ArgumentOutOfRangeException(nameof(targetStep)),
    };
}
