using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>
/// What sending an order's confirmation came to: sent (or sent before), refused because the checkout holds no placed
/// order, or refused because the mail provider — a foreign system — did not accept the mail.
/// </summary>
[GenerateOneOf]
public sealed partial class SendOrderConfirmationResult
    : OneOfBase<
        SendOrderConfirmationResult.Sent,
        SendOrderConfirmationResult.NoPlacedOrder,
        SendOrderConfirmationResult.MailProviderUnavailable>
{
    /// <summary>The order whose confirmation was sent; <paramref name="AlreadySent"/> when this delivery found it sent before.</summary>
    public sealed record Sent(Guid SessionId, bool AlreadySent);

    /// <summary>
    /// No order was placed with that session of the caller — there is none (ADR-007), or it names no buyer to send the
    /// confirmation to; nothing can be sent, now or later.
    /// </summary>
    public sealed record NoPlacedOrder(CheckoutSessionId SessionId) : IRefusal;

    /// <summary>
    /// The mail provider did not accept the mail under <paramref name="Key"/>; nothing was recorded, and trying again
    /// later with the same key is the sensible answer.
    /// </summary>
    public sealed record MailProviderUnavailable(string Key) : IRefusal;
}