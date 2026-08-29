using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.GetCartById;

public sealed class GetCartByIdUseCase : IGetCartByIdInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly EnrichedCartReader _reader;

    public GetCartByIdUseCase(IShoppingCartRepository carts, EnrichedCartReader reader)
    {
        _carts = carts;
        _reader = reader;
    }

    public async Task<GetCartByIdResult> ExecuteAsync(GetCartByIdQuery query, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.FindByIdAsync(new CartId(query.CartId), cancellationToken).ConfigureAwait(false);
        if (cart is null)
        {
            return new GetCartByIdResult(null);
        }

        return new GetCartByIdResult(await _reader.ReadAsync(cart, cancellationToken).ConfigureAwait(false));
    }
}
