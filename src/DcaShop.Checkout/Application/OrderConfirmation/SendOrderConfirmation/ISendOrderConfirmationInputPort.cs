using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>Sends the order confirmation of a confirmed checkout to its buyer's e-mail address, once per order.</summary>
public interface ISendOrderConfirmationInputPort : IUseCase<SendOrderConfirmationCommand, SendOrderConfirmationResult>
{
}