using DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;
using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging.Abstractions;

namespace DcaShop.UnitTests.Cart;

/// <summary>
/// Delivery is at least once: the consumer deduplicates on the event id, so the same completion trigger delivered
/// twice completes the cart once. The cart's own reconciliation tolerates a replay as well; this pins that the
/// consumer does not even ask it twice.
/// </summary>
public sealed class CartCompletionEventConsumerTest
{
    [Fact]
    public async Task ARedeliveredCompletionTriggerCompletesTheCartOnce()
    {
        var completeCart = new CountingCompleteCart();
        IEventListener consumer = new CartCompletionEventConsumer(completeCart, new InMemoryProcessedEvents(), NullLogger<CartCompletionEventConsumer>.Instance);
        var trigger = new CompletionTrigger(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), "session-1", ["position-1:1-2"]);

        await consumer.OnAsync(trigger);
        await consumer.OnAsync(trigger);

        Assert.Equal(1, completeCart.Executions);
    }

    [Fact]
    public async Task TwoDistinctCompletionTriggersCompleteTheCartTwice()
    {
        var completeCart = new CountingCompleteCart();
        IEventListener consumer = new CartCompletionEventConsumer(completeCart, new InMemoryProcessedEvents(), NullLogger<CartCompletionEventConsumer>.Instance);
        var cartId = Guid.NewGuid().ToString();

        await consumer.OnAsync(new CompletionTrigger(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, "session-1", ["position-1:1-2"]));
        await consumer.OnAsync(new CompletionTrigger(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, "session-2", ["position-2:1-1"]));

        Assert.Equal(2, completeCart.Executions);
    }

    private sealed record CompletionTrigger(
        Guid EventId,
        DateTimeOffset OccurredOn,
        string CartId,
        string SessionId,
        IReadOnlyList<string> PurchasedPositions) : ICartCompletionTrigger;

    private sealed class CountingCompleteCart : ICompleteCartInputPort
    {
        public int Executions { get; private set; }

        public Task<CompleteCartResult> ExecuteAsync(CompleteCartCommand input, CancellationToken cancellationToken = default)
        {
            Executions++;
            return Task.FromResult(new CompleteCartResult(input.CartId, "Active"));
        }
    }
}