using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.GetOrCreateActiveCart;

public sealed class GetOrCreateActiveCartUseCase : IGetOrCreateActiveCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;

    public GetOrCreateActiveCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events)
    {
        _carts = carts;
        _events = events;
    }

    public async Task<GetOrCreateActiveCartResult> ExecuteAsync(GetOrCreateActiveCartCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = CustomerId.Of(command.CustomerId);
        var existing = await _carts.FindActiveByCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return new GetOrCreateActiveCartResult(existing.Id.Value, Created: false);
        }

        var cart = new ShoppingCart(CartId.Generate(), customerId);
        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);
        return new GetOrCreateActiveCartResult(cart.Id.Value, Created: true);
    }
}
