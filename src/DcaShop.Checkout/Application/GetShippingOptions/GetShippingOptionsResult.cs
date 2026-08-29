using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetShippingOptions;

public sealed record GetShippingOptionsResult(IReadOnlyList<GetShippingOptionsResult.ShippingOptionData> Options)
{
    public sealed record ShippingOptionData(string Id, string Name, string EstimatedDelivery, string Cost, bool IsFree);
}
