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

    public override string ToString() => Value;
}
