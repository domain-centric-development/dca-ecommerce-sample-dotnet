using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.CheckoutCompletion.GetReview;

/// <summary>The review, or no checkout session of the caller under that id.</summary>
[GenerateOneOf]
public sealed partial class GetReviewResult : OneOfBase<GetReviewResult.Found, GetReviewResult.CheckoutSessionNotFound>
{
    /// <summary>The order as the review shows it, and the fingerprint "Place Order" carries back.</summary>
    public sealed record Found(CheckoutCartSnapshot Order, string Fingerprint);

    /// <summary>The caller has no checkout session under that id — it does not exist or is somebody else's (ADR-007).</summary>
    public sealed record CheckoutSessionNotFound(CheckoutSessionId SessionId) : IRefusal;
}