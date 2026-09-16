using DcaShop.Cart.Api;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Web.ViewComponents;

/// <summary>
/// Header mini basket: the visitor's active cart through the Cart context's published API. It never creates a
/// cart — an empty header must not be the reason one exists.
/// </summary>
public sealed class MiniBasketViewComponent : ViewComponent
{
    private readonly CartService _carts;
    private readonly IIdentityProvider _identityProvider;

    public MiniBasketViewComponent(CartService carts, IIdentityProvider identityProvider)
    {
        _carts = carts;
        _identityProvider = identityProvider;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customerId = _identityProvider.GetCurrentIdentity().UserId.Value;
        return View(await _carts.FindMiniBasketAsync(customerId, HttpContext.RequestAborted));
    }
}
