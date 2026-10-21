using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

/// <summary>The guest cart itself, handed to the account at a login: the same cart, the same positions, another customer.</summary>
public sealed class ShoppingCartHandOverTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));
    private static readonly CustomerId Guest = CustomerId.Of("guest-1");
    private static readonly CustomerId Account = CustomerId.Of("account-1");

    [Fact]
    public void AnAbandonedCartIsNotHandedOver()
    {
        var cart = ShoppingCart.Create(CartId.Generate(), Guest);
        cart.Abandon();

        Assert.Throws<CartNotModifiableException>(() => cart.HandOverTo(Account));
        Assert.Equal(Guest, cart.CustomerId);
    }

    [Fact]
    public void AHandedOverCartKeepsItsIdAndItsPositionsAndNamesTheNewCustomer()
    {
        var id = CartId.Generate();
        var cart = ShoppingCart.Create(id, Guest);
        cart.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);
        var positions = cart.Items.Select(i => (i.Id, i.ProductId, i.Quantity)).ToList();

        cart.HandOverTo(Account);

        Assert.Equal(Account, cart.CustomerId);
        Assert.Equal(id, cart.Id);
        Assert.Equal(positions, cart.Items.Select(i => (i.Id, i.ProductId, i.Quantity)).ToList());
    }
}