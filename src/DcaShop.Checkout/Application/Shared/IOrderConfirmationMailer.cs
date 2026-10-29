using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>
/// Output port for the order confirmation mail. The checkout says to whom, with which total and under which key; how
/// the mail is rendered and delivered is the mail provider's.
/// </summary>
public interface IOrderConfirmationMailer : IOutputPort
{
    /// <summary>
    /// Hands the mail to the mail provider. Answers whether the provider accepted it; a provider that did not accept it
    /// or did not answer is unavailable, and asking again later with the same key is the sensible answer.
    /// </summary>
    Task<bool> SendAsync(OrderConfirmationMail mail, CancellationToken cancellationToken = default);

    /// <summary>
    /// One order confirmation: the buyer's address, the order total, and the key that is the same for every attempt of
    /// this mail, so the provider sends it once however often it is asked. Nested in the port because it is part of its
    /// contract.
    /// </summary>
    public sealed record OrderConfirmationMail(string To, Money OrderTotal, string Key);
}