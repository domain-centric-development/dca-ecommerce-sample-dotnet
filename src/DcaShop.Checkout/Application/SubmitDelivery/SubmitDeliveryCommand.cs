namespace DcaShop.Checkout.Application.SubmitDelivery;

public sealed record SubmitDeliveryCommand(Guid SessionId, string Street, string? StreetLine2, string City, string PostalCode, string Country, string? State, string ShippingOptionId);
