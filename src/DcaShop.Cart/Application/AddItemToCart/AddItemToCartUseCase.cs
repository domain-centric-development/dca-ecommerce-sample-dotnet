using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.AddItemToCart;

public sealed class AddItemToCartUseCase : IAddItemToCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IArticleDataPort _articles;
    private readonly IDomainEventPublisher _events;

    public AddItemToCartUseCase(IShoppingCartRepository carts, IArticleDataPort articles, IDomainEventPublisher events)
    {
        _carts = carts;
        _articles = articles;
        _events = events;
    }

    public async Task<AddItemToCartResult> ExecuteAsync(AddItemToCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var productId = new ProductId(command.ProductId);
        var quantity = Quantity.Of(command.Quantity);

        var cart = await _carts.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

        var article = await _articles.GetArticleDataAsync(productId, cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Product not found: {productId}", nameof(command));

        if (!article.HasStockFor(quantity.Value))
        {
            throw new InvalidOperationException($"Insufficient stock for product: {productId}");
        }

        cart.AddItem(productId, quantity, Price.Of(article.CurrentPrice));

        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);

        return new AddItemToCartResult(cart.Id.Value, cart.ItemCount, cart.TotalQuantity, cart.CalculateTotal().ToString());
    }
}
