using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.RecoverCartOnLogin;

public interface IRecoverCartOnLoginInputPort : IUseCase<RecoverCartOnLoginCommand, RecoverCartOnLoginResult>
{
}
