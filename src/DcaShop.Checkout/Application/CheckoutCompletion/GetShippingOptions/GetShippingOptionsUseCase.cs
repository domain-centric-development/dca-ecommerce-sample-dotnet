using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetShippingOptions;

public sealed class GetShippingOptionsUseCase : IGetShippingOptionsInputPort
{
    public Task<GetShippingOptionsResult> ExecuteAsync(GetShippingOptionsQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GetShippingOptionsResult(
            ShippingOptions.All.Select(o => new GetShippingOptionsResult.ShippingOptionData(o.Id, o.Name, o.EstimatedDelivery, o.Cost.ToString(), o.IsFree)).ToList()));
}
