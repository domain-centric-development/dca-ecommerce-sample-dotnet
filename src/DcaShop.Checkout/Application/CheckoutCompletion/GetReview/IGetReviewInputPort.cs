using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.GetReview;

/// <summary>Reads the customer's checkout as the review shows it — at today's prices — with the order's fingerprint.</summary>
public interface IGetReviewInputPort : IUseCase<GetReviewQuery, GetReviewResult>
{
}