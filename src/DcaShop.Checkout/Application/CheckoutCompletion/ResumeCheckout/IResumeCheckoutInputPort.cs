using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;

public interface IResumeCheckoutInputPort : IUseCase<ResumeCheckoutCommand, ResumeCheckoutResult>
{
}