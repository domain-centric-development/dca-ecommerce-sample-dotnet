using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// What every checkout step page does the same way: it opens only when the domain grants the step, and it turns a
/// refused submission into the page's answer. A refusal is translated here, once: the page controllers hand their
/// use-case call to <see cref="SubmittingAsync"/> or <see cref="PlacingTheOrderAsync"/> and never catch a refusal
/// themselves. The checkout session is keyed on the visitor identity, exactly as the cart is — so a guest who
/// registers mid-checkout keeps the session they started; the current session is resolved from the customer, never
/// from the URL.
/// </summary>
/// <param name="page">The page controller this serves; its TempData carries the notices.</param>
internal sealed class CheckoutStepPage(
    Controller page,
    IGetActiveCheckoutSessionInputPort active,
    IGetCheckoutSessionInputPort session,
    IIdentityProvider identityProvider)
{
    /// <summary>Renders a step's page for a session, with the error it shows, if any.</summary>
    public delegate Task<IActionResult> Render(CheckoutCartSnapshot session, string? error);

    public string CurrentCustomerId() => identityProvider.GetCurrentIdentity().UserId.Value;

    /// <summary>
    /// A step's page. The notice a redirect left for it is read — and so removed — whatever the page answers: a step
    /// that is denied and redirects passes no earlier notice on, only its own refusal applies. A step's notice is shown
    /// only once step access granted the page; the review's, a refused "Place Order", keeps the review like an error.
    /// </summary>
    public async Task<IActionResult> ShowAsync(CheckoutStep step, Render render, CancellationToken cancellationToken, string? error = null)
    {
        var (notice, count) = page.TempData.TakeCheckoutStepNotice(step);
        var current = await ActiveSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        if (step == CheckoutStep.Review)
        {
            error ??= notice?.Sentence(count);
        }

        // A step re-rendered with an error — a rejected submit, or a refused "Place Order" carried to the review — keeps
        // the page; otherwise the domain decides who may see it
        if (error is null)
        {
            var result = await session.ExecuteAsync(
                GetCheckoutSessionQuery.ForStep(current.SessionId.Value, CurrentCustomerId(), step), cancellationToken);
            if (result.StepAccess is { Granted: false } access)
            {
                return page.Redirect(CheckoutRoutes.PathTo(access));
            }

            current = result.Session!;
        }

        return await render(current, error ?? notice?.Sentence(count));
    }

    /// <summary>
    /// Submits a step and redirects to <paramref name="next"/>; a refusal is said by the step still open when it came
    /// too early, otherwise by the step itself, re-rendered with the sentence.
    /// </summary>
    public async Task<IActionResult> SubmittingAsync(
        CheckoutStep step, string next, Func<Guid, Task> submission, Render render, CancellationToken cancellationToken)
    {
        var current = await ActiveSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        try
        {
            await submission(current.SessionId.Value);
            return page.Redirect(next);
        }
        catch (Exception e) when (e is UseCaseException or DomainException or ArgumentException)
        {
            // The two base types are the checkout's own refusals; the argument exception is the form field that
            // reaches a value object unvalidated.
            return await ToTheStepStillOpenAsync(step, e, cancellationToken)
                ?? await ShowAsync(step, render, cancellationToken, MessageFor(step, e));
        }
    }

    /// <summary>
    /// Places the order and redirects to the confirmation. A refused "Place Order" redirects to the review, so the
    /// browser stays at the review's own address — or to the step still open, when it came too early.
    /// </summary>
    public async Task<IActionResult> PlacingTheOrderAsync(Func<Guid, Task> placing, CancellationToken cancellationToken)
    {
        var current = await ActiveSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        try
        {
            await placing(current.SessionId.Value);
            return page.Redirect(CheckoutRoutes.PathOf(CheckoutStep.Confirmation));
        }
        catch (Exception e) when (e is UseCaseException or DomainException)
        {
            // The confirmation carries no field the customer typed, so only the checkout's own refusals arrive here.
            return await ToTheStepStillOpenAsync(CheckoutStep.Review, e, cancellationToken) ?? BackToTheReview(e);
        }
    }

    /// <summary>Back to the cart, leaving the cart page the notice that says why.</summary>
    public RedirectResult ToTheCart(CartNotice notice)
    {
        page.TempData.LeaveForTheCart(notice);
        return page.Redirect(CheckoutRoutes.Cart);
    }

    /// <summary>
    /// A step sent before the earlier steps are done goes to the step still open, which says why — none for any other
    /// refusal, and none when step access grants <paramref name="step"/> after all: then the step itself says it.
    /// </summary>
    private async Task<RedirectResult?> ToTheStepStillOpenAsync(CheckoutStep step, Exception refusal, CancellationToken cancellationToken)
    {
        if (refusal is not (CheckoutStepNotCompletedException or CheckoutStepOutOfOrderException))
        {
            return null;
        }

        var access = await ActiveSessionAsync(cancellationToken) is { } current
            ? (await session.ExecuteAsync(
                GetCheckoutSessionQuery.ForStep(current.SessionId.Value, CurrentCustomerId(), step), cancellationToken)).StepAccess
            : StepAccess.BackToCart();
        if (access is not { Granted: false })
        {
            return null;
        }

        // The cart reads no notice of the steps, so a customer sent there goes without one.
        if (access.RedirectStep is { } stillOpen)
        {
            page.TempData.Leave(stillOpen, CheckoutStepNotice.EarlierStepsFirst);
        }

        return page.Redirect(CheckoutRoutes.PathTo(access));
    }

    /// <summary>The refused "Place Order" carried to the review, which renders it at its own address.</summary>
    private RedirectResult BackToTheReview(Exception refusal)
    {
        if (refusal is CheckoutValidationException invalid)
        {
            page.TempData.Leave(CheckoutStep.Review, CheckoutStepNotice.ItemsUnavailable, invalid.Validation.Errors.Count);
        }
        else
        {
            page.TempData.Leave(CheckoutStep.Review, ReviewNoticeFor(refusal));
        }

        return page.Redirect(CheckoutRoutes.PathOf(CheckoutStep.Review));
    }

    /// <summary>
    /// What the review tells the customer when "Place Order" was refused, chosen from the failure's type — the
    /// exception's message is for the log, never for the page.
    /// </summary>
    private static CheckoutStepNotice ReviewNoticeFor(Exception refusal) => refusal switch
    {
        CheckoutSessionNotFoundException => CheckoutStepNotice.SessionNotFound,
        CheckoutNotModifiableException => CheckoutStepNotice.CheckoutNotModifiable,
        CheckoutStepNotCompletedException => CheckoutStepNotice.EarlierStepsFirst,
        CheckoutStepOutOfOrderException => CheckoutStepNotice.ReviewFirst,
        _ => CheckoutStepNotice.OrderNotPlaced,
    };

    /// <summary>
    /// What the customer is told when a step refuses a submission, worded from the step and the failure's type and
    /// values — the exception's message is for the log, never for the page; the provider's own wording never
    /// reaches the page either, the shop's does.
    /// </summary>
    private static string MessageFor(CheckoutStep step, Exception failure) => (step, failure) switch
    {
        (_, CheckoutSessionNotFoundException) => CheckoutStepNotice.SessionNotFound.Sentence()!,
        (_, CheckoutNotModifiableException) => CheckoutStepNotice.CheckoutNotModifiable.Sentence()!,
        (CheckoutStep.Payment, PaymentInitiationFailedException) =>
            "The payment was refused. Please choose another way to pay.",
        (CheckoutStep.Payment, PaymentProviderUnavailableException) =>
            "The payment provider is not available right now. Please try again later.",
        (CheckoutStep.Payment, PaymentProviderNotFoundException) =>
            "This way to pay is not offered. Please choose another one.",
        (CheckoutStep.Payment, NothingToPayException) => "There is nothing to pay for this order.",
        (_, CheckoutStepNotCompletedException or CheckoutStepOutOfOrderException) => CheckoutStepNotice.EarlierStepsFirst.Sentence()!,
        (CheckoutStep.BuyerInfo, ArgumentException) => "Please check the details you entered.",
        (CheckoutStep.Delivery, ArgumentException) => "Please check the delivery address you entered.",
        (CheckoutStep.Payment, ArgumentException) => "Please choose a way to pay.",
        (CheckoutStep.BuyerInfo, _) => "Your details could not be saved. Please try again.",
        (CheckoutStep.Delivery, _) => "Your delivery details could not be saved. Please try again.",
        (CheckoutStep.Payment, _) => "Your payment selection could not be saved. Please try again.",
        _ => "The checkout could not continue. Please try again.",
    };

    /// <summary>Back to the cart, telling the customer there is no checkout to continue.</summary>
    private RedirectResult NoActiveSession() => ToTheCart(CartNotice.NoActiveCheckout);

    private async Task<CheckoutCartSnapshot?> ActiveSessionAsync(CancellationToken cancellationToken) =>
        (await active.ExecuteAsync(new GetActiveCheckoutSessionQuery(CurrentCustomerId()), cancellationToken)).Session;
}