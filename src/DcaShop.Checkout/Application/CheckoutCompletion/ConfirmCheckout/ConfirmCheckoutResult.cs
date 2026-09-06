using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.ConfirmCheckout;

public sealed record ConfirmCheckoutResult(CheckoutCartSnapshot Session);
