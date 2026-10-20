using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Result of validating the line items against current price and availability data.</summary>
public sealed record CheckoutValidationResult(IReadOnlyList<ValidationError> Errors) : IValue
{
    public bool IsValid => Errors.Count == 0;

    public static CheckoutValidationResult Valid() => new(Array.Empty<ValidationError>());

    public static CheckoutValidationResult WithErrors(IReadOnlyList<ValidationError> errors) => new(errors);

    /// <summary>
    /// The line a refusal names: the first, in the order's line order, that the stock no longer covers — unavailable or
    /// short; none when only prices changed.
    /// </summary>
    public ValidationError? ShortOfStock =>
        Errors.FirstOrDefault(error => error.Type is ErrorType.ProductUnavailable or ErrorType.InsufficientStock);
}

public enum ErrorType
{
    ProductUnavailable,
    InsufficientStock,
    PriceChanged,
}

/// <summary>
/// What failed on one line: the product, by its id and by the name the line carries, the failed rule, and a message
/// for the log.
/// </summary>
public sealed record ValidationError(ProductId ProductId, ErrorType Type, string Message, string ProductName) : IValue
{
    public static ValidationError ProductUnavailable(ProductId productId, string productName) =>
        new(productId, ErrorType.ProductUnavailable, $"Product {productId} is not available", productName);

    public static ValidationError InsufficientStock(ProductId productId, int requested, int available, string productName) =>
        new(productId, ErrorType.InsufficientStock, $"Product {productId}: requested {requested}, only {available} available", productName);
}