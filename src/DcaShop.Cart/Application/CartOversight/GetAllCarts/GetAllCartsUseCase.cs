using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.CartOversight.GetAllCarts;

/// <summary>
/// Lists every cart in the shop. This crosses customer boundaries by definition, so the adapter exposing it has
/// to demand an operator role — the use case itself states no such rule, it only answers the question.
/// </summary>
/// <remarks>
/// Each cart is totalled at today's prices, as every answer about one cart is. The article data is asked once for
/// the products of all carts, not once per cart.
/// </remarks>
public sealed class GetAllCartsUseCase : IGetAllCartsInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IArticleDataPort _articles;
    private readonly EnrichedCartFactory _factory;

    public GetAllCartsUseCase(IShoppingCartRepository carts, IArticleDataPort articles, EnrichedCartFactory factory)
    {
        _carts = carts;
        _articles = articles;
        _factory = factory;
    }

    public async Task<GetAllCartsResult> ExecuteAsync(GetAllCartsQuery query, CancellationToken cancellationToken = default)
    {
        var carts = await _carts.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var productIds = carts.SelectMany(cart => cart.Items).Select(item => item.ProductId).Distinct().ToArray();
        var articles = await _articles.GetArticleDataAsync(productIds, cancellationToken).ConfigureAwait(false);
        var summaries = carts
            .Select(cart =>
            {
                var enriched = _factory.Create(cart, articles);
                return new GetAllCartsResult.CartSummary(
                    cart.Id.Value,
                    cart.CustomerId.Value,
                    cart.Status,
                    cart.ItemCount,
                    enriched.CurrentSubtotal);
            })
            .ToList();

        return new GetAllCartsResult(summaries);
    }
}