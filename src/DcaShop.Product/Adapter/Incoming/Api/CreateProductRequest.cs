using System.ComponentModel.DataAnnotations;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// The body of a product creation. A field its attributes refuse is answered with the attribute's message as the
/// reason (<see cref="UnacceptableValueProblemAttribute"/>).
/// </summary>
public sealed record CreateProductRequest(
    [Required] string Sku,
    [Required(ErrorMessage = "A name is required.")] string Name,
    string? Description,
    string? ImageUrl,
    [Range(0d, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "The price must be greater than zero.")] decimal Price,
    [Required] string Category,
    [Range(0, int.MaxValue, ErrorMessage = "The initial stock must not be negative.")] int Stock);