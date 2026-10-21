using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shared;

/// <summary>
/// Raised when a merge or a take-over names a guest other than the one the visitor was before their login: only that
/// guest's cart may follow into the account (ADR-007 — ownership is the use case's).
/// </summary>
public sealed class GuestCartNotOwnedException : UseCaseException
{
    public GuestCartNotOwnedException(CustomerId guest)
        : base($"Not the visitor's own guest: {guest.Value}")
    {
        Guest = guest;
    }

    public CustomerId Guest { get; }
}