using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetPaymentProviders;

public sealed class GetPaymentProvidersUseCase : IGetPaymentProvidersInputPort
{
    private readonly IPaymentProviderRegistry _providers;

    public GetPaymentProvidersUseCase(IPaymentProviderRegistry providers)
    {
        _providers = providers;
    }

    public async Task<GetPaymentProvidersResult> ExecuteAsync(GetPaymentProvidersQuery query, CancellationToken cancellationToken = default)
    {
        var providers = await _providers.GetAvailableProvidersAsync(cancellationToken).ConfigureAwait(false);
        return new GetPaymentProvidersResult(providers.Select(p => new GetPaymentProvidersResult.PaymentProviderData(p.Id.Value, p.DisplayName)).ToList());
    }
}
