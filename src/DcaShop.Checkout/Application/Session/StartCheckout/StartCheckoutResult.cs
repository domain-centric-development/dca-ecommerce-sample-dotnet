using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.Session.StartCheckout;

/// <summary>What starting a checkout came to: started, or one of the refusals the cart page answers.</summary>
[GenerateOneOf]
public sealed partial class StartCheckoutResult
    : OneOfBase<
        StartCheckoutResult.Started,
        StartCheckoutResult.CartNotAvailable,
        StartCheckoutResult.CartNotActive,
        StartCheckoutResult.EmptyCart,
        StartCheckoutResult.ArticleNotAvailable,
        StartCheckoutResult.CheckoutItemsUnavailable>
{
    /// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
    public sealed record Started(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
    {
        public static Started From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
    }

    /// <summary>The caller has no cart under that id — it does not exist or is somebody else's, told apart by nothing.</summary>
    public sealed record CartNotAvailable(CartId CartId) : IRefusal;

    /// <summary>The cart is no longer active and cannot be checked out.</summary>
    public sealed record CartNotActive(CartId CartId) : IRefusal;

    /// <summary>The cart holds nothing to check out.</summary>
    public sealed record EmptyCart(CartId CartId) : IRefusal;

    /// <summary>The assortment carries no article for a product of the cart.</summary>
    public sealed record ArticleNotAvailable(ProductId ProductId) : IRefusal;

    /// <summary>Products of the cart are unavailable or short in stock right now; they are named.</summary>
    public sealed record CheckoutItemsUnavailable(IReadOnlyList<ProductId> ProductIds) : IRefusal;
}