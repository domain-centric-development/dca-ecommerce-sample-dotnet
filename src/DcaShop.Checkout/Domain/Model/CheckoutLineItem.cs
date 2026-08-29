using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>A line of the checkout session: product reference, label, unit price at the time of addition, quantity, image.</summary>
public sealed record CheckoutLineItem : IValue
{
    public CheckoutLineItem(CheckoutLineItemId id, ProductId productId, string productName, Money unitPrice, int quantity, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name cannot be blank", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }

        Id = id;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        Quantity = quantity;
        ImageUrl = imageUrl;
    }

    public CheckoutLineItemId Id { get; }

    public ProductId ProductId { get; }

    public string ProductName { get; }

    public Money UnitPrice { get; }

    public int Quantity { get; }

    public string? ImageUrl { get; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public CheckoutLineItem WithQuantity(int newQuantity) => new(Id, ProductId, ProductName, UnitPrice, newQuantity, ImageUrl);
}
