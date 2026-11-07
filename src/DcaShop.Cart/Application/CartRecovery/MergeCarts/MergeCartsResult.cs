using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.CartRecovery.MergeCarts;

/// <summary>
/// What merging the guest's cart into the account's came to: merged, or the request named a guest other than the one
/// the visitor was before their login.
/// </summary>
[GenerateOneOf]
public sealed partial class MergeCartsResult : OneOfBase<MergeCartsResult.Merged, MergeCartsResult.GuestCartNotOwned>
{
    /// <summary>The account's cart after the merge, and what the merge did.</summary>
    public sealed record Merged(
        Guid CartId,
        string CustomerId,
        IReadOnlyList<CartItemSummary> Items,
        Money Total,
        CartMergeStrategy StrategyApplied,
        int ItemsFromAnonymous,
        int ItemsFromAccount,
        bool AnonymousCartDeleted);

    /// <summary>
    /// The request named a guest other than the one the visitor was before their login: only that guest's cart may
    /// follow into the account (ADR-007 — ownership is the use case's). Nothing was read or changed.
    /// </summary>
    /// <param name="Guest">The guest the request named.</param>
    public sealed record GuestCartNotOwned(CustomerId Guest) : IRefusal;

    public sealed record CartItemSummary(Guid ItemId, Guid ProductId, int Quantity, Money UnitPrice);
}