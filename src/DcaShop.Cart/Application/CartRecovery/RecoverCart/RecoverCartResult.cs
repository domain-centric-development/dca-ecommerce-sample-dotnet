using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

/// <summary>What the recovery did, if anything.</summary>
public sealed record RecoverCartResult(
    Guid? CartId,
    string CustomerId,
    IReadOnlyList<RecoverCartResult.CartItemSummary> Items,
    Money Total,
    int ItemsRecovered,
    bool AnonymousCartDeleted)
{
    /// <summary>Nothing to recover: the identity did not change, or the guest cart was empty or absent.</summary>
    public static RecoverCartResult NoRecoveryNeeded(string customerId) =>
        new(null, customerId, [], Money.Zero(Currency.Euro), 0, false);

    public sealed record CartItemSummary(Guid ItemId, Guid ProductId, int Quantity, Money UnitPrice);
}