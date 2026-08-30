using DcaShop.Cart.Adapter.Incoming.Web;
using DcaShop.Cart.Api;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Web.ViewComponents;

/// <summary>Header mini basket: the guest's active cart through the Cart context's published API; never creates a cart.</summary>
public sealed class MiniBasketViewComponent : ViewComponent
{
    private readonly CartService _carts;

    public MiniBasketViewComponent(CartService carts)
    {
        _carts = carts;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        CartService.MiniBasket? basket = null;
        if (HttpContext.Request.Cookies.TryGetValue(GuestCustomer.CookieName, out var customerId) && !string.IsNullOrWhiteSpace(customerId))
        {
            basket = await _carts.FindMiniBasketAsync(customerId, HttpContext.RequestAborted);
        }

        return View(basket);
    }
}
