using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Service;

/// <summary>
/// Decides whether a checkout step may be opened: no session sends the customer back to the cart, terminal and
/// confirmed sessions only reach the confirmation, and a step whose prerequisites are unfulfilled sends them to
/// the step they are actually on. Going back to a completed step is allowed.
/// </summary>
public sealed class CheckoutStepValidator : IDomainService
{
    private const string CheckoutBasePath = "/checkout";
    private const string CartPath = "/cart";

    /// <summary>Null when access is allowed, otherwise the path to redirect to.</summary>
    public string? ValidateStepAccess(CheckoutCartSnapshot? session, CheckoutStep targetStep)
    {
        if (session is null)
        {
            return CartPath;
        }

        if (session.Status.IsTerminal())
        {
            return TerminalStateRedirect(session, targetStep);
        }

        if (session.Status.CanComplete())
        {
            return targetStep == CheckoutStep.Confirmation ? null : PathOf(CheckoutStep.Confirmation);
        }

        if (targetStep == CheckoutStep.Confirmation)
        {
            return session.IsCompleted ? null : PathOf(session.Step);
        }

        return IsSkippingAhead(session, targetStep) ? PathOf(session.Step) : null;
    }

    /// <summary>Where a session that must be redirected to its own progress belongs.</summary>
    public string CurrentStepPath(CheckoutCartSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return PathOf(session.Step);
    }

    public static string PathOf(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => CheckoutBasePath + "/buyer",
        CheckoutStep.Delivery => CheckoutBasePath + "/delivery",
        CheckoutStep.Payment => CheckoutBasePath + "/payment",
        CheckoutStep.Review => CheckoutBasePath + "/review",
        CheckoutStep.Confirmation => CheckoutBasePath + "/confirmation",
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    private static string? TerminalStateRedirect(CheckoutCartSnapshot session, CheckoutStep targetStep) => session.Status switch
    {
        CheckoutSessionStatus.Completed => targetStep == CheckoutStep.Confirmation ? null : PathOf(CheckoutStep.Confirmation),
        CheckoutSessionStatus.Abandoned or CheckoutSessionStatus.Expired => CartPath,
        _ => null,
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
