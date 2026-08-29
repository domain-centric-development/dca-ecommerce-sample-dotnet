using DcaShop.Product.Domain.Event;
using DcaShop.Product.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Adapter.Outgoing.Event;

/// <summary>Translates the domain event <see cref="ProductCreated"/> into the published <see cref="ProductCreatedEvent"/>.</summary>
public sealed class ProductCreatedEventPublisher : EventListener<ProductCreated>
{
    private readonly IIntegrationEventPublisher _publisher;

    public ProductCreatedEventPublisher(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    protected override Task OnAsync(ProductCreated @event, CancellationToken cancellationToken) =>
        _publisher.PublishAsync(
            new ProductCreatedEvent(
                @event.EventId,
                @event.OccurredOn,
                @event.ProductId,
                @event.Sku.Value,
                @event.Name.Value,
                @event.Category.Name,
                @event.InitialPrice.Value,
                @event.InitialStock),
            cancellationToken);
}
