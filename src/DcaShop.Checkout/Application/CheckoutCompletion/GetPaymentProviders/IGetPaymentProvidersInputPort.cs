using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.GetPaymentProviders;

public interface IGetPaymentProvidersInputPort : IUseCase<GetPaymentProvidersQuery, GetPaymentProvidersResult>
{
}
