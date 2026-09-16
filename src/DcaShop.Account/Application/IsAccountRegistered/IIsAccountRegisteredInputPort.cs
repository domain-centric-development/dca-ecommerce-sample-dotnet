using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>
/// Asks whether an account is registered for a user id. Called by the authentication handler before it honours a
/// session token: the token is self-contained and outlives the account it names, so the question has to be asked
/// of the context that owns accounts. It points inward, which is why it is a query use case and not an output port.
/// </summary>
public interface IIsAccountRegisteredInputPort : IUseCase<IsAccountRegisteredQuery, IsAccountRegisteredResult>
{
}
