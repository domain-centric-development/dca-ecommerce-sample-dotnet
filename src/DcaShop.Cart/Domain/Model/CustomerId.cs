using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Owner of a cart — a registered user or a guest session. Context-local reference into Account.</summary>
public readonly record struct CustomerId(string Value) : IId
{
    public static CustomerId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Customer id cannot be blank", nameof(value));
        }

        return new CustomerId(value);
    }

    public override string ToString() => Value;
}
