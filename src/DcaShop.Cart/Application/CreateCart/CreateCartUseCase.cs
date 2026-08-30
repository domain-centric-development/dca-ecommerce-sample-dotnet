using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CreateCart;

public sealed class CreateCartUseCase : ICreateCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _carts = carts;
        _events = events;
    }

    public async Task<CreateCartResult> ExecuteAsync(CreateCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short unit of work
        return await _unitOfWork.RunAsync(
            async ct =>
            {
                var cart = new ShoppingCart(CartId.Generate(), CustomerId.Of(command.CustomerId));
                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                return new CreateCartResult(cart.Id.Value, cart.CustomerId.Value);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
