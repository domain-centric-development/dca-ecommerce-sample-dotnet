using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.StartCheckout;

public sealed record StartCheckoutResult(CheckoutCartSnapshot Session);
