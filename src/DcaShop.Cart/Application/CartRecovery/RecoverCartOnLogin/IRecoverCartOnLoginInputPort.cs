using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartRecovery.RecoverCartOnLogin;

public interface IRecoverCartOnLoginInputPort : IUseCase<RecoverCartOnLoginCommand, RecoverCartOnLoginResult>
{
}
