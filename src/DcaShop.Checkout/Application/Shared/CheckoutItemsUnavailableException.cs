using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when a checkout would start although some of the cart's positions cannot be bought.</summary>
/// <remarks>
/// Out of stock, withdrawn from the assortment, priced differently — the enriched read model answers that per
/// position, and the customer is told which ones before a session exists.
/// </remarks>
public sealed class CheckoutItemsUnavailableException : UseCaseException
{
    public CheckoutItemsUnavailableException(IReadOnlyList<ProductId> productIds)
        : base($"Cannot start checkout, {productIds.Count} item(s) unavailable or out of stock")
    {
        ProductIds = productIds.ToList();
    }

    /// <summary>The positions that refused, in cart order.</summary>
    public IReadOnlyList<ProductId> ProductIds { get; }
}
