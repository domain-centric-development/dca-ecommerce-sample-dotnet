using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Application.CartSync.SyncCheckoutWithCart;

/// <summary>
/// Legacy cart-change compatibility operation. Cart edits never create or mutate checkout snapshots.
/// </summary>
public sealed class SyncCheckoutWithCartUseCase : ISyncCheckoutWithCartInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICartDataPort _cartData;
    private readonly TaxCalculator _taxCalculator;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<SyncCheckoutWithCartUseCase> _logger;

    public SyncCheckoutWithCartUseCase(
        ICheckoutSessionRepository sessions,
        ICartDataPort cartData,
        TaxCalculator taxCalculator,
        ICheckoutArticleDataPort articleData,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<SyncCheckoutWithCartUseCase> logger)
    {
        _sessions = sessions;
        _cartData = cartData;
        _taxCalculator = taxCalculator;
        _articleData = articleData;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public Task<SyncCheckoutWithCartResult> ExecuteAsync(SyncCheckoutWithCartCommand command, CancellationToken cancellationToken = default)
    {
        // D06a: cart edits neither create nor mutate a checkout snapshot.
        return Task.FromResult(SyncCheckoutWithCartResult.NoActiveSession());
    }
}
