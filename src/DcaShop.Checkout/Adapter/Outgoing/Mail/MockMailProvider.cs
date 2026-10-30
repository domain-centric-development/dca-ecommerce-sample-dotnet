using DcaShop.Checkout.Application.Shared;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Outgoing.Mail;

/// <summary>
/// Stands in for a mail provider where none is configured: it takes every mail, logs it and sends nothing. Enough to run
/// the shop without one.
/// </summary>
public sealed class MockMailProvider : IOrderConfirmationMailer
{
    private readonly ILogger<MockMailProvider> _logger;

    public MockMailProvider(ILogger<MockMailProvider> logger)
    {
        _logger = logger;
    }

    public Task<IOrderConfirmationMailer.MailOutcome> SendAsync(IOrderConfirmationMailer.OrderConfirmationMail mail, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        _logger.LogInformation("Order confirmation {Key} taken by the stand-in mail provider, not sent", mail.Key);
        return Task.FromResult(IOrderConfirmationMailer.MailOutcome.Accepted);
    }
}