using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Shared shape of a session as the query use cases return it.</summary>
public sealed record CheckoutSessionData(
    Guid SessionId,
    Guid CartId,
    string CustomerId,
    string CurrentStep,
    string Status,
    IReadOnlyList<CheckoutSessionData.LineItemData> LineItems,
    string Subtotal,
    string Shipping,
    string Tax,
    string Total,
    BuyerInfo? BuyerInfo,
    DeliveryAddress? DeliveryAddress,
    ShippingOption? ShippingOption,
    PaymentSelection? PaymentSelection,
    string? OrderReference)
{
    public sealed record LineItemData(Guid LineItemId, Guid ProductId, string ProductName, string UnitPrice, int Quantity, string LineTotal, string? ImageUrl);

    public static CheckoutSessionData From(CheckoutSession s) =>
        new(
            s.Id.Value,
            s.CartId.Value,
            s.CustomerId.Value,
            s.CurrentStep.ToString(),
            s.Status.ToString(),
            s.LineItems.Select(i => new LineItemData(i.Id.Value, i.ProductId.Value, i.ProductName, i.UnitPrice.ToString(), i.Quantity, i.LineTotal.ToString(), i.ImageUrl)).ToList(),
            s.Totals.Subtotal.ToString(),
            s.Totals.Shipping.ToString(),
            s.Totals.Tax.ToString(),
            s.Totals.Total.ToString(),
            s.BuyerInfo,
            s.DeliveryAddress,
            s.ShippingOption,
            s.PaymentSelection,
            s.OrderReference);
}
