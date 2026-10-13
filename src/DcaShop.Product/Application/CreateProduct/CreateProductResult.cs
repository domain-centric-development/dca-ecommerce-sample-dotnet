using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Application.CreateProduct;

/// <summary><see cref="Price"/> is the price the product was created with, as <see cref="Money"/> normalised it.</summary>
public sealed record CreateProductResult(Guid ProductId, string Sku, string Name, Money Price);