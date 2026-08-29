using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

public readonly record struct CheckoutSessionId(Guid Value) : IId
{
    public static CheckoutSessionId Generate() => new(Guid.NewGuid());

    public static CheckoutSessionId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}

public readonly record struct CheckoutLineItemId(Guid Value) : IId
{
    public static CheckoutLineItemId Generate() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>Context-local copy of the cart's identity — no coupling to the Cart context's types.</summary>
public readonly record struct CartId(Guid Value) : IId
{
    public static CartId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}

/// <summary>Context-local identifier of the customer (may be a guest session).</summary>
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

/// <summary>Identifier of an external payment provider, e.g. <c>stripe</c>, <c>paypal</c>, <c>invoice</c>.</summary>
public readonly record struct PaymentProviderId(string Value) : IId
{
    public static PaymentProviderId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Payment provider id cannot be blank", nameof(value));
        }

        return new PaymentProviderId(value);
    }

    public override string ToString() => Value;
}
