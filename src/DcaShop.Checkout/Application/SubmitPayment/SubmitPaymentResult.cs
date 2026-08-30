using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.SubmitPayment;

public sealed record SubmitPaymentResult(CheckoutCartSnapshot Session);
