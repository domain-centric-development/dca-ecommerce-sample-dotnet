using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the mail provider did not accept a mail: it is down or did not answer in time.</summary>
/// <remarks>The mail is not lost: the delivery that asked for it is tried again later, under the same key.</remarks>
public sealed class MailProviderUnavailableException : UseCaseException
{
    public MailProviderUnavailableException(string key)
        : base($"The mail provider did not accept the mail {key}")
    {
        Key = key;
    }

    public string Key { get; }
}