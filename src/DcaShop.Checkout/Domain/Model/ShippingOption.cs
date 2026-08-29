using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Selectable shipping method with estimated delivery time and cost.</summary>
public sealed record ShippingOption : IValue
{
    public ShippingOption(string id, string name, string estimatedDelivery, Money cost)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        EstimatedDelivery = Required(estimatedDelivery, nameof(estimatedDelivery));
        Cost = cost ?? throw new ArgumentNullException(nameof(cost));
    }

    public string Id { get; }

    public string Name { get; }

    public string EstimatedDelivery { get; }

    public Money Cost { get; }

    public bool IsFree => Cost.IsZero;

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} cannot be blank", name) : value.Trim();
}
