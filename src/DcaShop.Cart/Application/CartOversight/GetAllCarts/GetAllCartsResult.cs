using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
namespace DcaShop.Cart.Application.CartOversight.GetAllCarts;

/// <summary>
/// Every cart in the shop, as an operator sees them. Each summary's total is at today's prices — the total its
/// customer reads.
/// </summary>
public sealed record GetAllCartsResult(IReadOnlyList<GetAllCartsResult.CartSummary> Carts)
{
    public sealed record CartSummary(
        Guid CartId,
        string CustomerId,
        CartStatus Status,
        int ItemCount,
        Money Total);
}