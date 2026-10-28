namespace DcaShop.Checkout.Adapter.Outgoing.Mail;

/// <summary>
/// Where the mail provider answers and how long it may take — the REST mail adapter's settings; the context
/// registration binds it from configuration.
/// </summary>
public sealed class MailProviderOptions
{
    public const string SectionName = "Checkout:MailProvider";

    /// <summary>The provider's address. Without one the stand-in inside the sample takes the mails and sends nothing.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>A provider that has not answered within this time counts as unavailable.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
}