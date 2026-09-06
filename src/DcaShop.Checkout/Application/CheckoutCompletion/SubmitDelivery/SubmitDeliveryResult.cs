using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.SubmitDelivery;

public sealed record SubmitDeliveryResult(CheckoutCartSnapshot Session);
