using DcaShop.Account.Adapter.Outgoing.Persistence;
using DcaShop.Account.Domain.Model;
using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Inventory.Adapter.Outgoing.Persistence;
using DcaShop.Inventory.Domain.Model;
using DcaShop.Pricing.Adapter.Outgoing.Persistence;
using DcaShop.Pricing.Domain.Model;
using DcaShop.Product.Adapter.Outgoing.Persistence;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using CartCustomerId = DcaShop.Cart.Domain.Model.CustomerId;
using CheckoutCartId = DcaShop.Checkout.Domain.Model.CartId;
using CheckoutCustomerId = DcaShop.Checkout.Domain.Model.CustomerId;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The Dapper adapters against PostgreSQL: what is saved comes back as it was — a fresh instance every time — the
/// constraints a store holds answer as the port declares, and a check-then-change read takes its row lock.
/// </summary>
public sealed class DapperRepositoriesTest : IDisposable
{
    private readonly IsolatedDatabase _store = new();

    public void Dispose() => _store.Dispose();

    [Fact]
    public async Task AnAccountComesBackWithItsRolesAndAFreshInstance()
    {
        var accounts = new DapperAccountRepository(_store.Database);
        var account = DcaShop.Account.Domain.Model.Account.Reconstitute(
            AccountId.Generate(), Email.Of("jane@example.com"), Owner.Of("Jane", "Doe", new DateOnly(1990, 5, 17)),
            UserId.Of(Guid.NewGuid().ToString()), HashedPassword.FromHash("$2a$12$hash"), AccountStatus.Active,
            [Role.Customer, Role.Staff], DateTimeOffset.UtcNow.AddDays(-1), null);
        await accounts.SaveAsync(account);

        var loaded = await accounts.FindByEmailAsync(Email.Of("jane@example.com"));

        Assert.NotNull(loaded);
        Assert.NotSame(account, loaded);
        Assert.Equal(account.LinkedUserId, loaded.LinkedUserId);
        Assert.Equal(new DateOnly(1990, 5, 17), loaded.Owner.DateOfBirth);
        Assert.Equal([Role.Customer, Role.Staff], loaded.Roles.OrderBy(r => r.Code));
        Assert.Null(loaded.LastLoginAt);
    }

    [Fact]
    public async Task ASkuAnotherProductCarriesIsRefusedAsTaken()
    {
        var products = new DapperProductRepository(_store.Database);
        await products.SaveAsync(Product("SKU-1", "First"));

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => products.SaveAsync(Product("SKU-1", "Second")));

