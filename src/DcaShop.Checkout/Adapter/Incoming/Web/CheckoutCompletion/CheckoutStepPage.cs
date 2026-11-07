using DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// What every checkout step page does the same way: it opens only when the domain grants the step, and it turns a
/// refused submission into the page's answer. A refusal is worded here, once: the page controllers hand their
/// use-case call to <see cref="SubmittingAsync"/> or <see cref="PlacingTheOrderAsync"/>, matching its result's cases
/// onto a <see cref="StepRefusal"/>, and never catch a refusal themselves; what is still thrown — a value a value object
/// refuses, a rule of the model no use case returns as a case — is caught here. The checkout session is keyed on the visitor identity, exactly as the cart is — so a guest who
/// registers mid-checkout keeps the session they started; the current session is resolved from the customer, never
/// from the URL. A guest who logs in mid-checkout hands the cart to the account, and the step page they return to
/// resumes the cart's open checkout for the account.
/// </summary>
/// <param name="page">The page controller this serves; its TempData carries the notices.</param>
internal sealed class CheckoutStepPage(
    Controller page,
    IGetActiveCheckoutSessionInputPort active,
    IGetCheckoutSessionInputPort session,
    IResumeCheckoutInputPort resume,
    IIdentityProvider identityProvider)
{
    /// <summary>Renders a step's page for a session, with the error it shows, if any.</summary>
    public delegate Task<IActionResult> Render(CheckoutCartSnapshot session, string? error);

    public string CurrentCustomerId() => identityProvider.GetCurrentIdentity().UserId.Value;

    /// <summary>
    /// A step's page. The notice a redirect left for it is read — and so removed — whatever the page answers: a step
    /// that is denied and redirects passes no earlier notice on, only its own refusal applies. A step's notice is shown
    /// only once step access granted the page; the review's, a refused "Place Order", keeps the review like an error.
    /// Without a session of the customer the page resumes the open checkout of the customer's cart, if there is one.
    /// </summary>
    public async Task<IActionResult> ShowAsync(CheckoutStep step, Render render, CancellationToken cancellationToken, string? error = null)
    {
        var (notice, count, product) = page.TempData.TakeCheckoutStepNotice(step);
        var current = await ActiveSessionAsync(cancellationToken) ?? await ResumedSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        if (step == CheckoutStep.Review)
        {
            error ??= notice?.Sentence(count, product);
        }

        // A step re-rendered with an error — a rejected submit, or a refused "Place Order" carried to the review — keeps
        // the page; otherwise the domain decides who may see it
        if (error is null)
        {
            var result = await ForStepAsync(current, step, cancellationToken);
            if (StepAccessOf(result) is { Granted: false } access)
            {
                return page.Redirect(CheckoutRoutes.PathTo(access));
            }

            current = result.AsT0.Session;
        }

        return await render(current, error ?? notice?.Sentence(count, product));
    }

    /// <summary>
    /// Submits a step and redirects to <paramref name="next"/>; a refusal is said by the step itself, re-rendered with
    /// the sentence.
    /// </summary>
    public Task<IActionResult> SubmittingAsync(
        CheckoutStep step, string next, Func<Guid, Task<StepRefusal?>> submission, Render render, CancellationToken cancellationToken) =>
        SubmitAsync(
            step,
            next,
            submission,
            // A form field the customer typed still reaches a value object unvalidated.
            acceptsTypedValues: true,
            refusal => ShowAsync(step, render, cancellationToken, MessageFor(step, refusal)),
            cancellationToken);

    /// <summary>
    /// Places the order and redirects to the confirmation. A refused "Place Order" redirects to the review, so the
    /// browser stays at the review's own address. One not sent <paramref name="fromTheReview"/> places nothing: it
    /// leads to the review, which asks for a look at the order first — or, while the review is closed, to the step
    /// still open.
    /// </summary>
    public Task<IActionResult> PlacingTheOrderAsync(bool fromTheReview, Func<Guid, Task<StepRefusal?>> placing, CancellationToken cancellationToken) =>
        fromTheReview
            ? SubmitAsync(
                CheckoutStep.Review,
                CheckoutRoutes.PathOf(CheckoutStep.Confirmation),
                placing,
                // The confirmation carries no field the customer typed.
                acceptsTypedValues: false,
                refusal => Task.FromResult<IActionResult>(BackToTheReview(refusal)),
                cancellationToken)
            : ToTheReviewFirstAsync(cancellationToken);

    /// <summary>Back to the cart, leaving the cart page the notice that says why.</summary>
    public RedirectResult ToTheCart(CartNotice notice)
    {
        page.TempData.LeaveForTheCart(notice);
        return page.Redirect(CheckoutRoutes.Cart);
    }

    /// <summary>
    /// The one place a step's submission meets a refusal, whichever handler sent it. A step sent before the earlier
    /// steps are done goes to the step still open, which says why; any other refusal — and an early one whose step the
    /// domain grants after all — is said by the step, as <paramref name="saidByTheStep"/> answers it.
    /// </summary>
    private async Task<IActionResult> SubmitAsync(
        CheckoutStep step,
        string next,
        Func<Guid, Task<StepRefusal?>> submission,
        bool acceptsTypedValues,
        Func<StepRefusal, Task<IActionResult>> saidByTheStep,
        CancellationToken cancellationToken)
    {
        var current = await ActiveSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        StepRefusal? refusal;
        try
        {
            refusal = await submission(current.SessionId.Value);
        }
        catch (ArgumentException) when (acceptsTypedValues)
        {
            refusal = StepRefusal.ValueRefused;
        }
        catch (DomainException)
        {
            refusal = StepRefusal.RuleRefused;
        }

        if (refusal is null)
        {
            return page.Redirect(next);
        }

        return await ToTheStepStillOpenAsync(step, refusal, cancellationToken) ?? await saidByTheStep(refusal);
    }

    /// <summary>
    /// A step sent before the earlier steps are done goes to the step still open, which says why — none for any other
    /// refusal, and none when step access grants <paramref name="step"/> after all: then the step itself says it.
    /// </summary>
    private async Task<RedirectResult?> ToTheStepStillOpenAsync(CheckoutStep step, StepRefusal refusal, CancellationToken cancellationToken)
    {
        if (!refusal.SentTooEarly)
        {
            return null;
        }

        var access = await ActiveSessionAsync(cancellationToken) is { } current
            ? StepAccessOf(await ForStepAsync(current, step, cancellationToken))
            : StepAccess.BackToCart();
        return access is { Granted: false } ? ToTheStepStillOpen(access) : null;
    }

    /// <summary>
    /// A "Place Order" sent from another page than the review: the review, once open, asks for a look at the order
    /// first; before that the step still open says why.
    /// </summary>
    private async Task<IActionResult> ToTheReviewFirstAsync(CancellationToken cancellationToken)
    {
        var current = await ActiveSessionAsync(cancellationToken);
        if (current is null)
        {
            return NoActiveSession();
        }

        if (StepAccessOf(await ForStepAsync(current, CheckoutStep.Review, cancellationToken)) is { Granted: false } access)
        {
            return ToTheStepStillOpen(access);
        }

        page.TempData.Leave(CheckoutStep.Review, CheckoutStepNotice.ReviewFirst);
        return page.Redirect(CheckoutRoutes.PathOf(CheckoutStep.Review));
    }

    /// <summary>To the step <paramref name="access"/> denies in favour of, which says that earlier steps come first.</summary>
    private RedirectResult ToTheStepStillOpen(StepAccess access)
    {
        // The cart reads no notice of the steps, so a customer sent there goes without one.
        if (access.RedirectStep is { } stillOpen)
        {
            page.TempData.Leave(stillOpen, CheckoutStepNotice.EarlierStepsFirst);
        }

        return page.Redirect(CheckoutRoutes.PathTo(access));
    }

    /// <summary>
    /// The refused "Place Order" carried to the review, which renders it at its own address — and with it the order as
    /// it is now. A line the stock no longer covers and a price changed since the review are named by their product;
    /// lines whose prices changed are counted.
    /// </summary>
    private RedirectResult BackToTheReview(StepRefusal refusal)
    {
        switch (refusal.What)
        {
            case StepRefusal.Kind.StockRanShort:
                page.TempData.Leave(CheckoutStep.Review, CheckoutStepNotice.StockRanShort, product: refusal.ProductName);
                break;
            case StepRefusal.Kind.PriceChangedSinceReview:
                page.TempData.Leave(CheckoutStep.Review, CheckoutStepNotice.PriceChangedSinceReview, product: refusal.ProductName);
                break;
            case StepRefusal.Kind.PricesChanged:
                page.TempData.Leave(CheckoutStep.Review, CheckoutStepNotice.PricesChanged, refusal.Count);
                break;
            default:
                page.TempData.Leave(CheckoutStep.Review, ReviewNoticeFor(refusal));
                break;
        }

        return page.Redirect(CheckoutRoutes.PathOf(CheckoutStep.Review));
    }

    /// <summary>What the review tells the customer when "Place Order" was refused, chosen from the refusal.</summary>
    private static CheckoutStepNotice ReviewNoticeFor(StepRefusal refusal) => refusal.What switch
    {
        StepRefusal.Kind.SessionNotFound => CheckoutStepNotice.SessionNotFound,
        StepRefusal.Kind.NotModifiable => CheckoutStepNotice.CheckoutNotModifiable,
        StepRefusal.Kind.StepNotCompleted => CheckoutStepNotice.EarlierStepsFirst,
        StepRefusal.Kind.StepOutOfOrder => CheckoutStepNotice.ReviewFirst,
        StepRefusal.Kind.OrderChangedSinceReview => CheckoutStepNotice.OrderChangedSinceReview,
        StepRefusal.Kind.PaymentRefused => CheckoutStepNotice.PaymentRefused,
        StepRefusal.Kind.PaymentProviderUnavailable => CheckoutStepNotice.PaymentProviderUnavailable,
        _ => CheckoutStepNotice.OrderNotPlaced,
    };

    /// <summary>
    /// What the customer is told when a step refuses a submission, worded from the step and the refusal — the
    /// provider's own wording never reaches the page, the shop's does.
    /// </summary>
    private static string MessageFor(CheckoutStep step, StepRefusal refusal) => (step, refusal.What) switch
    {
        (_, StepRefusal.Kind.SessionNotFound) => CheckoutStepNotice.SessionNotFound.Sentence()!,
        (_, StepRefusal.Kind.NotModifiable) => CheckoutStepNotice.CheckoutNotModifiable.Sentence()!,
        (CheckoutStep.Payment, StepRefusal.Kind.PaymentProviderUnavailable) =>
            CheckoutStepNotice.PaymentProviderUnavailable.Sentence()!,
        (CheckoutStep.Payment, StepRefusal.Kind.PaymentProviderNotFound) =>
            "This way to pay is not offered. Please choose another one.",
        (CheckoutStep.Payment, StepRefusal.Kind.NothingToPay) => "There is nothing to pay for this order.",
        (CheckoutStep.Delivery, StepRefusal.Kind.ShippingOptionNotOffered) =>
            "This shipping method is not offered. Please choose another one.",
        (_, StepRefusal.Kind.StepNotCompleted or StepRefusal.Kind.StepOutOfOrder) => CheckoutStepNotice.EarlierStepsFirst.Sentence()!,
        (CheckoutStep.BuyerInfo, StepRefusal.Kind.ValueRefused) => "Please check the details you entered.",
        (CheckoutStep.Delivery, StepRefusal.Kind.ValueRefused) => "Please check the delivery address you entered.",
        (CheckoutStep.Payment, StepRefusal.Kind.ValueRefused) => "Please choose a way to pay.",
        (CheckoutStep.BuyerInfo, _) => "Your details could not be saved. Please try again.",
        (CheckoutStep.Delivery, _) => "Your delivery details could not be saved. Please try again.",
        (CheckoutStep.Payment, _) => "Your payment selection could not be saved. Please try again.",
        _ => "The checkout could not continue. Please try again.",
    };

    /// <summary>Back to the cart, telling the customer there is no checkout to continue.</summary>
    private RedirectResult NoActiveSession() => ToTheCart(CartNotice.NoActiveCheckout);

    /// <summary>
    /// The domain's decision whether a step may be opened, read from the session query asked for that step: a checkout
    /// the caller does not have sends them back to the cart.
    /// </summary>
    private static StepAccess StepAccessOf(GetCheckoutSessionResult result) => result.Match(
        found => found.StepAccess!,
        notFound => StepAccess.BackToCart());

    /// <summary>The session with the domain's decision whether <paramref name="step"/> may be opened.</summary>
    private Task<GetCheckoutSessionResult> ForStepAsync(CheckoutCartSnapshot current, CheckoutStep step, CancellationToken cancellationToken) =>
        session.ExecuteAsync(GetCheckoutSessionQuery.ForStep(current.SessionId.Value, CurrentCustomerId(), step), cancellationToken);

    private async Task<CheckoutCartSnapshot?> ActiveSessionAsync(CancellationToken cancellationToken) =>
        (await active.ExecuteAsync(new GetActiveCheckoutSessionQuery(CurrentCustomerId()), cancellationToken)).Session;

    /// <summary>The open checkout of the customer's cart, now the customer's — null when the cart has none.</summary>
    private async Task<CheckoutCartSnapshot?> ResumedSessionAsync(CancellationToken cancellationToken) =>
        (await resume.ExecuteAsync(new ResumeCheckoutCommand(CurrentCustomerId()), cancellationToken)).Resumed
            ? await ActiveSessionAsync(cancellationToken)
            : null;
}