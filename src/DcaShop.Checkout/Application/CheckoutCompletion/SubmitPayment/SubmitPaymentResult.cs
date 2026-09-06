using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;

public sealed record SubmitPaymentResult(CheckoutCartSnapshot Session);
