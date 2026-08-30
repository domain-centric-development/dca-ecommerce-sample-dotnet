using DcaShop.Checkout.Domain.Event;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class CheckoutSessionTest
{
    private static readonly ProductId Product = ProductId.Generate();
    private static readonly ShippingOption Standard = new("standard", "Standard", "5-7 days", Money.Euro(4.99m));

    private static CheckoutSession Started()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), Product, "Thing", Money.Euro(10m), 2, null);
        return CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal, new TaxCalculator());
    }

    private sealed class FixedResolver : ICheckoutArticlePriceResolver
    {
        private readonly ArticlePrice _price;

        public FixedResolver(bool available, int stock) => _price = new ArticlePrice(Money.Euro(10m), available, stock);

        public ArticlePrice Resolve(ProductId productId) => _price;
    }

    [Fact]
    public void StartRaisesCheckoutSessionStartedAndBeginsAtBuyerInfo()
    {
        var session = Started();

        Assert.Equal(CheckoutStep.BuyerInfo, session.CurrentStep);
        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
        Assert.IsType<CheckoutSessionStarted>(Assert.Single(session.DomainEvents));
        Assert.Equal(Money.Euro(20m), session.Totals.Total);
    }

    [Fact]
    public void StepsMustBeCompletedInOrder()
    {
        var session = Started();

        Assert.Throws<InvalidOperationException>(() => session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard, new TaxCalculator()));
        Assert.Throws<InvalidOperationException>(() => session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice"))));
    }

    [Fact]
    public void HappyPathAdvancesStepsAndAddsShipping()
    {
        var session = Started();

        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        Assert.Equal(CheckoutStep.Delivery, session.CurrentStep);

        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard, new TaxCalculator());
        Assert.Equal(CheckoutStep.Payment, session.CurrentStep);
        Assert.Equal(Money.Euro(24.99m), session.Totals.Total);

        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));
        Assert.Equal(CheckoutStep.Review, session.CurrentStep);

        session.ClearDomainEvents();
        session.Confirm(new FixedResolver(available: true, stock: 5));

        Assert.Equal(CheckoutSessionStatus.Confirmed, session.Status);
        Assert.Equal(CheckoutStep.Confirmation, session.CurrentStep);
        var confirmed = Assert.IsType<CheckoutConfirmed>(Assert.Single(session.DomainEvents));
        Assert.Equal(Money.Euro(24.99m), confirmed.TotalAmount);
    }

    [Fact]
    public void ConfirmFailsWhenStockIsInsufficient()
    {
        var session = Started();
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard, new TaxCalculator());
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));

        Assert.Throws<InvalidOperationException>(() => session.Confirm(new FixedResolver(available: true, stock: 1)));
        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
    }

    [Fact]
    public void ConfirmedSessionIsNoLongerModifiable()
    {
        var session = Started();
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard, new TaxCalculator());
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));
        session.Confirm(new FixedResolver(available: true, stock: 5));

        Assert.Throws<InvalidOperationException>(() => session.SubmitBuyerInfo(new BuyerInfo("x@y.de", "B", "C", "1")));
    }

    [Fact]
    public void BuyerInfoRequiresValidEmail() =>
        Assert.Throws<ArgumentException>(() => new BuyerInfo("not-an-email", "A", "B", "1"));

    [Fact]
    public void CannotStartWithoutLineItems() =>
        Assert.Throws<ArgumentException>(() => CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("g"), Array.Empty<CheckoutLineItem>(), Money.Euro(0m), new TaxCalculator()));
}
