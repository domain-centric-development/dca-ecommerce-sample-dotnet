using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Identity of a user account, shared by every context that refers to a user.</summary>
public readonly record struct UserId(string Value) : IId
{
    public static UserId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("User id cannot be blank", nameof(value));
        }

        return new UserId(value);
    }

    /// <summary>
    /// Mints an identity for a browser that presents none. Anonymous and registered identities are the same kind
    /// of value — the cart is keyed on it either way — so registration keeps the identity it was handed rather
    /// than replacing it.
    /// </summary>
    public static UserId GenerateAnonymous() => new(Guid.NewGuid().ToString());

    public override string ToString() => Value;
}
