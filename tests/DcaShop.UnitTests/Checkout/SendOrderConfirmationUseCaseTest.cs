using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Transactions;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// What the use case refuses before any mail goes out: a checkout that was never confirmed has no buyer info, and the
/// refusal is named — nothing is sent, nothing is recorded, so the delivery's own retry decides what happens next.
/// </summary>
public sealed class SendOrderConfirmationUseCaseTest
{
    private readonly InMemoryCheckoutSessionRepository _sessions = new();
    private readonly InMemoryProcessedEvents _processedEvents = new();
    private readonly RecordingMailer _mailer = new();

    [Fact]
    public async Task ACheckoutWithoutBuyerInfoIsRefusedByNameAndNothingIsSentOrRecorded()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10m), 1, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
        await _sessions.SaveAsync(session);
        var eventId = Guid.NewGuid();

        var refusal = await Assert.ThrowsAsync<BuyerInfoNotSubmittedException>(() =>
            UseCase().ExecuteAsync(new SendOrderConfirmationCommand(eventId, session.Id.Value, "guest")));

        Assert.Equal(session.Id, refusal.SessionId);
        Assert.Empty(_mailer.Sent);
        Assert.False(await _processedEvents.ContainsAsync("send-order-confirmation", eventId));
    }

    private SendOrderConfirmationUseCase UseCase() => new(_sessions, _processedEvents, _mailer, new InMemoryTransactionBoundary());

    private sealed class RecordingMailer : IOrderConfirmationMailer
    {
        public List<IOrderConfirmationMailer.OrderConfirmationMail> Sent { get; } = new();

        public Task<IOrderConfirmationMailer.MailOutcome> SendAsync(IOrderConfirmationMailer.OrderConfirmationMail mail, CancellationToken cancellationToken = default)
        {
            Sent.Add(mail);
            return Task.FromResult(IOrderConfirmationMailer.MailOutcome.Accepted);
        }
    }
}