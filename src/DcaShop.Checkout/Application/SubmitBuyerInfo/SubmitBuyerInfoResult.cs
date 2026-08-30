using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.SubmitBuyerInfo;

public sealed record SubmitBuyerInfoResult(CheckoutCartSnapshot Session);
