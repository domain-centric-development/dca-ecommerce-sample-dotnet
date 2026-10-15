using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The confirmation page's translation of a confirmed order: the amount is the order's total, not an amount paid —
/// against a stubbed query that answers with the confirmed order.
/// </summary>
public sealed class ConfirmationOrderTotalTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConfirmationOrderTotalTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheConfirmationCallsTheAmountTheOrderTotal()
    {
        var shop = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<IGetConfirmedCheckoutSessionInputPort>(new AnsweringWith(DomainDrivenDesignOnceWithStandardShipping()))));
        var guest = shop.CreateDefaultClient(new CookieContainerHandler(new CookieContainer()));

        var confirmation = await guest.GetStringAsync("/checkout/confirmation");

        var total = Regex.Match(confirmation, @"class=""order-summary__total""><span>([^<]*)</span><span>([^<]*)</span>");
        Assert.True(total.Success, "the confirmation has a total line");
        Assert.Equal(("Order Total", "59.98 EUR"), (total.Groups[1].Value, total.Groups[2].Value));
        Assert.DoesNotContain("Total Paid", AvailabilityPages.Section(confirmation, "confirmation-message"), StringComparison.Ordinal);
    }

    /// <summary>A confirmed order of "Domain-Driven Design" once, shipped with "Standard Shipping".</summary>
    private static CheckoutCartSnapshot DomainDrivenDesignOnceWithStandardShipping()
    {
        var price = Money.Euro(54.99m);
        var shipping = new ShippingOption("standard", "Standard Shipping", "5-7 business days", Money.Euro(4.99m));
        return new CheckoutCartSnapshot(
            CheckoutSessionId.Generate(),
            new CartId(Guid.NewGuid()),
            CustomerId.Of("jane"),
            CheckoutStep.Confirmation,
            CheckoutSessionStatus.Completed,
            [new LineItemSnapshot(CheckoutLineItemId.Generate(), ProductId.Generate(), "Domain-Driven Design", price, 1, null)],
            price,
            CheckoutTotals.Calculate(price, shipping.Cost),
            null,
            null,
            shipping,
            null,
            null);
    }

    /// <summary>A confirmed-order query that answers every customer with the same order.</summary>
    private sealed class AnsweringWith(CheckoutCartSnapshot session) : IGetConfirmedCheckoutSessionInputPort
    {
        public Task<GetConfirmedCheckoutSessionResult> ExecuteAsync(GetConfirmedCheckoutSessionQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GetConfirmedCheckoutSessionResult(session));
    }
}