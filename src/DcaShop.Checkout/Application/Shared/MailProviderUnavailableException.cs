using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>
/// Raised when the mail provider did not accept the mail: it answered otherwise, could not be reached, or did not
/// answer in time.
/// </summary>
/// <remarks>
/// Part of the <see cref="IOrderConfirmationMailer"/> contract: the provider is a foreign system, and its being
/// unreachable is expected behaviour of the integration. A failure the output port declares, so an
/// <see cref="OutputPortException"/>: the adapter throws it, the use case catches it and answers with a case of its
/// result. The mail is not lost: trying again later is the sensible answer, with the same key.
/// </remarks>
public sealed class MailProviderUnavailableException : OutputPortException
{
    public MailProviderUnavailableException(string key, string reason)
        : base($"The mail provider did not accept the mail {key}: {reason}")
    {
        Key = key;
    }

    public string Key { get; }
}