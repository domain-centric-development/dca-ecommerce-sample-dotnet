using System.ComponentModel.DataAnnotations;

namespace DcaShop.Cart.Adapter.Incoming.Api;

public sealed record AddToCartRequest(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity);
