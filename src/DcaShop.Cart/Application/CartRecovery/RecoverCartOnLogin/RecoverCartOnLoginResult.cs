using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Application.RecoverCartOnLogin;

/// <summary>What the recovery did, if anything.</summary>
public sealed record RecoverCartOnLoginResult(
    Guid? CartId,
    string CustomerId,
    IReadOnlyList<RecoverCartOnLoginResult.CartItemSummary> Items,
    string Total,
    int ItemsRecovered,
    bool AnonymousCartDeleted)
{
    /// <summary>Nothing to recover: the identity did not change, or the guest cart was empty or absent.</summary>
    public static RecoverCartOnLoginResult NoRecoveryNeeded(string customerId) =>
        new(null, customerId, [], Money.Zero("EUR").ToString(), 0, false);

    public sealed record CartItemSummary(Guid ItemId, Guid ProductId, int Quantity, string UnitPrice);
}
