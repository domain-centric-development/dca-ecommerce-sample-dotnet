using DcaShop.Pricing.Application.ChangePrice;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Pricing.Adapter.Incoming.Api;

/// <summary>
/// The price of a catalogue product over HTTP. Changing it is an operator action and demands the staff role.
/// </summary>
/// <remarks>
/// Authenticated by an <c>Authorization: Bearer</c> token and nothing else, like the whole <c>/api/</c> surface
/// (ADR-007). The staff gate is a claims-only check and therefore a property of this exposure, stated as
/// <c>[Authorize]</c> so the pipeline answers <c>401</c> to a stranger and <c>403</c> to a customer (ADR-008).
/// </remarks>
[ApiController]
[Route("api/products/{productId}/price")]
public sealed class ProductPriceResource : ControllerBase
{
    /// <summary>The title of every refusal of a value on this route — here and in <see cref="PricingApiExceptionHandler"/>.</summary>
    internal const string UnacceptableValue = "Unacceptable value";

    private readonly IChangePriceInputPort _changePrice;

    public ProductPriceResource(IChangePriceInputPort changePrice)
    {
        _changePrice = changePrice;
    }

    /// <summary>
    /// Sets the product's price to the one in the body. A product id that does not parse and a price that is not
    /// greater than zero are answered here at the edge; a product without a price travels as
    /// <see cref="ProductPriceNotFoundException"/> to <see cref="PricingApiExceptionHandler"/>.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = Role.StaffCode)]
    public async Task<IActionResult> ChangePrice(
        string productId, [FromBody] ChangePriceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ProductId.TryOf(productId, out var product))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: UnacceptableValue);
        }

        if (request.Price <= 0)
        {
            ModelState.AddModelError("price", "A price is greater than zero.");
            return ValidationProblem(title: UnacceptableValue, modelStateDictionary: ModelState);
        }

        await _changePrice.ExecuteAsync(new ChangePriceCommand(product.Value, request.Price, "EUR"), cancellationToken);
        return NoContent();
    }
}