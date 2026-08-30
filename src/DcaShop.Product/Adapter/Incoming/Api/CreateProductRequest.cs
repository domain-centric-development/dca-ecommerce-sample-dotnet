using System.ComponentModel.DataAnnotations;

namespace DcaShop.Product.Adapter.Incoming.Api;

public sealed record CreateProductRequest(
    [Required] string Sku,
    [Required] string Name,
    string? Description,
    string? ImageUrl,
    [Range(0.01, double.MaxValue)] decimal Price,
    [Required] string Category,
    [Range(0, int.MaxValue)] int Stock);
