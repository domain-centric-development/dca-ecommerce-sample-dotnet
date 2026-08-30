using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>What checkout needs from the Cart context, in checkout's own terms — the adapter translates the cart's published Api.</summary>
public interface ICartDataPort : IOutputPort
{
    /// <summary>The named customer's cart — <see langword="null"/> when it does not exist or is not theirs.</summary>
    Task<CartData?> FindByIdAsync(CartId cartId, CustomerId customerId, CancellationToken cancellationToken = default);

}

public sealed record CartData(CartId CartId, CustomerId CustomerId, IReadOnlyList<CartData.CartItemData> Items, bool Active)
{
    public sealed record CartItemData(ProductId ProductId, Money PriceAtAddition, int Quantity);
}
