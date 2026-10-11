using DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;
using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.Logging.Abstractions;

namespace DcaShop.UnitTests.Cart;

/// <summary>
/// Delivery is at least once. The consumer only hands the event id on; <see cref="CompleteCartUseCase"/> deduplicates
/// per effect inside its transaction, so the same completion trigger delivered twice completes the cart once.
/// </summary>
public sealed class CartCompletionRedeliveryTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    [Fact]
    public async Task TheConsumerHandsTheEventIdToTheUseCase()
    {
        var completeCart = new RecordingCompleteCart();
        IEventListener consumer = new CartCompletionEventConsumer(completeCart, NullLogger<CartCompletionEventConsumer>.Instance);
        var trigger = new CompletionTrigger(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), "session-1", ["position-1:1-2"]);

        await consumer.OnAsync(trigger);

        Assert.Equal(trigger.EventId, Assert.Single(completeCart.Commands).EventId);
    }

    [Fact]
    public async Task ARedeliveredCompletionCompletesTheCartOnce()
    {
        var (useCase, publisher, command) = await CartReadyForCompletionAsync();

        var first = await useCase.ExecuteAsync(command);
        var second = await useCase.ExecuteAsync(command);

        Assert.False(first.AlreadyCompleted);
        Assert.True(second.AlreadyCompleted);
        Assert.Equal(1, publisher.Publications);
    }

    [Fact]
    public async Task AnotherEffectOfTheSameEventIsNotTakenForADuplicate()
    {
        var processedEvents = new InMemoryProcessedEvents();
        var eventId = Guid.NewGuid();

        await processedEvents.MarkProcessedAsync("complete-cart", eventId);

        Assert.True(await processedEvents.ContainsAsync("complete-cart", eventId));
        Assert.False(await processedEvents.ContainsAsync("another-effect", eventId));
    }

    private static async Task<(CompleteCartUseCase UseCase, CountingPublisher Publisher, CompleteCartCommand Command)> CartReadyForCompletionAsync()
    {
        var carts = new InMemoryShoppingCartRepository();
        var cart = ShoppingCart.Create(CartId.Generate(), CustomerId.Of("customer-1"));
        cart.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);
        cart.ClearDomainEvents();
        await carts.SaveAsync(cart);
        var publisher = new CountingPublisher();
        var useCase = new CompleteCartUseCase(carts, new InMemoryProcessedEvents(), publisher, new InMemoryTransactionBoundary());
        var command = new CompleteCartCommand(cart.Id.Value, "session-1", [cart.Items[0].PositionSnapshot], Guid.NewGuid());
        return (useCase, publisher, command);
    }

    private sealed record CompletionTrigger(
        Guid EventId,
        DateTimeOffset OccurredOn,
        string CartId,
        string SessionId,
        IReadOnlyList<string> PurchasedPositions) : ICartCompletionTrigger;

    private sealed class RecordingCompleteCart : ICompleteCartInputPort
    {
        public List<CompleteCartCommand> Commands { get; } = [];

        public Task<CompleteCartResult> ExecuteAsync(CompleteCartCommand input, CancellationToken cancellationToken = default)
        {
            Commands.Add(input);
            return Task.FromResult(new CompleteCartResult(input.CartId, "Active"));
        }
    }

    private sealed class CountingPublisher : IDomainEventPublisher
    {
        public int Publications { get; private set; }

        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
        {
            Publications++;
            aggregate.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }
}