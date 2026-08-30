using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.StartCheckout;

/// <summary>Starts a checkout from an active cart. Line items get fresh prices; the cart stays active until confirmation.</summary>
public sealed class StartCheckoutUseCase : IStartCheckoutInputPort
{
    private readonly ICartDataPort _cartData;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;

    public StartCheckoutUseCase(ICartDataPort cartData, ICheckoutArticleDataPort articleData, ICheckoutSessionRepository sessions, IDomainEventPublisher events, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _cartData = cartData;
        _articleData = articleData;
        _sessions = sessions;
        _events = events;
    }

    public async Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var existing = await _sessions.FindActiveByCartIdAsync(cartId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return new StartCheckoutResult(CheckoutSessionData.From(existing));
        }

        // Cart and article data come from other contexts (remote-capable) — outside the unit of work
        var cart = await _cartData.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));
        if (!cart.Active)
        {
            throw new InvalidOperationException($"Cart is not active: {cartId}");
        }

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException($"Cannot checkout empty cart: {cartId}");
        }

        var articles = await _articleData.GetArticleDataAsync(cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var lineItems = new List<CheckoutLineItem>();
        var subtotal = Money.Euro(0m);
        foreach (var cartItem in cart.Items)
        {
            if (!articles.TryGetValue(cartItem.ProductId, out var article))
            {
                throw new ArgumentException($"Product not found: {cartItem.ProductId}", nameof(command));
            }

            var lineItem = new CheckoutLineItem(CheckoutLineItemId.Generate(), cartItem.ProductId, article.Name, article.CurrentPrice, cartItem.Quantity, article.ImageUrl);
            lineItems.Add(lineItem);
            subtotal = subtotal.Add(lineItem.LineTotal);
        }

        // Short unit of work: create, save, publish
        return await _unitOfWork.RunAsync(
            async ct =>
            {
                var session = CheckoutSession.Start(cart.CartId, cart.CustomerId, lineItems, subtotal);
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return new StartCheckoutResult(CheckoutSessionData.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
