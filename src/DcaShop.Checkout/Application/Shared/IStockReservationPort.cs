using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>
/// Holds the stock of an order from "Place Order" until the stock reduction that follows the confirmation, so two
/// orders placed at once never both count on the same units.
/// </summary>
public interface IStockReservationPort : IOutputPort
{
    /// <summary>
    /// Reserves the stock of every line, or of none: the answer names each product the stock no longer covers, with
    /// how much of it is left — empty when the order's stock is reserved.
    /// </summary>
    Task<IReadOnlyList<Shortage>> ReserveAsync(IReadOnlyCollection<CheckoutLineItem> lines, CancellationToken cancellationToken = default);

    /// <summary>Gives back what <see cref="ReserveAsync"/> reserved for lines whose order was then not placed.</summary>
    Task ReleaseAsync(IReadOnlyCollection<CheckoutLineItem> lines, CancellationToken cancellationToken = default);

    /// <summary>A product the stock no longer covers, and how much of it is left.</summary>
    public sealed record Shortage(ProductId ProductId, int Left);
}