using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Outgoing.Payment;

/// <summary>
/// The payment service provider over REST — the anti-corruption layer towards it. A payment is one
/// <c>POST /payments</c> with amount, currency and the checkout reference (<c>merchantReference</c>): <c>201</c> with a payment reference authorizes it, <c>402</c>
/// refuses it, and anything else — no answer in time, no connection, another status, no reference — counts as the
/// provider being unavailable. An authorized payment is cancelled with <c>POST /payments/{payment reference}/cancel</c>:
/// <c>204</c> cancels it, anything else counts as unavailable. The provider's request and answer shapes stay inside
/// this class.
/// </summary>
/// <remarks>
/// The contract has no availability or confirmation endpoint: availability is learned from the payment request itself,
/// and a payment is complete once authorized.
/// </remarks>
public sealed class RestPaymentProvider : IPaymentProvider
{
    public const string HttpClientName = "payment-provider";

    public static readonly PaymentProviderId ProviderId = PaymentProviderId.Of("provider");

    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<RestPaymentProvider> _logger;

    public RestPaymentProvider(IHttpClientFactory httpClients, ILogger<RestPaymentProvider> logger)
    {
        _httpClients = httpClients;
        _logger = logger;
    }

    public PaymentProviderId Id => ProviderId;

    public string DisplayName => "Payment provider";

    public async Task<IPaymentProvider.PaymentResult> InitiatePaymentAsync(CheckoutSessionId sessionId, Money amount, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClients.CreateClient(HttpClientName)
                .PostAsJsonAsync("payments", new PaymentRequest(amount.Amount.ToString("0.00", CultureInfo.InvariantCulture), amount.Currency.Code, sessionId.ToString()), cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.PaymentRequired)
            {
                return IPaymentProvider.PaymentResult.Refused($"The payment provider refused the payment for session {sessionId}");
            }

            if (response.StatusCode != HttpStatusCode.Created)
            {
                return Unavailable($"The payment provider answered {(int)response.StatusCode}", sessionId);
            }

            var authorization = await response.Content.ReadFromJsonAsync<PaymentAuthorization>(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(authorization?.Reference)
                ? Unavailable("The payment provider authorized without a payment reference", sessionId)
                : IPaymentProvider.PaymentResult.Succeeded(authorization.Reference);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The client's own deadline, not the caller giving up: the caller's cancellation travels on unchanged
            return Unavailable("The payment provider did not answer in time", sessionId);
        }
        catch (HttpRequestException e)
        {
            return Unavailable($"The payment provider could not be reached: {e.Message}", sessionId);
        }
        catch (JsonException e)
        {
            return Unavailable($"The payment provider's answer could not be read: {e.Message}", sessionId);
        }
    }

    public Task<IPaymentProvider.PaymentResult> ConfirmPaymentAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(IPaymentProvider.PaymentResult.Succeeded(providerReference));

    public async Task<IPaymentProvider.PaymentResult> CancelPaymentAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClients.CreateClient(HttpClientName)
                .PostAsync($"payments/{Uri.EscapeDataString(providerReference)}/cancel", content: null, cancellationToken)
                .ConfigureAwait(false);

            return response.StatusCode == HttpStatusCode.NoContent
                ? IPaymentProvider.PaymentResult.Succeeded(providerReference)
                : CancellationUnavailable($"The payment provider answered {(int)response.StatusCode}", providerReference);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CancellationUnavailable("The payment provider did not answer in time", providerReference);
        }
        catch (HttpRequestException e)
        {
            return CancellationUnavailable($"The payment provider could not be reached: {e.Message}", providerReference);
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    private IPaymentProvider.PaymentResult Unavailable(string reason, CheckoutSessionId sessionId)
    {
        _logger.LogWarning("Payment for session {SessionId} not taken: {Reason}", sessionId, reason);
        return IPaymentProvider.PaymentResult.Unavailable(reason);
    }

    private IPaymentProvider.PaymentResult CancellationUnavailable(string reason, string providerReference)
    {
        _logger.LogWarning("Payment {Reference} not cancelled: {Reason}", providerReference, reason);
        return IPaymentProvider.PaymentResult.Unavailable(reason);
    }

    /// <summary>
    /// The amount as a decimal string with two places — a JSON number would pass through binary floating point — and
    /// the checkout reference: the checkout session's id as text, the same in every request of that checkout.
    /// </summary>
    private sealed record PaymentRequest(string Amount, string Currency, string MerchantReference);

    private sealed record PaymentAuthorization(string? Reference);
}