using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Result of validating the line items against current price and availability data.</summary>
public sealed record CheckoutValidationResult(IReadOnlyList<ValidationError> Errors) : IValue
{
    public bool IsValid => Errors.Count == 0;

    public static CheckoutValidationResult Valid() => new(Array.Empty<ValidationError>());

    public static CheckoutValidationResult WithErrors(IReadOnlyList<ValidationError> errors) => new(errors);
}

public enum ErrorType
{
    ProductUnavailable,
    InsufficientStock,
}

public sealed record ValidationError(ProductId ProductId, ErrorType Type, string Message) : IValue
{
    public static ValidationError ProductUnavailable(ProductId productId) =>
        new(productId, ErrorType.ProductUnavailable, $"Product {productId} is not available");

    public static ValidationError InsufficientStock(ProductId productId, int requested, int available) =>
        new(productId, ErrorType.InsufficientStock, $"Product {productId}: requested {requested}, only {available} available");
}
