using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

public sealed record ConfirmCheckoutResult(CheckoutCartSnapshot Session);
