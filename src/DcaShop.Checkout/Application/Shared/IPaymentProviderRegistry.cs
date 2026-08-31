using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>The payment providers a customer may choose from — the caller-owned port in front of the payment service provider.</summary>
public interface IPaymentProviderRegistry : IOutputPort
{
    Task<IReadOnlyList<IPaymentProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    Task<IPaymentProvider?> FindAsync(PaymentProviderId providerId, CancellationToken cancellationToken = default);
}
