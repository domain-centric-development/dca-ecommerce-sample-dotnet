using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>The shipping options this shop offers.</summary>
public static class ShippingOptions
{
    public static readonly IReadOnlyList<ShippingOption> All = new[]
    {
        new ShippingOption("standard", "Standard Shipping", "5-7 business days", Money.Euro(4.99m)),
        new ShippingOption("express", "Express Shipping", "2-3 business days", Money.Euro(9.99m)),
        new ShippingOption("overnight", "Overnight Shipping", "Next business day", Money.Euro(19.99m)),
        new ShippingOption("free", "Free Shipping", "7-10 business days", Money.Zero("EUR")),
    };

    public static ShippingOption? Find(string id) => All.FirstOrDefault(o => o.Id == id);
}
