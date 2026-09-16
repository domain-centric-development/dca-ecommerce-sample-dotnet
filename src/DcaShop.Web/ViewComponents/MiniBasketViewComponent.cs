using DcaShop.Cart.Api;
using DcaShop.Account.Api;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Web.ViewComponents;

/// <summary>
/// Header mini basket: the visitor's active cart through the Cart context's published API. It never creates a
/// cart — an empty header must not be the reason one exists.
/// </summary>
public sealed class MiniBasketViewComponent : ViewComponent
{
    private readonly CartService _carts;
    private readonly IIdentityService _identityService;

    public MiniBasketViewComponent(CartService carts, IIdentityService identityService)
    {
        _carts = carts;
        _identityService = identityService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customerId = _identityService.CurrentIdentity().UserId.Value;
        return View(await _carts.FindMiniBasketAsync(customerId, HttpContext.RequestAborted));
    }
}
