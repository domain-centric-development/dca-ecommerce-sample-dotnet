using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>
/// Adds a product to one of the caller's carts. The article facts are read outside the transaction, because the port
/// may reach another context; inside one short transaction the cart is loaded, the line it would hold is checked
/// against the stock, the item is added, the cart saved and its events published.
/// </summary>
/// <remarks>
/// Every refusal is returned as a case of <see cref="AddItemToCartResult"/>, the aggregate's caught once here. Each one
/// is decided before the transaction writes anything, so the transaction that returns it commits nothing.
/// </remarks>
public sealed class AddItemToCartUseCase : IAddItemToCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IArticleDataPort _articles;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public AddItemToCartUseCase(IShoppingCartRepository carts, IArticleDataPort articles, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _articles = articles;
        _events = events;
    }

    public async Task<AddItemToCartResult> ExecuteAsync(AddItemToCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var productId = new ProductId(command.ProductId);
        var quantity = Quantity.Of(command.Quantity);

        // Remote-capable read (Product Catalog via ACL) — outside the transaction
        var article = await _articles.GetArticleDataAsync(productId, cancellationToken).ConfigureAwait(false);
        if (article is null)
        {
            return new AddItemToCartResult.ArticleNotAvailable(productId);
        }

        var priceAtAddition = Price.Of(article.CurrentPrice);

        // Short transaction: load, check what the line would hold against the article facts read above, mutate,
        // save, publish
        return await _transactionBoundary.InTransactionAsync<AddItemToCartResult>(
            async ct =>
            {
                var cart = await _carts.FindByIdForCustomerAsync(cartId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false);
                if (cart is null)
                {
                    return new AddItemToCartResult.CartNotFound(cartId);
                }

                var held = cart.QuantityOf(productId);
                if (!article.HasStockFor(held + quantity.Value))
                {
                    return new AddItemToCartResult.InsufficientArticleStock(productId, quantity.Value, held, article.AvailableStock);
                }

                try
                {
                    cart.AddItem(productId, quantity, priceAtAddition);
                }
                catch (CartNotModifiableException notModifiable)
                {
                    return new AddItemToCartResult.CartNotModifiable(notModifiable.CartId, notModifiable.Status);
                }

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                return new AddItemToCartResult.Added(cart.Id.Value, cart.ItemCount, cart.TotalQuantity, cart.CalculateTotal());
            },
            cancellationToken).ConfigureAwait(false);
    }
}