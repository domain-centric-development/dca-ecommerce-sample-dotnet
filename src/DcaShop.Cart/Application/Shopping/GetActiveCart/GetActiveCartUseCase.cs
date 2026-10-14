using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.Shopping.GetActiveCart;

/// <summary>
/// Read use case behind the cart page and the mini basket: the active cart of a customer, enriched with its totals, or
/// nothing — it never opens one.
/// </summary>
public sealed class GetActiveCartUseCase : IGetActiveCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly EnrichedCartReader _reader;

    public GetActiveCartUseCase(IShoppingCartRepository carts, EnrichedCartReader reader)
    {
        _carts = carts;
        _reader = reader;
    }

    public async Task<GetActiveCartResult> ExecuteAsync(GetActiveCartQuery query, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.FindActiveByCustomerAsync(CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        if (cart is null)
        {
            return new GetActiveCartResult(null);
        }

        var enriched = await _reader.ReadAsync(cart, cancellationToken).ConfigureAwait(false);
        return new GetActiveCartResult(enriched, CartTotals.From(enriched));
    }
}