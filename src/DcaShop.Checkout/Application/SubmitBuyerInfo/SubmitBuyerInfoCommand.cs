namespace DcaShop.Checkout.Application.SubmitBuyerInfo;

public sealed record SubmitBuyerInfoCommand(Guid SessionId, string Email, string FirstName, string LastName, string Phone);
