namespace DcaShop.Cart.Application.MergeCarts;

/// <summary>
/// How the visitor wants their two carts reconciled. It is their decision, not the system's: only they know
/// whether the items they collected as a guest replace or complete what the account already holds.
/// </summary>
public enum CartMergeStrategy
{
    /// <summary>Keep everything: the guest cart's items are added to the account cart.</summary>
    MergeBoth,

    /// <summary>Keep the account cart and discard the guest cart.</summary>
    UseAccountCart,

    /// <summary>Keep the guest cart and discard what the account held.</summary>
    UseAnonymousCart,
}

/// <summary>
/// Reads the strategy a form submitted. The submitted values are the same in both samples so the shared
/// end-to-end suite drives either shop, which is why they are screaming case rather than the C# member names.
/// </summary>
public static class CartMergeStrategySubmission
{
    public const string MergeBoth = "MERGE_BOTH";
    public const string UseAccountCart = "USE_ACCOUNT_CART";
    public const string UseAnonymousCart = "USE_ANONYMOUS_CART";

    public static CartMergeStrategy? Parse(string? submitted) => submitted switch
    {
        MergeBoth => CartMergeStrategy.MergeBoth,
        UseAccountCart => CartMergeStrategy.UseAccountCart,
        UseAnonymousCart => CartMergeStrategy.UseAnonymousCart,
        _ => null,
    };
}
