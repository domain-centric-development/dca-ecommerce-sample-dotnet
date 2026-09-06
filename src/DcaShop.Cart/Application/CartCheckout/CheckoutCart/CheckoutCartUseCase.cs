using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CheckoutCart;

/// <summary>
/// Hands a cart over to checkout: reads the cart, fetches current article data, and only then — inside the
/// transaction — validates availability and stock and marks the cart checked out. A cart holding an article
/// that is gone, or more of it than the stock covers, is refused with a <see cref="CartValidationException"/>;
/// a cart that is empty or no longer active is refused by the aggregate, which names the reason itself.
/// </summary>
public sealed class CheckoutCartUseCase : ICheckoutCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IArticleDataPort _articles;
    private readonly EnrichedCartFactory _enrichedCarts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public CheckoutCartUseCase(
        IShoppingCartRepository carts,
        IArticleDataPort articles,
        EnrichedCartFactory enrichedCarts,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary)
    {
        _carts = carts;
        _articles = articles;
        _enrichedCarts = enrichedCarts;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<CheckoutCartResult> ExecuteAsync(CheckoutCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var customerId = CustomerId.Of(command.CustomerId);

        // Which articles does this cart need? Read the cart once and fetch the article data of other contexts
        // before the unit of work — neither call may sit inside the transaction (ADR-004).
        var current = await _carts.FindByIdForCustomerAsync(cartId, customerId, cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));
        var productIds = current.Items.Select(i => i.ProductId).Distinct().ToArray();
        var articleData = await _articles.GetArticleDataAsync(productIds, cancellationToken).ConfigureAwait(false);

        // Short transaction: reload, validate, check out, save, publish
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var cart = await _carts.FindByIdForCustomerAsync(cartId, customerId, ct).ConfigureAwait(false)
                           ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

                var enrichedCart = _enrichedCarts.Create(cart, articleData);
                if (!enrichedCart.IsValidForCheckout)
                {
                    var validation = cart.ValidateForCheckout(new ArticleDataPriceResolver(articleData));
                    if (!validation.IsValid)
                    {
                        throw new CartValidationException(validation);
                    }

                    // Nothing is wrong with the articles: the cart itself is in no state to be checked out
                    // (empty, or no longer active). Let the aggregate say so in its own words.
                }

                cart.Checkout();

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new CheckoutCartResult(cart.Id.Value, cart.Status.ToString(), enrichedCart.CurrentSubtotal.ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}
