using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Domain.Service;

namespace DcaShop.Cart.Application.Shopping.GetCartById;

public sealed class GetCartByIdUseCase : IGetCartByIdInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly EnrichedCartReader _reader;
    private readonly CartTotalCalculator _totalCalculator;

    public GetCartByIdUseCase(IShoppingCartRepository carts, EnrichedCartReader reader, CartTotalCalculator totalCalculator)
    {
        _carts = carts;
        _reader = reader;
        _totalCalculator = totalCalculator;
    }

    public async Task<GetCartByIdResult> ExecuteAsync(GetCartByIdQuery query, CancellationToken cancellationToken = default)
    {
        var cart = await _carts
            .FindByIdForCustomerAsync(new CartId(query.CartId), CustomerId.Of(query.CustomerId), cancellationToken)
            .ConfigureAwait(false);
        if (cart is null)
        {
            return GetCartByIdResult.NotFound();
        }

        var enriched = await _reader.ReadAsync(cart, cancellationToken).ConfigureAwait(false);
        return new GetCartByIdResult(enriched, CartTotals.From(enriched, _totalCalculator));
    }
}
