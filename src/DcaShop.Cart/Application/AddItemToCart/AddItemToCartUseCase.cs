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
    private readonly IUnitOfWork _unitOfWork;

    public AddItemToCartUseCase(IShoppingCartRepository carts, IArticleDataPort articles, IDomainEventPublisher events, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _carts = carts;
        _articles = articles;
        _events = events;
    }

    public async Task<AddItemToCartResult> ExecuteAsync(AddItemToCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var productId = new ProductId(command.ProductId);
        var quantity = Quantity.Of(command.Quantity);

        // Remote-capable read (Product Catalog via ACL) — outside the unit of work
        var article = await _articles.GetArticleDataAsync(productId, cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Product not found: {productId}", nameof(command));
        if (!article.HasStockFor(quantity.Value))
        {
            throw new InvalidOperationException($"Insufficient stock for product: {productId}");
        }

        var priceAtAddition = Price.Of(article.CurrentPrice);

        // Short unit of work: load, mutate, save, publish
        return await _unitOfWork.RunAsync(
            async ct =>
            {
                var cart = await _carts.FindByIdAsync(cartId, ct).ConfigureAwait(false)
                           ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));
                cart.AddItem(productId, quantity, priceAtAddition);
                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                return new AddItemToCartResult(cart.Id.Value, cart.ItemCount, cart.TotalQuantity, cart.CalculateTotal().ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}
