using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Cart;

/// <summary>Shopping Cart bounded context: a customer's cart from the first item to the hand-over to checkout.</summary>
[BoundedContext("Shopping Cart", Description = "Cart management, item additions/removals, and cart lifecycle")]
[Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Cart works with its own article snapshot; the catalog model must not leak into cart invariants")]
[Partnership("Checkout",
    Rationale = "Cart owns the consumer-defined ICartCompletionTrigger contract that checkout events implement; both contexts evolve it together")]
public static class CartContext
{
}
