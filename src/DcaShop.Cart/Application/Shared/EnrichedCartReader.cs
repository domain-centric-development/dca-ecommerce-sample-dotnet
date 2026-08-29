using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.Shared;

/// <summary>Loads the article data for a cart and hands both to the <see cref="EnrichedCartFactory"/>.</summary>
public sealed class EnrichedCartReader
{
    private readonly IArticleDataPort _articles;
    private readonly EnrichedCartFactory _factory;

    public EnrichedCartReader(IArticleDataPort articles, EnrichedCartFactory factory)
    {
        _articles = articles;
        _factory = factory;
    }

    public async Task<EnrichedCart> ReadAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var ids = cart.Items.Select(i => i.ProductId).Distinct().ToArray();
        var articles = await _articles.GetArticleDataAsync(ids, cancellationToken).ConfigureAwait(false);
        return _factory.Create(cart, articles);
    }
}
