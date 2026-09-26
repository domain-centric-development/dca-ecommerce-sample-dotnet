namespace DcaShop.Checkout.Adapter.Outgoing.Payment;

/// <summary>
/// Where the payment service provider answers and how long it may take — the REST payment adapter's settings, so the
/// class lives there; the context registration binds it from configuration.
/// </summary>
public sealed class PaymentProviderOptions
{
    public const string SectionName = "Checkout:PaymentProvider";

    /// <summary>
    /// The provider's address. Without one the shop takes payments with the stand-in inside the sample, so running it
    /// locally needs no provider.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>A provider that has not answered within this time counts as unavailable.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
}