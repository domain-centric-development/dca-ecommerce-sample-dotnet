using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Checkout;

/// <summary>Checkout bounded context: the five-step flow from a cart to a confirmed order.</summary>
[BoundedContext("Checkout", Description = "Checkout process, order placement, and payment orchestration")]
[Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Product data is translated into checkout's own article types")]
[Upstream("Pricing", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Prices are translated into checkout's own line item amounts")]
[Upstream("Inventory", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Stock availability is translated into checkout's own article data")]
[Upstream("Cart", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Cart snapshots are translated into checkout's own CartData")]
[Upstream("Cart", Translation.Conformist, Consumes.Events,
    Rationale = "CheckoutConfirmedEvent implements cart's consumer-defined ICartCompletionTrigger contract as-is; cart's CartContentsChangedEvent is consumed as published")]
[ExternalUpstream("Payment Service Provider", Translation.AntiCorruptionLayer, Interaction.Outbound,
    Protocol = "REST", Exchanges = "payment operations (initiate, confirm, refund)",
    Rationale = "Behind the caller-owned IPaymentProviderRegistry port; the sample ships an in-memory registry in place of a real gateway")]
[Upstream("Inventory", Translation.Conformist, Consumes.Events,
    Rationale = "CheckoutConfirmedEvent implements inventory's consumer-defined IStockReductionTrigger contract as-is")]
[Partnership("Cart",
    Rationale = "Checkout implements cart's consumer-defined ICartCompletionTrigger contract; both contexts evolve it together")]
[Partnership("Inventory",
    Rationale = "Checkout implements inventory's consumer-defined IStockReductionTrigger contract; both contexts evolve it together")]
public static class CheckoutContext
{
}
