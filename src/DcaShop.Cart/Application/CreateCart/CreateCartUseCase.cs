using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CreateCart;

public sealed class CreateCartUseCase : ICreateCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;

    public CreateCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events)
    {
        _carts = carts;
        _events = events;
    }

    public async Task<CreateCartResult> ExecuteAsync(CreateCartCommand command, CancellationToken cancellationToken = default)
    {
        var cart = new ShoppingCart(CartId.Generate(), CustomerId.Of(command.CustomerId));
        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);
        return new CreateCartResult(cart.Id.Value, cart.CustomerId.Value);
    }
}
