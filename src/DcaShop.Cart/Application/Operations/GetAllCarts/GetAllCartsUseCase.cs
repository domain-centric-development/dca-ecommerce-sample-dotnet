using DcaShop.Cart.Application.Shared;

namespace DcaShop.Cart.Application.GetAllCarts;

/// <summary>
/// Lists every cart in the shop. This crosses customer boundaries by definition, so the adapter exposing it has
/// to demand an operator role — the use case itself states no such rule, it only answers the question.
/// </summary>
public sealed class GetAllCartsUseCase : IGetAllCartsInputPort
{
    private readonly IShoppingCartRepository _carts;

    public GetAllCartsUseCase(IShoppingCartRepository carts)
    {
        _carts = carts;
    }

    public async Task<GetAllCartsResult> ExecuteAsync(GetAllCartsQuery query, CancellationToken cancellationToken = default)
    {
        var carts = await _carts.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var summaries = carts
            .Select(cart =>
            {
                var total = cart.CalculateTotal();
                return new GetAllCartsResult.CartSummary(
                    cart.Id.Value,
                    cart.CustomerId.Value,
                    cart.Status.ToString(),
                    cart.ItemCount,
                    total.Amount,
                    total.Currency);
            })
            .ToList();

        return new GetAllCartsResult(summaries);
    }
}
