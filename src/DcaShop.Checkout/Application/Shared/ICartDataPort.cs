using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>What checkout needs from the Cart context, in checkout's own terms — the adapter translates the cart's published Api.</summary>
public interface ICartDataPort : IOutputPort
{
    Task<CartData?> FindByIdAsync(CartId cartId, CancellationToken cancellationToken = default);

    Task MarkAsCheckedOutAsync(CartId cartId, CancellationToken cancellationToken = default);
}

public sealed record CartData(CartId CartId, CustomerId CustomerId, IReadOnlyList<CartData.CartItemData> Items, bool Active)
{
    public sealed record CartItemData(ProductId ProductId, Money PriceAtAddition, int Quantity);
}
