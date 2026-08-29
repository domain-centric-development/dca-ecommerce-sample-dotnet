using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Checkout;

/// <summary>Checkout bounded context: the five-step flow from a cart to a confirmed order.</summary>
[BoundedContext("Checkout", Description = "Checkout process, order placement, and payment orchestration")]
[Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Product data is translated into checkout's own article types")]
[Upstream("Cart", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Cart snapshots are translated into checkout's own CartData")]
[Upstream("Cart", Translation.Conformist, Consumes.Events,
    Rationale = "CheckoutConfirmedEvent implements cart's consumer-defined ICartCompletionTrigger contract as-is")]
[ExternalUpstream("Payment Service Provider", Translation.AntiCorruptionLayer, Interaction.Outbound,
    Protocol = "REST", Exchanges = "payment operations (initiate, confirm, refund)",
    Rationale = "Behind the caller-owned IPaymentProviderRegistry port; the sample ships an in-memory registry in place of a real gateway")]
[Partnership("Cart",
    Rationale = "Checkout implements cart's consumer-defined ICartCompletionTrigger contract; both contexts evolve it together")]
public static class CheckoutContext
{
}
