using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

/// <summary>
/// What recovering a guest cart came to: the recovery ran — whether or not there was anything to bring — or the
/// request named a guest other than the one the visitor was before their login.
/// </summary>
[GenerateOneOf]
public sealed partial class RecoverCartResult : OneOfBase<RecoverCartResult.Recovered, RecoverCartResult.GuestCartNotOwned>
{
    /// <summary>What the recovery did, if anything; <paramref name="CartId"/> is absent when there was nothing to recover.</summary>
    public sealed record Recovered(
        Guid? CartId,
        string CustomerId,
        IReadOnlyList<CartItemSummary> Items,
        Money Total,
        int ItemsRecovered,
        bool AnonymousCartDeleted)
    {
        /// <summary>Nothing to recover: the identity did not change, or the guest cart was empty or absent.</summary>
        public static Recovered NoRecoveryNeeded(string customerId) =>
            new(null, customerId, [], Money.Zero(Currency.Euro), 0, false);
    }

    /// <summary>
    /// The request named a guest other than the one the visitor was before their login: only that guest's cart may
    /// follow into the account (ADR-007 — ownership is the use case's). Nothing was read or changed.
    /// </summary>
    /// <param name="Guest">The guest the request named.</param>
    public sealed record GuestCartNotOwned(CustomerId Guest) : IRefusal;

    public sealed record CartItemSummary(Guid ItemId, Guid ProductId, int Quantity, Money UnitPrice);
}