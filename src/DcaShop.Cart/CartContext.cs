using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Cart;

/// <summary>Shopping Cart bounded context: a customer's cart from the first item to the hand-over to checkout.</summary>
[BoundedContext("Shopping Cart", Description = "Cart management, item additions/removals, and cart lifecycle")]
[Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Cart works with its own article snapshot; the catalog model must not leak into cart invariants")]
[Upstream("Pricing", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Price lookups are translated into the cart's own article data")]
[Upstream("Inventory", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Stock availability is translated into the cart's own article data")]
[Partnership("Checkout",
    Rationale = "Cart owns the consumer-defined ICartCompletionTrigger contract that checkout events implement; both contexts evolve it together")]
[Upstream("Account", Translation.Conformist, Consumes.Api,
    Rationale = "Incoming adapters read the caller's identity from Account's published IIdentityService as-is and hand the customer to their use cases as a command or query parameter")]
public static class CartContext
{
}
