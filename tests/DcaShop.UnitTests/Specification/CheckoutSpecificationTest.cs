using System.Text.Json;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CartSync.SyncCheckoutWithCart;
using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.Checkout.Domain.Event;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging.Abstractions;
using CartModel = DcaShop.Cart.Domain.Model;
namespace DcaShop.UnitTests.Specification;

public sealed class CheckoutSpecificationTest
{
    public static IEnumerable<object[]> Vectors()
    {
        if (!SpecificationVectors.Present) yield break;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specification/vectors/checkout.json")));
        foreach (var v in document.RootElement.EnumerateArray()) yield return new object[] { v.GetProperty("id").GetString()! };
    }
    [SpecificationTheory, MemberData(nameof(Vectors))]
    public async Task DrivesRealUseCases(string id)
    {
        var f = new Fixture(); var session = await f.Start(); Ready(session); session.ClearDomainEvents();
        switch (id)
        {
            case "checkout.snapshot.unchanged-after-cart-edit":
            case "checkout.cart-edit.does-not-create-session":
                {
                    var snapshot = session.LineItems.ToArray(); f.Cart.AddItem(f.Product, CartModel.Quantity.Of(3), Price.Of(Money.Euro(10)));
                    var sync = new SyncCheckoutWithCartUseCase(f.Repository, f, new TaxCalculator(), f, f.Events, new InMemoryTransactionBoundary(), NullLogger<SyncCheckoutWithCartUseCase>.Instance);
                    Assert.False((await sync.ExecuteAsync(new SyncCheckoutWithCartCommand(f.Cart.Id.Value))).WasSynced);
                    Assert.Equal(snapshot, session.LineItems); Assert.Equal(session.Id, (await f.Repository.FindActiveByCartIdAsync(session.CartId))!.Id); break;
                }
            case "checkout.restart.supersedes-open-session":
            case "checkout.superseded.confirm-rejected":
                {
                    var next = await f.Start(); Assert.NotEqual(session.Id, next.Id); Assert.Equal(CheckoutSessionStatus.Superseded, session.Status);
                    await Assert.ThrowsAsync<InvalidOperationException>(() => f.Confirm(session)); Assert.Empty(session.DomainEvents); break;
                }
            case "checkout.abandon.preserves-cart":
                {
                    var snapshot = f.Cart.Items[0].PositionSnapshot; session.Abandon(); Assert.Equal(snapshot, f.Cart.Items[0].PositionSnapshot);
                    var next = await f.Start(); Assert.NotEqual(session.Id, next.Id); Assert.Equal(CheckoutSessionStatus.Abandoned, session.Status); break;
                }
            case "checkout.completed.not-superseded":
                {
                    await f.Confirm(session); session.Complete("order"); await f.Start(); Assert.Equal(CheckoutSessionStatus.Completed, session.Status); break;
                }
            case "checkout.confirm.price-changed":
            case "checkout.confirm.out-of-stock":
                {
                    var totals = session.Totals; var events = f.Events.Published.Count;
                    if (id.EndsWith("price-changed", StringComparison.Ordinal)) f.Price = Money.Euro(11); else f.Stock = 1;
                    var failure = await Assert.ThrowsAsync<CheckoutValidationException>(() => f.Confirm(session));
                    Assert.Equal(f.Product, Assert.Single(failure.Validation.Errors).ProductId); Assert.Equal(CheckoutSessionStatus.Active, session.Status);
                    Assert.Equal(totals, session.Totals); Assert.Empty(session.DomainEvents); Assert.Equal(events, f.Events.Published.Count); break;
                }
            case "checkout.confirm.unchanged":
            case "checkout.confirmed-event.total":
                {
                    await f.Confirm(session); var confirmed = Assert.IsType<CheckoutConfirmed>(f.Events.Published.Last());
                    Assert.Equal(Money.Euro(20), session.Totals.Total); Assert.Equal(session.Totals.Total, confirmed.TotalAmount); break;
                }
            case "checkout.replacement.confirmation-wins": await Race(f, session, true); break;
            case "checkout.replacement.replacement-wins": await Race(f, session, false); break;
            default: Assert.Fail("Vector has no adapter: " + id); break;
        }
    }
    private static async Task Race(Fixture f, CheckoutSession session, bool confirmationWins)
    {
        f.Events.Arm();
        Task first = confirmationWins ? f.Confirm(session) : f.Start();
        await f.Events.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task second = confirmationWins ? f.Start() : f.Confirm(session);
        // Both have fetched facts; the second cannot finish while the first holds the repository operation.
        Assert.False(second.IsCompleted); f.Events.Release.TrySetResult(); await first;
        if (confirmationWins) { await second; Assert.Equal(CheckoutSessionStatus.Confirmed, session.Status); }
        else { await Assert.ThrowsAsync<InvalidOperationException>(() => second); Assert.Equal(CheckoutSessionStatus.Superseded, session.Status); }
    }
    private static void Ready(CheckoutSession s)
    {
        s.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        s.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), new ShippingOption("free", "Free", "Tomorrow", Money.Euro(0)), new TaxCalculator());
        s.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));
    }
    private sealed class Fixture : ICartDataPort, ICheckoutArticleDataPort
    {
        public readonly ProductId Product = ProductId.Generate();
        public readonly CartModel.ShoppingCart Cart = new(CartModel.CartId.Generate(), CartModel.CustomerId.Of("specification"));
        public readonly InMemoryCheckoutSessionRepository Repository = new();
        public readonly Publisher Events = new();
        public Money Price = Money.Euro(10); public int Stock = 100;
        public Fixture() { Cart.AddItem(Product, CartModel.Quantity.Of(2), DcaShop.SharedKernel.Domain.Model.Price.Of(Price)); }
        public async Task<CheckoutSession> Start()
        {
            var result = await new StartCheckoutUseCase(this, new CheckoutCartFactory(), new TaxCalculator(), this, Repository, Events, new InMemoryTransactionBoundary())
                .ExecuteAsync(new StartCheckoutCommand(Cart.Id.Value, Cart.CustomerId.Value));
            return (await Repository.FindByIdAsync(new CheckoutSessionId(result.SessionId)))!;
        }
        public Task<ConfirmCheckoutResult> Confirm(CheckoutSession session) => new ConfirmCheckoutUseCase(Repository, this, Events, new InMemoryTransactionBoundary()).ExecuteAsync(new ConfirmCheckoutCommand(session.Id.Value));
        public Task<CartData?> FindByIdAsync(CartId cartId, CustomerId customerId, CancellationToken cancellationToken = default) => Task.FromResult<CartData?>(new CartData(cartId, customerId, Cart.Items.Select(i => new CartData.CartItemData(i.ProductId, i.PriceAtAddition.Value, i.Quantity.Value, i.PositionSnapshot)).ToArray(), Cart.IsActive));
        public Task<IReadOnlyDictionary<ProductId, CheckoutArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<ProductId, CheckoutArticle>>(ids.ToDictionary(i => i, i => new CheckoutArticle(i, "Thing", Price, true, Stock, null)));
    }
    private sealed class Publisher : IDomainEventPublisher
    {
        public readonly List<IDomainEvent> Published = new();
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously), Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _armed;
        public void Arm() => _armed = true;
        public Task PublishAsync(IDomainEvent e, CancellationToken cancellationToken = default) { Published.Add(e); return Task.CompletedTask; }
        public async Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
        {
            if (_armed) { _armed = false; Entered.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
            Published.AddRange(aggregate.DomainEvents); aggregate.ClearDomainEvents();
        }
    }
}
