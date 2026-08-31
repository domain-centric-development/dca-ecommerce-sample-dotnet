using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Domain.Specification;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

/// <summary>
/// The repository answers specification queries page by page. The in-memory adapter uses the port's default —
/// filter and page where the carts are — so an adapter that can push the predicate down may replace it later.
/// </summary>
public sealed class ShoppingCartRepositoryPagingTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    [Fact]
    public async Task OnlyMatchingCartsAreCountedAndPaged()
    {
        IShoppingCartRepository repository = new InMemoryShoppingCartRepository();
        for (var i = 0; i < 5; i++)
        {
            await repository.SaveAsync(CartWith(Quantity.Of(1)));
        }

        var checkedOut = CartWith(Quantity.Of(1));
        checkedOut.Checkout();
        await repository.SaveAsync(checkedOut);

        var firstPage = await repository.FindByAsync(new ActiveCart(), PagingRequest.Of(0, 2));
        var lastPage = await repository.FindByAsync(new ActiveCart(), PagingRequest.Of(2, 2));

        Assert.Equal(5, firstPage.TotalElements);
        Assert.Equal(2, firstPage.Content.Count);
        Assert.Single(lastPage.Content);
        Assert.All(firstPage.Content, cart => Assert.True(cart.IsActive));
    }

    [Fact]
    public async Task ACustomersCartsAreFoundWhateverTheirStatus()
    {
        var repository = new InMemoryShoppingCartRepository();
        var customerId = CustomerId.Of("customer-1");
        var first = new ShoppingCart(CartId.Generate(), customerId);
        first.AddItem(ProductId.Generate(), Quantity.Of(1), Ten);
        first.Checkout();
        await repository.SaveAsync(first);
        await repository.SaveAsync(new ShoppingCart(CartId.Generate(), customerId));
        await repository.SaveAsync(CartWith(Quantity.Of(1)));

        var carts = await repository.FindByCustomerAsync(customerId);

        Assert.Equal(2, carts.Count);
    }

    private static ShoppingCart CartWith(Quantity quantity)
    {
        var cart = new ShoppingCart(CartId.Generate(), CustomerId.Of($"customer-{Guid.NewGuid()}"));
        cart.AddItem(ProductId.Generate(), quantity, Ten);
        return cart;
    }
}
