using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Session.StartCheckout;

/// <summary>
/// Starts a checkout from an active cart. Line items get fresh prices; the cart stays active until confirmation. Every
/// refusal is returned as a case of <see cref="StartCheckoutResult"/>, decided before the transaction opens.
/// </summary>
public sealed class StartCheckoutUseCase : IStartCheckoutInputPort
{
    private readonly ICartDataPort _cartData;
    private readonly CheckoutCartFactory _checkoutCartFactory;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public StartCheckoutUseCase(ICartDataPort cartData, CheckoutCartFactory checkoutCartFactory, ICheckoutArticleDataPort articleData, ICheckoutSessionRepository sessions, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _cartData = cartData;
        _checkoutCartFactory = checkoutCartFactory;
        _articleData = articleData;
        _sessions = sessions;
        _events = events;
    }

    public async Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        // Cart and article data come from other contexts (remote-capable) — outside the transaction
        // Scoped to the caller: a cart that is not theirs is indistinguishable from one that does not exist.
        var cart = await _cartData.FindByIdAsync(cartId, CustomerId.Of(command.CustomerId), cancellationToken).ConfigureAwait(false);
        if (cart is null)
        {
            return new StartCheckoutResult.CartNotAvailable(cartId);
        }

        if (!cart.Active)
        {
            return new StartCheckoutResult.CartNotActive(cartId);
        }

        if (cart.Items.Count == 0)
        {
            return new StartCheckoutResult.EmptyCart(cartId);
        }

        var articles = await _articleData.GetArticleDataAsync(cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var lineItems = new List<CheckoutLineItem>();
        foreach (var cartItem in cart.Items)
        {
            if (!articles.TryGetValue(cartItem.ProductId, out var article))
            {
                return new StartCheckoutResult.ArticleNotAvailable(cartItem.ProductId);
            }

            lineItems.Add(new CheckoutLineItem(CheckoutLineItemId.Generate(), cartItem.ProductId, article.Name, article.CurrentPrice, cartItem.Quantity, article.ImageUrl, cartItem.PositionSnapshot));
        }

        // The enriched read model pairs each line item with its current article data, so the domain can
        // answer availability, stock and pricing questions before a session exists
        var checkoutCart = _checkoutCartFactory.Create(cart.CartId, cart.CustomerId, lineItems, articles);
        if (!checkoutCart.IsValidForCheckout)
        {
            return new StartCheckoutResult.CheckoutItemsUnavailable(checkoutCart.InvalidItems().Select(i => i.LineItem.ProductId).ToList());
        }

        var subtotal = checkoutCart.CalculateCurrentSubtotal();

        // Short transaction: create, save, publish
        return await _sessions.InCartSessionAsync(cartId, () => _transactionBoundary.InTransactionAsync<StartCheckoutResult>(
            async ct =>
            {
                // Decide, then write: the session refuses an empty checkout before the one it replaces is superseded
                CheckoutSession session;
                try
                {
                    session = CheckoutSession.Start(cart.CartId, cart.CustomerId, lineItems, subtotal);
                }
                catch (EmptyCheckoutException)
                {
                    return new StartCheckoutResult.EmptyCart(cartId);
                }

                var previous = await _sessions.FindActiveByCartIdAsync(cartId, ct).ConfigureAwait(false);
                if (previous is not null)
                {
                    previous.Supersede();
                    await _sessions.SaveAsync(previous, ct).ConfigureAwait(false);
                }

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return StartCheckoutResult.Started.From(session);
            },
            cancellationToken), cancellationToken).ConfigureAwait(false);
    }
}