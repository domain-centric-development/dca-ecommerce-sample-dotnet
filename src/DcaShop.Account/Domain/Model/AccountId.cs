using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>
/// Aggregate-internal identity of an account. Distinct from <see cref="SharedKernel.Domain.Model.UserId"/>: an
/// account has exactly one <c>AccountId</c> and is linked to exactly one <c>UserId</c>.
/// </summary>
public readonly record struct AccountId(Guid Value) : IId
{
    public static AccountId Generate() => new(Guid.NewGuid());

    public static AccountId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
