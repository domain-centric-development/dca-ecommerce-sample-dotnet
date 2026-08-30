using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.ReadModel;

/// <summary>One line of a <see cref="CheckoutCartSnapshot"/>, as the checkout pages display it.</summary>
public sealed record LineItemSnapshot(
    CheckoutLineItemId LineItemId,
    ProductId ProductId,
    string Name,
    Money Price,
    int Quantity,
    string? ImageUrl) : IValue
{
    public Money LineTotal => Price.Multiply(Quantity);
}
