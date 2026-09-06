using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Application.GetCartMergeOptions;

/// <summary>
/// Decides whether the visitor has a choice to make. They have one only when both carts hold items and the two
/// identities really differ — after a registration the identity is preserved, so there is only ever one cart.
/// </summary>
public sealed class GetCartMergeOptionsUseCase : IGetCartMergeOptionsInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IArticleDataPort _articleData;

    public GetCartMergeOptionsUseCase(IShoppingCartRepository carts, IArticleDataPort articleData)
    {
        _carts = carts;
        _articleData = articleData;
    }

    public async Task<GetCartMergeOptionsResult> ExecuteAsync(
        GetCartMergeOptionsQuery query, CancellationToken cancellationToken = default)
    {
        var anonymousCustomerId = CustomerId.Of(query.AnonymousUserId);
        var registeredCustomerId = CustomerId.Of(query.RegisteredUserId);

        if (anonymousCustomerId == registeredCustomerId)
        {
            return GetCartMergeOptionsResult.NoMergeRequired();
        }

        var anonymousCart = await _carts.FindActiveByCustomerAsync(anonymousCustomerId, cancellationToken)
            .ConfigureAwait(false);
        var accountCart = await _carts.FindActiveByCustomerAsync(registeredCustomerId, cancellationToken)
            .ConfigureAwait(false);

        if (anonymousCart is not { IsEmpty: false } || accountCart is not { IsEmpty: false })
        {
            return GetCartMergeOptionsResult.NoMergeRequired();
        }

        var productIds = anonymousCart.Items
            .Concat(accountCart.Items)
            .Select(i => i.ProductId)
            .ToHashSet();

        var articles = await _articleData.GetArticleDataAsync(productIds, cancellationToken).ConfigureAwait(false);

        return GetCartMergeOptionsResult.Required(Summarize(anonymousCart, articles), Summarize(accountCart, articles));
    }

    private static GetCartMergeOptionsResult.CartSummary Summarize(
        ShoppingCart cart, IReadOnlyDictionary<ProductId, CartArticle> articles) =>
        new(
            cart.Id.Value,
            cart.ItemCount,
            cart.TotalQuantity,
            cart.CalculateTotal().ToString(),
            cart.Items.Select(item => ToItemSummary(item, articles)).ToList());

    private static GetCartMergeOptionsResult.CartItemSummary ToItemSummary(
        CartItem item, IReadOnlyDictionary<ProductId, CartArticle> articles)
    {
        // A product the catalog no longer answers for still has to be shown, or the visitor would be asked to
        // choose between carts one of which silently lost a line.
        var article = articles.GetValueOrDefault(item.ProductId);
        return new GetCartMergeOptionsResult.CartItemSummary(
            item.ProductId.Value,
            article?.Name ?? item.ProductId.Value.ToString(),
            article?.ImageUrl,
            item.Quantity.Value,
            item.PriceAtAddition.Value.ToString());
    }
}
