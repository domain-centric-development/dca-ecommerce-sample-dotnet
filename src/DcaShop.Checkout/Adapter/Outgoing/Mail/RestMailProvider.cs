using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using DcaShop.Checkout.Application.Shared;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Outgoing.Mail;

/// <summary>
/// The mail provider over REST — the anti-corruption layer towards it. A mail is one <c>POST /messages</c> with the
/// recipient, the template <c>order-confirmation</c>, the idempotency key and the mail's values as text: <c>202</c>
/// accepts it, and anything else — another status, no answer in time, no connection — counts as the provider being
/// unavailable. The provider's request shape stays inside this class.
/// </summary>
public sealed class RestMailProvider : IOrderConfirmationMailPort
{
    public const string HttpClientName = "mail-provider";

    private const string Template = "order-confirmation";

    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<RestMailProvider> _logger;

    public RestMailProvider(IHttpClientFactory httpClients, ILogger<RestMailProvider> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    public async Task<bool> SendAsync(IOrderConfirmationMailPort.OrderConfirmationMail mail, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        var request = new MailRequest(mail.To, Template, mail.Key, new Dictionary<string, string>
        {
            ["orderTotal"] = mail.OrderTotal.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = mail.OrderTotal.Currency.Code,
        });

        try
        {
            using var response = await _httpClients.CreateClient(HttpClientName)
                .PostAsJsonAsync("messages", request, cancellationToken)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                return Unavailable($"The mail provider answered {(int)response.StatusCode}", mail.Key);
            }

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The client's own deadline, not the caller giving up: the caller's cancellation travels on unchanged
            return Unavailable("The mail provider did not answer in time", mail.Key);
        }
        catch (HttpRequestException e)
        {
            return Unavailable($"The mail provider could not be reached: {e.Message}", mail.Key);
        }
    }

    private bool Unavailable(string reason, string key)
    {
        _logger.LogWarning("Mail {Key} not accepted: {Reason}", key, reason);
        return false;
    }

    /// <summary>The provider's mail: the values in <c>data</c> as text, the key the same for every attempt of the mail.</summary>
    private sealed record MailRequest(string To, string Template, string Key, IReadOnlyDictionary<string, string> Data);
}