        Assert.Equal("SKU SKU-1 is already taken", refusal.Message);
    }

    [Fact]
    public async Task ProductsAreListedByNameInOrdinalOrder()
    {
        var products = new DapperProductRepository(_store.Database);
        await products.SaveAsync(Product("SKU-B", "beta"));
        await products.SaveAsync(Product("SKU-A", "Alpha"));
        await products.SaveAsync(Product("SKU-C", "alpha"));

        Assert.Equal(["Alpha", "alpha", "beta"], (await products.FindAllAsync()).Select(p => p.Name.Value));
    }

    [Fact]
    public async Task APriceComesBackWithItsAmountCurrencyAndMoment()
    {
        var prices = new DapperProductPriceRepository(_store.Database);
        var price = ProductPrice.Create(ProductId.Generate(), Price.Of(Money.Euro(12.99m)));
        await prices.SaveAsync(price);

        var loaded = await prices.FindByProductIdAsync(price.ProductId);

        Assert.NotNull(loaded);
        Assert.Equal(Money.Euro(12.99m), loaded.CurrentPrice.Value);
        Assert.Equal(price.EffectiveFrom.ToUnixTimeMilliseconds(), loaded.EffectiveFrom.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task ACartComesBackWithItsLinesAndTheirUnits()
    {
        var carts = new DapperShoppingCartRepository(_store.Database);
        var cart = ShoppingCart.Create(DcaShop.Cart.Domain.Model.CartId.Generate(), CartCustomerId.Of("jane"));
        cart.AddItem(ProductId.Generate(), Quantity.Of(3), Price.Of(Money.Euro(9.99m)));
        await carts.SaveAsync(cart);

        var loaded = await carts.FindActiveByCustomerAsync(CartCustomerId.Of("jane"));

        Assert.NotNull(loaded);
        var line = Assert.Single(loaded.Items);
        Assert.Equal(cart.Items[0].StoredUnits, line.StoredUnits);
        Assert.Equal(Money.Euro(29.97m), line.LineTotal);
    }

    [Fact]
    public async Task ASecondActiveCartOfTheSameCustomerIsRefusedAsThePortDeclares()
    {
        var carts = new DapperShoppingCartRepository(_store.Database);
        await carts.SaveAsync(ShoppingCart.Create(DcaShop.Cart.Domain.Model.CartId.Generate(), CartCustomerId.Of("jane")));

        await Assert.ThrowsAsync<ActiveCartAlreadyExistsException>(() =>
            carts.SaveAsync(ShoppingCart.Create(DcaShop.Cart.Domain.Model.CartId.Generate(), CartCustomerId.Of("jane"))));
    }

    [Fact]
    public async Task ACheckoutSessionComesBackWithEveryStepItCompleted()
    {
        var sessions = new DapperCheckoutSessionRepository(_store.Database);
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Mug", Money.Euro(16.99m), 2, null, "line:0-2");
        var session = CheckoutSession.Start(new CheckoutCartId(Guid.NewGuid()), CheckoutCustomerId.Of("jane"), [line], line.LineTotal);
        session.SubmitBuyerInfo(new BuyerInfo("jane@example.com", "Jane", "Doe", "+49 30 1234"));
        session.SubmitDelivery(new DeliveryAddress("Main Street 1", null, "Berlin", "10115", "Germany", null),
            new ShippingOption("standard", "Standard", "3-5 days", Money.Euro(4.99m)));
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("stand-in")));
        await sessions.SaveAsync(session);

        var loaded = await sessions.FindActiveByCartIdAsync(session.CartId);

        Assert.NotNull(loaded);
        Assert.Equal(session.CurrentStep, loaded.CurrentStep);
        Assert.Equal(session.Totals, loaded.Totals);
        Assert.Equal(session.BuyerInfo, loaded.BuyerInfo);
        Assert.Equal(session.DeliveryAddress, loaded.DeliveryAddress);
        Assert.Equal(session.ShippingOption, loaded.ShippingOption);
        Assert.Equal(session.PaymentSelection, loaded.PaymentSelection);
        Assert.Equal(session.LineItems, loaded.LineItems);
    }

    [Fact]
    public async Task AStockLevelReadInATransactionHoldsTheNextReaderBackUntilItCommits()
    {
        var stock = new DapperStockLevelRepository(_store.Database);
        var level = StockLevel.Create(ProductId.Generate(), 10);
        await stock.SaveAsync(level);
        var held = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        var first = _store.Boundary.InTransactionAsync(async ct =>
        {
            var read = await stock.FindByProductIdAsync(level.ProductId, ct);
            read!.Reserve(4);
            await stock.SaveAsync(read, ct);
            held.SetResult();
            await release.Task;
            return 0;
        });
        await held.Task;
        var second = Task.Run(() => _store.Boundary.InTransactionAsync(async ct =>
            (await stock.FindByProductIdAsync(level.ProductId, ct))!.ReservedQuantity.Value));

        await Task.Delay(200);
        Assert.False(second.IsCompleted, "the second reader must wait for the row lock");
        release.SetResult();
        await first;
        Assert.Equal(4, await second);
    }

    [Fact]
    public async Task AProcessedEventMarkRollsBackWithTheEffect()
    {
        var processed = new DcaShop.Inventory.Adapter.Outgoing.Persistence.DapperProcessedEvents(_store.Database);
        var eventId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.Boundary.InTransactionAsync<int>(async ct =>
        {
            await processed.MarkProcessedAsync("reduce-stock", eventId, ct);
            throw new InvalidOperationException("the effect failed");
        }));

        Assert.False(await processed.ContainsAsync("reduce-stock", eventId));
        await processed.MarkProcessedAsync("reduce-stock", eventId);
        Assert.True(await processed.ContainsAsync("reduce-stock", eventId));
        Assert.False(await processed.ContainsAsync("set-stock-level", eventId));
    }

    private static DcaShop.Product.Domain.Model.Product Product(string sku, string name) =>
        DcaShop.Product.Domain.Model.Product.Reconstitute(ProductId.Generate(), Sku.Of(sku), ProductName.Of(name),
            ProductDescription.Of("A product"), Category.Of("Test"), ImageUrl.None());
}