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
    Protocol = "REST", Exchanges = "payment operations (initiate, confirm, cancel)",
    Rationale = "Behind a caller-owned port per provider and a registry that chooses one; a REST adapter translates the provider's answers where its address is configured, a stand-in takes payments otherwise")]
[ExternalUpstream("Mail Provider", Translation.AntiCorruptionLayer, Interaction.Outbound,
    Protocol = "REST", Exchanges = "order confirmation mail (recipient, template, idempotency key, total) and its answer (accepted, unavailable)",
    Rationale = "Behind the caller-owned IOrderConfirmationMailer port; a REST adapter translates the provider's answers where its address is configured, a stand-in accepts every mail and sends nothing otherwise")]
[Upstream("Inventory", Translation.Conformist, Consumes.Events,
    Rationale = "CheckoutConfirmedEvent implements inventory's consumer-defined IStockReductionTrigger contract as-is")]
[Partnership("Cart",
    Rationale = "Checkout implements cart's consumer-defined ICartCompletionTrigger contract; both contexts evolve it together")]
[Partnership("Inventory",
    Rationale = "Checkout implements inventory's consumer-defined IStockReductionTrigger contract; both contexts evolve it together")]
public static class CheckoutContext
{
}