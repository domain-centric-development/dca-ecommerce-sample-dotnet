using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>
/// What stands between a cart and its checkout: the collected reasons why it cannot be settled — an article that
/// is no longer for sale, or a quantity the stock no longer covers. An empty list means the cart may proceed.
/// </summary>
public sealed record CartValidationResult : IValue
{
    public CartValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = (errors ?? throw new ArgumentNullException(nameof(errors))).ToList();
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static CartValidationResult Valid() => new(Array.Empty<ValidationError>());

    public static CartValidationResult WithErrors(IReadOnlyList<ValidationError> errors) => new(errors);

    /// <summary>One reason a single product keeps the cart from checkout.</summary>
    public sealed record ValidationError : IValue
    {
        public ValidationError(ProductId productId, string message, ValidationErrorType type)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be blank", nameof(message));
            }

            ProductId = productId;
            Message = message;
            Type = type;
        }

        public ProductId ProductId { get; }

        public string Message { get; }

        public ValidationErrorType Type { get; }

        public static ValidationError ProductUnavailable(ProductId productId) =>
            new(productId, $"Product is not available: {productId.Value}", ValidationErrorType.ProductUnavailable);

        public static ValidationError InsufficientStock(ProductId productId, int requested, int available) =>
            new(
                productId,
                $"Insufficient stock for product {productId.Value}: requested {requested}, available {available}",
                ValidationErrorType.InsufficientStock);
    }
}

/// <summary>The kind of reason a cart cannot be checked out.</summary>
public enum ValidationErrorType
{
    /// <summary>The product is not for sale any more.</summary>
    ProductUnavailable,

    /// <summary>The stock does not cover the requested quantity.</summary>
    InsufficientStock,
}
