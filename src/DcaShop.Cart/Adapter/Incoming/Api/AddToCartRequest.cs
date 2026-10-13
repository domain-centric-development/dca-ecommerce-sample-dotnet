using System.ComponentModel.DataAnnotations;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>A line to add. The product id is taken as sent: one the catalogue does not know is answered, not rejected.</summary>
public sealed record AddToCartRequest(
    [Required] string ProductId,
    [Range(1, int.MaxValue)] int Quantity);