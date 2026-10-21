using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>An open checkout follows its cart to the customer who owns the cart now.</summary>
public sealed class CheckoutSessionHandOverTest
{
    private static readonly CustomerId Guest = CustomerId.Of("guest");
    private static readonly CustomerId Account = CustomerId.Of("account");

    private static CheckoutSession Started(CartId cartId)
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10m), 2, null);
        return CheckoutSession.Start(cartId, Guest, new[] { line }, line.LineTotal);
    }

    [Fact]
    public void AnAbandonedSessionIsNotHandedOver()
    {
        var session = Started(new CartId(Guid.NewGuid()));
        session.Abandon();

        Assert.Throws<CheckoutNotModifiableException>(() => session.HandOverTo(Account));
        Assert.Equal(Guest, session.CustomerId);
    }

    [Fact]
    public void AHandedOverSessionKeepsItsCartItsLinesAndItsStepAndNamesTheNewCustomer()
    {
        var cartId = new CartId(Guid.NewGuid());
        var session = Started(cartId);
        var lines = session.LineItems.ToList();

        session.HandOverTo(Account);

        Assert.Equal(Account, session.CustomerId);
        Assert.Equal(cartId, session.CartId);
        Assert.Equal(lines, session.LineItems);
        Assert.Equal(CheckoutStep.BuyerInfo, session.CurrentStep);
    }
}