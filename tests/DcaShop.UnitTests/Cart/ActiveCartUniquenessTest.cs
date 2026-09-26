using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.UnitTests.Cart;

/// <summary>
/// A customer has at most one active cart, and the store is what holds that.
/// </summary>
/// <remarks>
/// The rule spans carts, so no single aggregate can enforce it, and "look, then create" cannot either — two
/// requests that both find nothing both create one. The store claims the customer the way a unique index does,
/// which is also what makes this adapter teach the same contract as a relational one.
/// </remarks>
public sealed class ActiveCartUniquenessTest
{
    private const int ConcurrentRequests = 8;
    private const int Rounds = 25;

    [Fact]
    public async Task ASecondActiveCartIsRefused()
    {
        var carts = new InMemoryShoppingCartRepository();
        var customer = CustomerId.Of($"customer-{Guid.NewGuid()}");
        await carts.SaveAsync(new ShoppingCart(CartId.Generate(), customer));

        await Assert.ThrowsAsync<ActiveCartAlreadyExistsException>(() =>
            carts.SaveAsync(new ShoppingCart(CartId.Generate(), customer)));
    }

    [Fact]
    public async Task TheCustomerIsFreeAgainOnceTheCartIsDone()
    {
        var carts = new InMemoryShoppingCartRepository();
        var customer = CustomerId.Of($"customer-{Guid.NewGuid()}");
        var first = new ShoppingCart(CartId.Generate(), customer);
        await carts.SaveAsync(first);

        first.Complete();
        await carts.SaveAsync(first);

        var second = new ShoppingCart(CartId.Generate(), customer);
        await carts.SaveAsync(second);

        Assert.Equal(second.Id, (await carts.FindActiveByCustomerAsync(customer))!.Id);
    }

    [Fact]
    public async Task ConcurrentRequestsShareOneCart()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var carts = new InMemoryShoppingCartRepository();
            var customer = $"customer-{Guid.NewGuid()}";

            using var startLine = new Barrier(ConcurrentRequests);
            var answers = await Task.WhenAll(Enumerable.Range(0, ConcurrentRequests).Select(_ => Task.Run(async () =>
            {
                // One boundary per request, as the application scopes it; the store is the one they share.
                var useCase = new GetOrCreateActiveCartUseCase(carts, new SilentPublisher(), new InMemoryTransactionBoundary());
                startLine.SignalAndWait();
                var result = await useCase.ExecuteAsync(new GetOrCreateActiveCartCommand(customer));
                return result.CartId;
            })));

            Assert.Single(answers.Distinct());
            Assert.Single(await carts.FindByCustomerAsync(CustomerId.Of(customer)));
            Assert.NotNull(await carts.FindActiveByCustomerAsync(CustomerId.Of(customer)));
        }
    }

    private sealed class SilentPublisher : IDomainEventPublisher
    {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
        {
            aggregate.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }
}