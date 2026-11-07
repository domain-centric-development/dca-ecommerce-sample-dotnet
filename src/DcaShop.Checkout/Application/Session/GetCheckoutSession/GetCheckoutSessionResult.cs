using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

/// <summary>
/// The session the caller asked for by its id, or no checkout session of the caller under that id. The caller names
/// one specific session, so its absence is a refusal, not an empty answer.
/// </summary>
[GenerateOneOf]
public sealed partial class GetCheckoutSessionResult
    : OneOfBase<GetCheckoutSessionResult.Found, GetCheckoutSessionResult.CheckoutSessionNotFound>
{
    /// <summary>
    /// The session. <see cref="StepAccess"/> is set only when the query named a step — the domain's decision whether
    /// that step may be opened.
    /// </summary>
    public sealed record Found(CheckoutCartSnapshot Session, StepAccess? StepAccess = null);

    /// <summary>The caller has no checkout session under that id — it does not exist or is somebody else's (ADR-007).</summary>
    public sealed record CheckoutSessionNotFound(CheckoutSessionId SessionId) : IRefusal;
}