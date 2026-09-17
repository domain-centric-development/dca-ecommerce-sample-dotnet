namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;

public sealed record SubmitBuyerInfoCommand(Guid SessionId, string CustomerId, string Email, string FirstName, string LastName, string Phone);
