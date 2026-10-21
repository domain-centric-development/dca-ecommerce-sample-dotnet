namespace DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;

/// <summary>
/// Resumes the open checkout of <paramref name="CustomerId"/>'s active cart. Which cart that is, the Cart context
/// says — so a checkout a guest started follows the cart once it was handed over to the account at a login.
/// </summary>
public sealed record ResumeCheckoutCommand(string CustomerId);