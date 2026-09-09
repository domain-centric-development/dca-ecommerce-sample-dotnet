namespace DcaShop.Checkout.Domain.Model;

public sealed class CheckoutValidationException : InvalidOperationException
{
    public CheckoutValidationResult Validation { get; }
    public CheckoutValidationException(CheckoutValidationResult validation) : base("Checkout validation failed: " + string.Join("; ", validation.Errors.Select(e => e.Message))) { Validation = validation; }
}
