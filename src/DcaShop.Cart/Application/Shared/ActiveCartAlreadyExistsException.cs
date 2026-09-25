using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shared;

/// <summary>Raised when a second active cart would be stored for a customer who already has one.</summary>
/// <remarks>
/// Part of the <see cref="IShoppingCartRepository"/> contract rather than of the aggregate: only the store
/// sees every cart of a customer at once, so only the store can refuse the second one. Every implementation
/// reports it with this type, whatever its own mechanism is, so a caller can react to a lost race without
/// knowing which store it is talking to.
/// </remarks>
public sealed class ActiveCartAlreadyExistsException : UseCaseException
{
    public ActiveCartAlreadyExistsException(CustomerId customerId)
        : base($"Customer {customerId.Value} already has an active cart")
    {
        CustomerId = customerId;
    }

    public CustomerId CustomerId { get; }
}