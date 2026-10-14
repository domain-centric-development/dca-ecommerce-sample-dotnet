namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect carries to a checkout page, as a fixed code in its address (<c>?notice=&lt;code&gt;</c>) —
/// never the sentence and never in a server session, so any instance that answers the next request shows it.
/// </summary>
internal enum CheckoutStepNotice
{
    /// <summary>A step was sent before the earlier steps were done.</summary>
    EarlierStepsFirst,
}

/// <summary>Each notice's code in the address and the sentence its page shows.</summary>
internal static class CheckoutStepNotices
{
    public static string Code(this CheckoutStepNotice notice) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "earlier-steps-first",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    public static string Sentence(this CheckoutStepNotice notice) => notice switch
    {
        CheckoutStepNotice.EarlierStepsFirst => "Please complete the previous checkout steps first.",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The notice an address names by its code; none for a code the shop does not know.</summary>
    public static CheckoutStepNotice? FromCode(string? code) =>
        Enum.GetValues<CheckoutStepNotice>().Cast<CheckoutStepNotice?>().FirstOrDefault(notice => notice?.Code() == code);
}