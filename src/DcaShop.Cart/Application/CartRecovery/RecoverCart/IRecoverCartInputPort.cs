using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

public interface IRecoverCartInputPort : IUseCase<RecoverCartCommand, RecoverCartResult>
{
}