using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging.Abstractions;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// What the payment provider is told, and when. Initiating a payment is a remote effect: it exists at the
/// provider whether or not this shop goes on to accept it.
/// </summary>
/// <remarks>
/// So everything the session itself can refuse is refused before the call, and an intent the session turns out
/// not to accept is released again.
/// </remarks>
public sealed class SubmitPaymentUseCaseTest
{
    private const string Customer = "customer-1";

    private readonly InMemoryCheckoutSessionRepository _sessions = new();
    private readonly RecordingPaymentProvider _provider = new();

    private SubmitPaymentUseCase UseCase() => new(
        _sessions,
        new SingleProviderRegistry(_provider),
        new SilentPublisher(),
        new InMemoryTransactionBoundary(),
        NullLogger<SubmitPaymentUseCase>.Instance);

    [Fact]
    public async Task AnIncompleteSessionNeverReachesTheProvider()
    {
        var session = await SessionWithBuyerInfoOnlyAsync();

        await Assert.ThrowsAsync<CheckoutStepNotCompletedException>(() => UseCase().ExecuteAsync(
            new SubmitPaymentCommand(session.Id.Value, Customer, "mock")));

        Assert.Empty(_provider.Initiations);
    }

    [Fact]
    public async Task AnIntentTheSessionRejectsIsReleased()
    {
        var session = await ReadySessionAsync();

        // The session moves on while the provider is being called — here by being abandoned, in production by a
        // concurrent confirmation or an expiry.
        _provider.DuringInitiation = async () =>
        {
            session.Abandon();
            await _sessions.SaveAsync(session);
        };

        await Assert.ThrowsAsync<CheckoutNotModifiableException>(() => UseCase().ExecuteAsync(
            new SubmitPaymentCommand(session.Id.Value, Customer, "mock")));

        Assert.Single(_provider.Initiations);
        Assert.Equal(_provider.Initiations, _provider.Cancellations);
    }

    [Fact]
    public async Task AnIntentIsReleasedEvenWhenTheRequestWasCancelled()
    {
        var session = await ReadySessionAsync();
        using var caller = new CancellationTokenSource();

        // The caller goes away while the provider is being called: the transaction is aborted by that same
        // token, and the clean-up must not be.
        _provider.DuringInitiation = () =>
        {
            caller.Cancel();
            return Task.CompletedTask;
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => UseCase().ExecuteAsync(
            new SubmitPaymentCommand(session.Id.Value, Customer, "mock"), caller.Token));

        Assert.Single(_provider.Initiations);
        Assert.Equal(_provider.Initiations, _provider.Cancellations);
    }

    [Fact]
    public async Task AReadySessionKeepsItsIntent()
    {
        var session = await ReadySessionAsync();

        await UseCase().ExecuteAsync(new SubmitPaymentCommand(session.Id.Value, Customer, "mock"));

        Assert.Single(_provider.Initiations);
        Assert.Empty(_provider.Cancellations);
    }

    private async Task<CheckoutSession> SessionWithBuyerInfoOnlyAsync()
    {
        var session = StartedSession();
        session.SubmitBuyerInfo(new BuyerInfo("ada@example.com", "Ada", "Lovelace", "+1-555-0100"));
        return await _sessions.SaveAsync(session);
    }

    private async Task<CheckoutSession> ReadySessionAsync()
    {
        var session = await SessionWithBuyerInfoOnlyAsync();
        session.SubmitDelivery(
            new DeliveryAddress("123 Main Street", "Springfield", "12345", "United States"),
            new ShippingOption("STANDARD", "Standard Shipping", "5-7 days", Money.Euro(5)));
        return await _sessions.SaveAsync(session);
    }

    private static CheckoutSession StartedSession() => CheckoutSession.Start(
        new CartId(Guid.NewGuid()),
        CustomerId.Of(Customer),
        new[] { new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10), 1, null) },
        Money.Euro(10));

    /// <summary>A provider that remembers what it was asked to do.</summary>
    private sealed class RecordingPaymentProvider : IPaymentProvider
    {
        public List<string> Initiations { get; } = new();

        public List<string> Cancellations { get; } = new();

        /// <summary>What happens at the provider's end while the call is in flight.</summary>
        public Func<Task> DuringInitiation { get; set; } = () => Task.CompletedTask;

        public PaymentProviderId Id => PaymentProviderId.Of("mock");

        public string DisplayName => "Recording provider";

        public async Task<IPaymentProvider.PaymentResult> InitiatePaymentAsync(CheckoutSessionId sessionId, Money amount, CancellationToken cancellationToken = default)
        {
            var reference = $"intent-{Initiations.Count}";
            Initiations.Add(reference);
            await DuringInitiation();
            return IPaymentProvider.PaymentResult.Succeeded(reference);
        }

        public Task<IPaymentProvider.PaymentResult> ConfirmPaymentAsync(string providerReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(IPaymentProvider.PaymentResult.Succeeded(providerReference));

        public Task<IPaymentProvider.PaymentResult> CancelPaymentAsync(string providerReference, CancellationToken cancellationToken = default)
        {
            // An HTTP client would do the same: a cancelled token means the call never leaves.
            cancellationToken.ThrowIfCancellationRequested();
            Cancellations.Add(providerReference);
            return Task.FromResult(IPaymentProvider.PaymentResult.Succeeded(providerReference));
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    /// <summary>The only provider this shop knows in the test.</summary>
    private sealed record SingleProviderRegistry(IPaymentProvider Provider) : IPaymentProviderRegistry
    {
        public Task<IReadOnlyList<IPaymentProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IPaymentProvider>>(new[] { Provider });

        public Task<IPaymentProvider?> FindAsync(PaymentProviderId providerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IPaymentProvider?>(Provider.Id == providerId ? Provider : null);
    }

    private sealed class SilentPublisher : IDomainEventPublisher
    {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
        {
            // Real infrastructure honours the token; the in-memory doubles are where a test would otherwise
            // never see a cancelled write.
            cancellationToken.ThrowIfCancellationRequested();
            aggregate.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }
}
