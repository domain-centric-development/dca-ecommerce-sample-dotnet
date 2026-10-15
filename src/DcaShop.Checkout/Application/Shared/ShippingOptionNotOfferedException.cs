using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the shipping method a delivery names is not one the shop offers.</summary>
/// <remarks>
/// <see cref="ShippingOptions"/> is the shop's list of methods; an identifier outside it names nothing this checkout
/// can ship with or price.
/// </remarks>
public sealed class ShippingOptionNotOfferedException : UseCaseException
{
    public ShippingOptionNotOfferedException(string shippingOptionId)
        : base($"Shipping option not offered: {shippingOptionId}")
    {
        ShippingOptionId = shippingOptionId;
    }

    public string ShippingOptionId { get; }
}