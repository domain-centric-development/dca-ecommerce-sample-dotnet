using DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;
using DcaShop.Checkout.Domain.Event;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The order confirmation mail: sent once per order however often its delivery is repeated, and delayed — not lost —
/// while the mail provider is down. Through the input ports of a shop of its own, wired to a payment provider and a mail
/// provider that are stubs on free ports.
/// </summary>
public sealed class OrderConfirmationTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string Jane = "jane@example.com";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly WireMockServer _payments = WireMockServer.Start();
    private readonly WireMockServer _mails = WireMockServer.Start();
    private WebApplicationFactory<Program>? _shop;

    public OrderConfirmationTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARepeatedDeliveryOfAnOrdersConfirmationSendsItsMailOnceUnderOneKey()
    {
        _payments.AuthorizesPayments();
        _mails.AcceptsMails();
        var checkouts = Checkouts();

        // Given a guest who placed an order with the buyer e-mail "jane@example.com"
        var checkout = await checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        await checkouts.PlaceOrderAsync(checkout);
        var sent = await MailProviderStubs.WithinFifteenSecondsAsync(() => _mails.MailsTo(Jane), mails => mails.Count > 0);
        Assert.NotEmpty(sent);

        // When the delivery of that order's confirmation is repeated
        var confirmed = ConfirmationOf(checkout);
        using (var scope = _shop!.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISendOrderConfirmationInputPort>()
                .ExecuteAsync(new SendOrderConfirmationCommand(confirmed.EventId, checkout.SessionId, checkout.CustomerId));
        }

        // Then the mail provider has received the order-confirmation mail for that order once, under the same key
        var mail = Assert.Single(_mails.MailsTo(Jane));
        Assert.Equal("order-confirmation", mail.Template);
        Assert.Contains(checkout.SessionId.ToString(), mail.Key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheConfirmationHeldBackByAnUnavailableMailProviderReachesItOnceItIsBack()
    {
        _payments.AuthorizesPayments();

        // Given the mail provider is unavailable
        _mails.IsUnavailable();
        var checkouts = Checkouts();

        // And a guest with the buyer e-mail "jane@example.com" has placed an order
        var checkout = await checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        await checkouts.PlaceOrderAsync(checkout);
        Assert.Equal(CheckoutSessionStatus.Confirmed, (await checkouts.SessionAsync(checkout)).Status);
        var turnedAway = await MailProviderStubs.WithinFifteenSecondsAsync(() => _mails.MailsTo(Jane), mails => mails.Count > 0);
        Assert.NotEmpty(turnedAway);
        Assert.All(turnedAway, mail => Assert.Equal(503, mail.Status));

        // When the mail provider answers again
        _mails.AcceptsMails();

        // Then shortly afterwards exactly one order-confirmation mail to "jane@example.com" reaches it
        var accepted = await MailProviderStubs.WithinFifteenSecondsAsync(
            () => _mails.MailsTo(Jane).Where(mail => mail.Status == 202).ToList(), mails => mails.Count > 0);
        var mail = Assert.Single(accepted);
        Assert.Equal("order-confirmation", mail.Template);
    }

    /// <summary>The confirmation the order raised, as the outbox holds it for its delivery.</summary>
    private CheckoutConfirmed ConfirmationOf(CustomersCheckout checkout) =>
        Assert.Single(_shop!.Services.GetRequiredService<IEventOutbox>().All()
            .Select(publication => publication.Event)
            .OfType<CheckoutConfirmed>(), confirmed => confirmed.SessionId.Value == checkout.SessionId);

    /// <summary>This test's own shop, wired to both stubs; its buyers give Jane's address.</summary>
    private PortCheckouts Checkouts()
    {
        _shop = PortCheckouts.ShopWith(_factory, _payments)
            .WithWebHostBuilder(builder => builder
                .UseSetting("Checkout:MailProvider:BaseUrl", _mails.Url)
                .UseSetting("Checkout:MailProvider:Timeout", "00:00:30"));
        return new PortCheckouts(_shop) { BuyerEmail = Jane };
    }

    public void Dispose()
    {
        _shop?.Dispose();
        _payments.Stop();
        _mails.Stop();
    }
}