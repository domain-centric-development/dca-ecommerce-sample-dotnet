using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>The fingerprint of an order as the review shows it: alike for the same order, different for any change.</summary>
public sealed class OrderFingerprintTest
{
    private static readonly CheckoutLineItemId First = CheckoutLineItemId.Generate();
    private static readonly CheckoutLineItemId Second = CheckoutLineItemId.Generate();
    private static readonly ProductId DomainDrivenDesign = ProductId.Generate();
    private static readonly ProductId CleanArchitecture = ProductId.Generate();
    private static readonly BuyerInfo Jane = new("jane@example.com", "Jane", "Doe", "+49 30 1234567");
    private static readonly DeliveryAddress Berlin = new("Hauptstrasse 1", "Berlin", "10115", "Germany");
    private static readonly ShippingOption Standard = new("standard", "Standard Shipping", "5-7 business days", Money.Euro(4.99m));
    private static readonly PaymentSelection Mock = new(PaymentProviderId.Of("mock"));

    [Fact]
    public void TheSameOrderGivesTheSameFingerprint()
    {
        Assert.Equal(Order(54.99m), Order(54.99m));
    }

    [Fact]
    public void AnotherUnitPriceOnALineGivesAnotherFingerprintNamingThatLine()
    {
        var shown = Order(54.99m);

        var today = Order(49.99m);

        Assert.NotEqual(shown, today);
        Assert.Equal(1, today.FirstChangedLine(shown));
    }

    public static TheoryData<string> OtherDetails() => new() { "shipping", "address", "buyer", "payment" };

    [Theory]
    [MemberData(nameof(OtherDetails))]
    public void AnotherDetailGivesAnotherFingerprintNamingNoLine(string detail)
    {
        var shown = Order(54.99m);

        var changed = detail switch
        {
            "shipping" => Order(54.99m, shipping: new ShippingOption("express", "Express Shipping", "2-3 business days", Money.Euro(9.99m))),
            "address" => Order(54.99m, address: new DeliveryAddress("Hauptstrasse 2", "Berlin", "10115", "Germany")),
            "buyer" => Order(54.99m, buyer: new BuyerInfo("john@example.com", "John", "Doe", "+49 30 1234567")),
            _ => Order(54.99m, payment: new PaymentSelection(PaymentProviderId.Of("invoice"))),
        };

        Assert.NotEqual(shown, changed);
        Assert.Null(changed.FirstChangedLine(shown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    public void AMissingOrGarbledFingerprintMatchesNoOrder(string? sent)
    {
        var order = Order(54.99m);

        Assert.NotEqual(order, OrderFingerprint.From(sent));
        Assert.Equal(order, OrderFingerprint.From(order.Value));
    }

    /// <summary>"Clean Architecture" once at 39.99 and "Domain-Driven Design" once at <paramref name="secondPrice"/>, with Standard Shipping.</summary>
    private static OrderFingerprint Order(
        decimal secondPrice,
        BuyerInfo? buyer = null,
        DeliveryAddress? address = null,
        ShippingOption? shipping = null,
        PaymentSelection? payment = null)
    {
        var lines = new[]
        {
            new CheckoutLineItem(First, CleanArchitecture, "Clean Architecture", Money.Euro(39.99m), 1, null),
            new CheckoutLineItem(Second, DomainDrivenDesign, "Domain-Driven Design", Money.Euro(secondPrice), 1, null),
        };
        var cost = (shipping ?? Standard).Cost;
        return OrderFingerprint.Of(lines, buyer ?? Jane, address ?? Berlin, shipping ?? Standard, payment ?? Mock, Money.Euro(39.99m + secondPrice).Add(cost));
    }
}