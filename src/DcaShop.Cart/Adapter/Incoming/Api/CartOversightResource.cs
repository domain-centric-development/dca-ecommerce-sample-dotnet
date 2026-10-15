using DcaShop.Cart.Application.CartOversight.GetAllCarts;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// Cart oversight over HTTP: every cart in the shop, the staff's view. It is the only route under <c>api/carts</c>
/// that leaves the caller's own data, so it demands the staff role. Authenticated by an
/// <c>Authorization: Bearer</c> token and nothing else (ADR-007).
/// </summary>
[ApiController]
[Route("api/carts")]
public sealed class CartOversightResource : ControllerBase
{
    private readonly IGetAllCartsInputPort _getAllCarts;
    private readonly ShoppingCartDtoConverter _converter;

    public CartOversightResource(IGetAllCartsInputPort getAllCarts, ShoppingCartDtoConverter converter)
    {
        _getAllCarts = getAllCarts;
        _converter = converter;
    }

    [HttpGet]
    [Authorize(Roles = Role.StaffCode)]
    public async Task<ActionResult<ShoppingCartListDto>> GetAllCarts(CancellationToken cancellationToken)
    {
        var result = await _getAllCarts.ExecuteAsync(new GetAllCartsQuery(), cancellationToken);
        return Ok(_converter.ToListDto(result));
    }
}