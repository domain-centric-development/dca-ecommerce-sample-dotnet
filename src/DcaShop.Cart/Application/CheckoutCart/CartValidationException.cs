using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.CheckoutCart;

/// <summary>
/// The cart cannot be settled: an article it holds is no longer for sale, or the stock no longer covers the
/// quantity. Carries the <see cref="CartValidationResult"/> so the caller can name every offending line.
/// </summary>
public sealed class CartValidationException : Exception
{
    public CartValidationException(CartValidationResult validationResult)
        : base($"Cart validation failed: {validationResult.Errors.Count} error(s)")
    {
        ValidationResult = validationResult;
    }

    public CartValidationResult ValidationResult { get; }
}
