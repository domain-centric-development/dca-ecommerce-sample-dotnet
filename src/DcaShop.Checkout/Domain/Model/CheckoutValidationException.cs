using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when the items of a checkout no longer pass their own validation at confirmation time.</summary>
/// <remarks>
/// Prices move and stock runs out while a customer fills in the steps. Confirmation re-checks the line items
/// against the facts of that moment, and this exception carries what failed, item by item, so the customer
/// sees which position to change rather than a bare refusal.
/// </remarks>
public sealed class CheckoutValidationException : DomainException
{
    public CheckoutValidationException(CheckoutValidationResult validation)
        : base("Checkout validation failed: " + string.Join("; ", validation.Errors.Select(e => e.Message)))
    {
        Validation = validation;
    }

    public CheckoutValidationResult Validation { get; }
}