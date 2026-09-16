using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>
/// Answers whether a registered account exists for an identity. A session token is self-contained, so it outlives
/// the account it names: the authentication adapter asks this port before honouring such a token.
/// </summary>
public interface IIsAccountRegisteredInputPort : IUseCase<IsAccountRegisteredQuery, IsAccountRegisteredResult>
{
}
