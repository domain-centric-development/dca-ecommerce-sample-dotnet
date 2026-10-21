using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.CheckoutCompletion.GetReview;

/// <summary>The order as the review shows it, and the fingerprint "Place Order" carries back.</summary>
public sealed record GetReviewResult(CheckoutCartSnapshot Order, string Fingerprint);