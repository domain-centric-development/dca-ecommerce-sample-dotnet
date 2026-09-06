using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.GetPaymentProviders;

public interface IGetPaymentProvidersInputPort : IUseCase<GetPaymentProvidersQuery, GetPaymentProvidersResult>
{
}
